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
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;

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

        static void Main(string[] args)
        {
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
                    byte[] responseBytes = new Response().Serialize();
                    channel.QueueDeclare(Constants.Exchange, false, false, false, null);
                    channel.BasicQos(0, 1, false);
                    var consumer = new QueueingBasicConsumer(channel);
                    channel.BasicConsume(Constants.Exchange, false, consumer);
                    Console.WriteLine(" [x] Awaiting RPC requests");

                    while (true)
                    {
                        var ea = (BasicDeliverEventArgs)consumer.Queue.Dequeue();

                        var body = ea.Body;
                        var props = ea.BasicProperties;
                        var replyProps = channel.CreateBasicProperties();
                        replyProps.CorrelationId = props.CorrelationId;
                        try
                        {
                            Request request = body.DeserializeRequest();
                            // Logowanie
                            if (request.RequestType == AuthRequest.Type.Login)
                            {
                                AuthResponse response = new AuthResponse();
                                AuthRequest authRequest = body.DeserializeAuthRequest();
                                if (db.Login(authRequest.Login, authRequest.Password))
                                {
                                    Console.WriteLine(string.Format("Uzytkownik {0} pomyslnie sie zalogowal.", authRequest.Login));
                                    response.Status = Status.OK;
                                    response.Message = "Udało się zalogować.";
                                }
                                else
                                {
                                    Console.WriteLine(string.Format("Nieudana proba zalogowania przez uzytkownika {0}.", authRequest.Login));
                                    response.Status = Status.Error;
                                    response.Message = "Nie udało się zalogować.";
                                }
                                responseBytes = response.Serialize();
                            }
                            // Rejestracja
                            else if(request.RequestType == AuthRequest.Type.Register)
                            {
                                AuthResponse response = new AuthResponse();
                                AuthRequest authRequest = body.DeserializeAuthRequest();
                                if (db.Register(authRequest.Login, authRequest.Password))
                                {
                                    Console.WriteLine(string.Format("Uzytkownik {0} pomyslnie sie zarejestrował.", authRequest.Login));
                                    response.Status = Status.OK;
                                    response.Message = "Udało się zarejestrować użytkownika.";
                                }
                                else
                                {
                                    Console.WriteLine(string.Format("Nieudana proba zarejestrowania uzytkownika {0}.", authRequest.Login));
                                    response.Status = Status.Error;
                                    response.Message = "Nie udało się zarejestrować użytkownika.";
                                }
                                responseBytes = response.Serialize();
                            }
                            // Dodawanie znajomych
                            else if(request.RequestType == Request.Type.AddFriend)
                            {
                                FriendResponse response = new FriendResponse();
                                FriendRequest friendRequest = body.DeserializeFriendRequest();
                                if (db.AddFriend(friendRequest.Login, friendRequest.FriendLogin))
                                {
                                    Console.WriteLine(string.Format("Uzytkownik {0} pomyslnie dodal kontakt {1}.", friendRequest.Login, friendRequest.FriendLogin));
                                    response.Status = Status.OK;
                                    response.Message = "Udało się dodać kontakt.";
                                }
                                else
                                {
                                    Console.WriteLine(string.Format("Nieudana proba dodania kontaktu {0} przez uzytkownika {1}.", friendRequest.FriendLogin, friendRequest.Login));
                                    response.Status = Status.Error;
                                    response.Message = "Nie udało się dodać kontaktu.";
                                }
                                responseBytes = response.Serialize();
                            }
                            // Usuwanie znajomych
                            else if (request.RequestType == Request.Type.RemoveFriend)
                            {
                                FriendResponse response = new FriendResponse();
                                FriendRequest friendRequest = body.DeserializeFriendRequest();
                                if (db.RemoveFriend(friendRequest.Login, friendRequest.FriendLogin))
                                {
                                    Console.WriteLine(string.Format("Uzytkownik {0} pomyslnie dodal kontakt {1}.", friendRequest.Login, friendRequest.FriendLogin));
                                    response.Status = Status.OK;
                                    response.Message = "Udało się dodać kontakt.";
                                }
                                else
                                {
                                    Console.WriteLine(string.Format("Nieudana proba dodania kontaktu {0} przez uzytkownika {1}.", friendRequest.FriendLogin, friendRequest.Login));
                                    response.Status = Status.Error;
                                    response.Message = "Nie udało się dodać kontaktu.";
                                }
                                responseBytes = response.Serialize();
                            }
                            // Pobieranie listy znajomych
                            else if (request.RequestType == Request.Type.GetFriends)
                            {
                                FriendResponse response = new FriendResponse();
                                FriendRequest friendRequest = body.DeserializeFriendRequest();
                                List<string> friends = db.GetFriends(friendRequest.Login);
                                if (friends.Count > 0)
                                {
                                    Console.WriteLine(string.Format("Uzytkownik {0} pomyslnie pobral kontakty.", friendRequest.Login));
                                    response.Status = Status.OK;
                                    response.friendsList = friends;
                                    response.Message = "Udało się pobrac kontakty.";
                                }
                                else
                                {
                                    Console.WriteLine(string.Format("Nie pobrano zadnych kontaktow dla uzytkownika {0}.", friendRequest.Login));
                                    response.Status = Status.Error;
                                    response.friendsList = friends;
                                    response.Message = "Nie pobrano żadnych kontaków.";
                                }
                                responseBytes = response.Serialize();
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("RPC Error: " + e.Message);
                        }
                        finally
                        {
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

        private static void SendMessageNotification(MessageReq messageReq)
        {
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(Constants.Exchange, "topic");

                    var routingKey = string.Format(Constants.keyClientNotificationMessage + messageReq.Recipient + "." + messageReq.Login);
                    var message = new MessageNotification
                    {
                        Message = messageReq.Message,
                        SendTime = DateTime.Now,
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
                }
            }
        }
    }


}