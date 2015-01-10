// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Server.cs" company="">
//   
// </copyright>
// <summary>
//  Serwer obsługujący wiadomości przechodzące przez kolejki rabbita 
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DNA
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    using DNAClient;

    using DTO;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;

    /// <summary>
    /// Serwer obsługujący wiadomości przechodzące przez kolejki rabbita
    /// </summary>
    public class Server
    {
        private static readonly ManualResetEvent FinishEvent = new ManualResetEvent(false);
        private static ConnectionFactory factory = Constants.ConnectionFactory;
        private static DatabaseHelper db = new DatabaseHelper();
        private static string userPath;

        static void Main(string[] args)
        {
            userPath = Constants.userPath;
            GlobalsParameters.Instance.status = new Dictionary<string, PresenceStatus>();
            Task.Factory.StartNew(() => GetChannel());
            Task.Factory.StartNew(() => GetChannelRPC());

            Console.WriteLine("Starting server...");

            Console.ReadLine();
            FinishEvent.Set();
        }

        public static void GetChannel()
        {
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(Constants.Exchange, "topic");
                    var queueName = channel.QueueDeclare();

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (_, msg) => Receive(msg);
                    channel.QueueBind(queueName, Constants.Exchange, Constants.keyServerRequest + ".*");
                    channel.BasicConsume(queueName, true, consumer);
                    FinishEvent.WaitOne();
                }
            }
        }

        public static void GetChannelRPC()
        {
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(Constants.Exchange, false, false, false, null);
                    channel.BasicQos(0, 1, false);
                    var consumer = new QueueingBasicConsumer(channel);
                    channel.BasicConsume(Constants.Exchange, false, consumer);
                    Console.WriteLine(" [x] Awaiting RPC requests");

                    while (true)
                    {
                        AuthResponse response = new AuthResponse();
                        var ea = (BasicDeliverEventArgs)consumer.Queue.Dequeue();

                        var body = ea.Body;
                        var props = ea.BasicProperties;
                        var replyProps = channel.CreateBasicProperties();
                        replyProps.CorrelationId = props.CorrelationId;

                        try
                        {
                            
                            var request = body.DeserializeAuthRequest();
                            if (request.Type == AuthRequest.AuthorizationType.Login)
                            {
                                if (db.Login(request.Login, request.Password))
                                {
                                    GlobalsParameters.Instance.status.Add(request.Login, PresenceStatus.Online);
                                    Console.WriteLine(string.Format("Uzytkownik {0} pomyslnie sie zalogowal.", request.Login));
                                    response.Status = Status.OK;
                                }
                                else
                                {
                                    Console.WriteLine(string.Format("Nieudana proba zalogowania przez uzytkownika {0}.", request.Login));
                                    response.Status = Status.Error;
                                    response.Message = "Nazwa użytkownika i hasło niepoprawne.";
                                }
                            }
                            else if(request.Type == AuthRequest.AuthorizationType.Register)
                            {
                                Console.WriteLine(" RPC Login: {0}", request);
                                if (db.Register(request.Login, request.Password))
                                {
                                    Console.WriteLine(string.Format("Uzytkownik {0} pomyslnie sie zarejestrował.", request.Login));
                                    response.Status = Status.OK;
                                }
                                else
                                {
                                    Console.WriteLine(string.Format("Nieudana proba zarejestrowania uzytkownika {0}.", request.Login));
                                    response.Status = Status.Error;
                                    response.Message = "Nazwa użytkownika i hasło niepoprawne.";
                                }

                                Console.WriteLine(" RPC Register: {0}", request);
                                response.Status = Status.Error;
                                response.Message = "Nie udało się zarejestrować użytkownika";
                            }
                            else if (request.Type == AuthRequest.AuthorizationType.GetOldMessages)
                            {
                                string[] filePaths = Directory.GetFiles(userPath);
                                for (int i = 0; i < filePaths.Length; i++)
                                {
                                    if (Regex.IsMatch(filePaths[i], string.Format(@"{0}_(.*)", request.Login)))
                                    {
                                        var regex = Regex.Match(
                                            filePaths[i],
                                            string.Format(@"{0}_(.*)", request.Login));
                                        var group = regex.Groups[1];
                                        string sender = group.Value;
                                        var file = userPath + "//" + request.Login + "_" + sender;
                                        StreamReader streamReader = new StreamReader(file);
                                        string message = streamReader.ReadToEnd();
                                        streamReader.Close();
                                        MessageReq messageReq = new MessageReq()
                                        {
                                            Recipient = request.Login,
                                            Login = sender,
                                            Message = message
                                        };
                                        File.Delete(file);
                                        SendMessageNotification(messageReq, true);
                                    }
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("RPC Error: " + e.Message);
                        }
                        finally
                        {
                            var responseBytes = response.Serialize();
                            channel.BasicPublish("", props.ReplyTo, replyProps, responseBytes);
                            channel.BasicAck(ea.DeliveryTag, false);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Metoda obsługująca odebranie wiadomości
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void Receive(BasicDeliverEventArgs args)
        {
            var body = args.Body;
            var routingKey = args.RoutingKey;

            if (routingKey == Constants.keyServerRequestMessage)
            {
                var message = body.DeserializeMessageReq();

                Console.WriteLine(
                    " [Msg] '{0}':'{1} - {2}'",
                    routingKey,
                    message.Login,
                    message.Message);
                SendMessageNotification(message);
            }

            if (routingKey == Constants.keyServerRequestStatus)
            {
                var message = body.DeserializePresenceStatusNotification();

                Console.WriteLine(
                    " [State] '{0}':'{1} - {2}'",
                    routingKey,
                    message.Login,
                    message.PresenceStatus);
                SendStatusNotification(message);
            }
        }

        private static void SendMessageNotification(MessageReq messageReq, bool dontDate = false)
        {
            if (! (GlobalsParameters.Instance.status.ContainsKey(messageReq.Recipient)
                && GlobalsParameters.Instance.status[messageReq.Recipient] != PresenceStatus.Offline))
            {
                var file = userPath + "//" + messageReq.Recipient + "_" + messageReq.Login;
                var msg = messageReq.SendTime + " przez " + messageReq.Login + ":\n" + messageReq.Message;
                Functions.saveFile(file, msg);
            }
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(Constants.Exchange, "topic");

                    var routingKey = string.Format(Constants.keyClientNotificationMessage + messageReq.Recipient + "." + messageReq.Login);
                    var message = new MessageNotification
                    {
                        Message = messageReq.Message,
                        SendTime = dontDate ? new DateTime(2000, 1, 1) : DateTime.Now,
                        Sender = messageReq.Login,
                        Recipient = messageReq.Recipient
                    };

                    var body = message.Serialize();
                    channel.BasicPublish(Constants.Exchange, routingKey, null, body);

                    Console.WriteLine(
                        " [Msg] KEY[{0}] From: {1} '{2}' - To: {3}",
                        routingKey,
                        message.Sender,
                        message.Message,
                        message.Recipient);
                }
            }
        }

        private static void SendStatusNotification(PresenceStatusNotification statusChange)
        {
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(Constants.Exchange, "topic");

                    var routingKey = string.Format(Constants.keyClientNotificationStatus + statusChange.Recipient + ".All");

                    var message = new PresenceStatusNotification
                    {
                        Login = statusChange.Login,
                        PresenceStatus = statusChange.PresenceStatus,
                        Recipient = statusChange.Recipient
                    };

                    var body = message.Serialize();
                    channel.BasicPublish(Constants.Exchange, routingKey, null, body);

                    Console.WriteLine(
                        " [State] KEY[{0}] From: {1} '{2}' - To: {3}",
                        routingKey,
                        statusChange.Login,
                        statusChange.PresenceStatus,
                        statusChange.Recipient);
                    if (!GlobalsParameters.Instance.status.ContainsKey(statusChange.Login))
                    {
                        GlobalsParameters.Instance.status.Add(statusChange.Login, statusChange.PresenceStatus);
                    }
                    else
                    {
                        GlobalsParameters.Instance.status[statusChange.Login] = statusChange.PresenceStatus;
                    }
                }
            }
        }
    }


}