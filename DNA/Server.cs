// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Server.cs" company="">
//   
// </copyright>
// <summary>
//   Serwer obsługujący wiadomości przechodzące przez kolejki rabbita
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

    using DTO;
    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;

    /// <summary>
    /// Serwer obsługujący wiadomości przechodzące przez kolejki rabbita
    /// </summary>
    public class Server
    {
        /// <summary>
        /// The finish event.
        /// </summary>
        private static readonly ManualResetEvent FinishEvent = new ManualResetEvent(false);

        /// <summary>
        /// The factory.
        /// </summary>
        private static ConnectionFactory factory = Constants.ConnectionFactory;

        /// <summary>
        /// The db.
        /// </summary>
        private static DatabaseHelper db = new DatabaseHelper();

        /// <summary>
        /// The user path.
        /// </summary>
        private static string userPath;

        /// <summary>
        /// The global.
        /// </summary>
        private static readonly GlobalsParameters Global = new GlobalsParameters(new Dictionary<string, PresenceStatus>());

        /// <summary>
        /// The main.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        public static void Main(string[] args)
        {
            userPath = Constants.userPath;
            Task.Factory.StartNew(GetChannel);
            Task.Factory.StartNew(GetChannelRpc);

            Console.WriteLine("Starting server...");

            Console.ReadLine();
            FinishEvent.Set();
        }

        /// <summary>
        /// The get channel.
        /// </summary>
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

        /// <summary>
        /// The get channel rpc.
        /// </summary>
        public static void GetChannelRpc()
        {
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    byte[] responseBytes = new Response().Serialize();
                    channel.QueueDeclare(Constants.Exchange, false, false, false, null);
                    channel.BasicQos(0, 1, false);
                    var consumer = new QueueingBasicConsumer(channel);
                    channel.BasicConsume(Constants.Exchange, false, consumer);
                    Console.WriteLine(" [x] Awaiting RPC requests");

                    while (true)
                    {
                        var ea = consumer.Queue.Dequeue();

                        var body = ea.Body;
                        var props = ea.BasicProperties;
                        var replyProps = channel.CreateBasicProperties();
                        replyProps.CorrelationId = props.CorrelationId;
                        try
                        {
                            var request = body.DeserializeRequest();

                            switch (request.RequestType)
                            {
                                case Request.Type.Login:
                                    {
                                        var response = new AuthResponse();
                                        var authRequest = body.DeserializeAuthRequest();
                                        if (db.Login(authRequest.Login, authRequest.Password))
                                        {
                                            Global.Status.Add(request.Login, PresenceStatus.Online);
                                            Console.WriteLine("Uzytkownik {0} pomyslnie sie zalogowal.", authRequest.Login);
                                            response.Status = Status.OK;
                                            response.Message = "Udało się zalogować.";
                                        }
                                        else
                                        {
                                            Console.WriteLine("Nieudana proba zalogowania przez uzytkownika {0}.", authRequest.Login);
                                            response.Status = Status.Error;
                                            response.Message = "Nie udało się zalogować.";
                                        }

                                        responseBytes = response.Serialize();
                                    }

                                    break;
                                case Request.Type.Register:
                                    {
                                        var response = new AuthResponse();
                                        var authRequest = body.DeserializeAuthRequest();
                                        if (db.Register(authRequest.Login, authRequest.Password))
                                        {
                                            Global.Status.Add(request.Login, PresenceStatus.Online);
                                            Console.WriteLine("Uzytkownik {0} pomyslnie sie zarejestrował.", authRequest.Login);
                                            response.Status = Status.OK;
                                            response.Message = "Udało się zarejestrować użytkownika.";
                                        }
                                        else
                                        {
                                            Console.WriteLine("Nieudana proba zarejestrowania uzytkownika {0}.", authRequest.Login);
                                            response.Status = Status.Error;
                                            response.Message = "Nie udało się zarejestrować użytkownika.";
                                        }

                                        responseBytes = response.Serialize();
                                    }

                                    break;
                                case Request.Type.AddFriend:
                                    {
                                        var response = new FriendResponse();
                                        var friendRequest = body.DeserializeFriendRequest();

                                        if (db.AddFriend(friendRequest.Login, friendRequest.FriendLogin))
                                        {
                                            Console.WriteLine("Uzytkownik {0} pomyslnie dodal kontakt {1}.", friendRequest.Login, friendRequest.FriendLogin);
                                            response.Status = Status.OK;
                                            response.Message = "Udało się dodać kontakt.";
                                        }
                                        else
                                        {
                                            Console.WriteLine("Nieudana proba dodania kontaktu {0} przez uzytkownika {1}.", friendRequest.FriendLogin, friendRequest.Login);
                                            response.Status = Status.Error;
                                            response.Message = "Nie udało się dodać kontaktu.";
                                        }

                                        responseBytes = response.Serialize();
                                    }

                                    break;
                                case Request.Type.RemoveFriend:
                                    {
                                        var response = new FriendResponse();
                                        var friendRequest = body.DeserializeFriendRequest();
                                        if (db.RemoveFriend(friendRequest.Login, friendRequest.FriendLogin))
                                        {
                                            Console.WriteLine("Uzytkownik {0} pomyslnie dodal kontakt {1}.", friendRequest.Login, friendRequest.FriendLogin);
                                            response.Status = Status.OK;
                                            response.Message = "Udało się dodać kontakt.";
                                        }
                                        else
                                        {
                                            Console.WriteLine("Nieudana proba dodania kontaktu {0} przez uzytkownika {1}.", friendRequest.FriendLogin, friendRequest.Login);
                                            response.Status = Status.Error;
                                            response.Message = "Nie udało się dodać kontaktu.";
                                        }

                                        responseBytes = response.Serialize();
                                    }

                                    break;
                                case Request.Type.GetFriends:
                                    {
                                        var response = new FriendResponse();
                                        var friendRequest = body.DeserializeFriendRequest();
                                        var friends = db.GetFriends(friendRequest.Login);
                                        if (friends != null)
                                        {
                                            Console.WriteLine("Uzytkownik {0} pomyslnie pobral kontakty.", friendRequest.Login);
                                            response.Status = Status.OK;
                                            response.friendsList = friends;
                                            response.Message = "Udało się pobrac kontakty.";
                                        }
                                        else
                                        {
                                            Console.WriteLine("Nie udalo sie pobrac kontaktow dla uzytkownika {0}.", friendRequest.Login);
                                            response.Status = Status.Error;
                                            response.friendsList = null;
                                            response.Message = "Nie pobrano żadnych kontaków.";
                                        }

                                        responseBytes = response.Serialize();
                                    }

                                    break;
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("RPC Error: " + e.Message);
                        }
                        finally
                        {
                            channel.BasicPublish(string.Empty, props.ReplyTo, replyProps, responseBytes);
                            channel.BasicAck(ea.DeliveryTag, false);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The send old messages.
        /// </summary>
        /// <param name="login">
        /// The login.
        /// </param>
        public static void SendOldMessages(string login)
        {
            var filePaths = Directory.GetFiles(userPath);
            foreach (var t in filePaths)
            {
                if (!Regex.IsMatch(t, string.Format(@"{0}_(.*)", login)))
                {
                    continue;
                }

                var regex = Regex.Match(t, string.Format(@"{0}_(.*)", login));
                var group = regex.Groups[1];
                var sender = @group.Value;
                var file = userPath + "//" + login + "_" + sender;
                var streamReader = new StreamReader(file);
                var message = streamReader.ReadToEnd();
                streamReader.Close();
                var messageReq = new MessageReq { Recipient = login, Login = sender, Message = message };
                File.Delete(file);
                SendMessageNotification(messageReq, true);
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

            if (routingKey == Constants.keyServerRequestGetOld)
            {
                var message = body.DeserializeRequest();

                Console.WriteLine(
                    " [GetOldMessages] '{0}':'{1}'",
                    routingKey,
                    message.Login);
                SendOldMessages(message.Login);
            }

            if (routingKey == Constants.keyServerRequestLogOff)
            {
                var message = body.DeserializeRequest();

                Console.WriteLine(
                    " [LogOff] '{0}':'{1}'",
                    routingKey,
                    message.Login);
                Global.Status.Remove(message.Login);
            }
        }

        /// <summary>
        /// The send message notification.
        /// </summary>
        /// <param name="messageReq">
        /// The message req.
        /// </param>
        /// <param name="dontDate">
        /// The dont date.
        /// </param>
        private static void SendMessageNotification(MessageReq messageReq, bool dontDate = false)
        {
            if (!(Global.Status.ContainsKey(messageReq.Recipient)
                && Global.Status[messageReq.Recipient] != PresenceStatus.Offline))
            {
                var file = userPath + "//" + messageReq.Recipient + "_" + messageReq.Login;
                var msg = messageReq.SendTime + " przez " + messageReq.Login + ":\n" + messageReq.Message + "\n";
                Functions.saveFile(file, msg);
                return;
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
                        Recipient = messageReq.Recipient,
                        Attachment = messageReq.Attachment
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

        /// <summary>
        /// The send status notification.
        /// </summary>
        /// <param name="statusChange">
        /// The status change.
        /// </param>
        private static void SendStatusNotification(PresenceStatusNotification statusChange)
        {
            if (Global.Status.ContainsKey(statusChange.Login)
                && Global.Status[statusChange.Login] == PresenceStatus.Offline
                && statusChange.PresenceStatus != PresenceStatus.Offline)
            {
                Global.Status[statusChange.Login] = statusChange.PresenceStatus;
                SendOldMessages(statusChange.Login);
            }

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
                    if (!Global.Status.ContainsKey(statusChange.Login))
                    {
                        Global.Status.Add(statusChange.Login, statusChange.PresenceStatus);
                    }
                    else
                    {
                        Global.Status[statusChange.Login] = statusChange.PresenceStatus;
                    }
                }
            }
        }
    }
}
