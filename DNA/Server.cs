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

    using DTO;

    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;

    /// <summary>
    /// Serwer obsługujący wiadomości przechodzące przez kolejki rabbita
    /// </summary>
    public class Server
    {
        private static readonly ManualResetEvent FinishEvent = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            Task.Factory.StartNew(GetChannel);
            Console.WriteLine("Starting server...");

            Console.ReadLine();
            FinishEvent.Set();
        }

        public static void GetChannel()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare("ServerExchange", "topic");
                    var queueName = channel.QueueDeclare();

                    Console.WriteLine(" [Srv] Waiting for request. " + "To exit press CTRL+C");

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (_, msg) => Receive(msg);
                    channel.QueueBind(queueName, "ServerExchange", "server.request.*");
                    channel.BasicConsume(queueName, true, consumer);

                    FinishEvent.WaitOne();
                }
            }
        }

        private static void Receive(BasicDeliverEventArgs args)
        {
            var body = args.Body;
            var routingKey = args.RoutingKey;

            if (routingKey == "server.request.message")
            {
                var message = body.DeserializeMessageReq();
                Console.WriteLine(
                    " [Msg] '{0}':'{1} - {2}'",
                    routingKey,
                    message.Login,
                    message.Message);
                SendMessageNotification(message);
            }

            if (routingKey == "server.request.authorization")
            {
                var authorizationRequest = body.DeserializeAuthResponse();
                Console.WriteLine(" [Auth] '{0}':'{1}'", routingKey, authorizationRequest.Login);
            }
        }

        private static void SendMessageNotification(MessageReq messageReq)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare("ClientExchange", "topic");

                    var routingKey = string.Format("client.notification.message.{0}", messageReq.Recipient);
                    var message = new MessageNotification
                    {
                        Message = messageReq.Message,
                        SendTime = DateTime.Now,
                        Sender = messageReq.Login,
                        Recipient = messageReq.Recipient
                    };

                    var body = message.Serialize();
                    channel.BasicPublish("ClientExchange", routingKey, null, body);

                    Console.WriteLine(
                        " [Msg] KEY[{0}] From: {1} '{2}' - To: {3}",
                        routingKey,
                        message.Sender,
                        message.Message,
                        message.Recipient);
                }
            }
        }
    }


}
