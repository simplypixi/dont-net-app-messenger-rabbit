using System;
using DTO;
using RabbitMQ.Client;

namespace DNAClient.RabbitFunctions
{
    class RpcWay
    {
        private readonly IConnection connection;
        private readonly IModel channel;
        private readonly string replyQueueName;
        private readonly QueueingBasicConsumer consumer;

        public RpcWay()
        {
            var factory = Constants.ConnectionFactory;
            this.connection = factory.CreateConnection();
            this.channel = this.connection.CreateModel();
            this.replyQueueName = this.channel.QueueDeclare();
            this.consumer = new QueueingBasicConsumer(this.channel);
            this.channel.BasicConsume(this.replyQueueName, true, this.consumer);
        }

        // Logowanie i rejestracja
        public AuthResponse AuthCall(byte[] serializedRequest)
        {
            var corrId = Guid.NewGuid().ToString();
            var props = this.channel.CreateBasicProperties();
            props.ReplyTo = this.replyQueueName;
            props.CorrelationId = corrId;

            this.channel.BasicPublish("", Constants.Exchange, props, serializedRequest);

            while (true)
            {
                var ea = this.consumer.Queue.Dequeue();
                if (ea.BasicProperties.CorrelationId == corrId)
                {
                    var authResponse = ea.Body.DeserializeAuthResponse();
                    return authResponse;
                }
            }
        }

        // Dodawanie / Usuwanie kontaktów + pobieranie kontaktów
        public FriendResponse FriendCall(byte[] serializedRequest)
        {
            var corrId = Guid.NewGuid().ToString();
            var props = this.channel.CreateBasicProperties();
            props.ReplyTo = this.replyQueueName;
            props.CorrelationId = corrId;

            this.channel.BasicPublish("", Constants.Exchange, props, serializedRequest);

            while (true)
            {
                var ea = this.consumer.Queue.Dequeue();
                if (ea.BasicProperties.CorrelationId == corrId)
                {
                    var friendResponse = ea.Body.DeserializeFriendResponse();
                    return friendResponse;
                }
            }
        }

        /// <summary>
        /// Metoda zamykająca rabbitowe połączenie 
        /// </summary>
        public void Close()
        {
            this.connection.Close();
        }
    }
}
