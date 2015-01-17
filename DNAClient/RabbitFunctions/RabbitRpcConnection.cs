// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RabbitRpcConnection.cs" company="">
//   
// </copyright>
// <summary>
//   Klasa zapewniająca wywołanie metod request-response
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DNAClient.RabbitFunctions
{
    using System;

    using DTO;

    using RabbitMQ.Client;

    /// <summary>
    /// Klasa zapewniająca wywołanie metod request-response 
    /// </summary>
    public class RabbitRpcConnection
    {
        private readonly IConnection connection;
        private readonly IModel channel;
        private readonly string replyQueueName;
        private readonly QueueingBasicConsumer consumer;

        public RabbitRpcConnection()
        {
            var factory = Constants.ConnectionFactory;
            this.connection = factory.CreateConnection();
            this.channel = this.connection.CreateModel();
            this.replyQueueName = this.channel.QueueDeclare();
            this.consumer = new QueueingBasicConsumer(this.channel);
            this.channel.BasicConsume(this.replyQueueName, true, this.consumer);
        }

        /// <summary>
        /// Logowanie i rejestracja na serwer
        /// </summary>
        /// <param name="serializedRequest">
        /// The serialized request.
        /// </param>
        /// <returns>
        /// The <see cref="AuthResponse"/>.
        /// </returns>
        public AuthResponse AuthCall(byte[] serializedRequest)
        {
            var corrId = Guid.NewGuid().ToString();
            var props = this.GetBasicProperties(corrId);

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

        /// <summary>
        /// Zarządzanie kontaktami (dodawanie, usuwanie, pobieranie)
        /// </summary>
        /// <param name="serializedRequest">
        /// The serialized request.
        /// </param>
        /// <returns>
        /// The <see cref="FriendResponse"/>.
        /// </returns>
        public FriendResponse FriendCall(byte[] serializedRequest)
        {
            var corrId = Guid.NewGuid().ToString();
            var properties = this.GetBasicProperties(corrId);
            this.channel.BasicPublish("", Constants.Exchange, properties, serializedRequest);

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
        /// Pobieranie wiadomości wysłanych do użytkownika podczas gdy był on offline
        /// </summary>
        /// <param name="serializedRequest">
        /// Serializowany request
        /// </param>
        public void OldMessagesCall(byte[] serializedRequest)
        {
            var corrId = Guid.NewGuid().ToString();
            var properties = this.GetBasicProperties(corrId);
            this.channel.BasicPublish("", Constants.Exchange, properties, serializedRequest);
        }

        /// <summary>
        /// Metoda zamykająca rabbitowe połączenie 
        /// </summary>
        public void Close()
        {
            this.connection.Close();
        }

        /// <summary>
        /// Metoda ustawiająca dane połączenia dla publikacji requestu
        /// </summary>
        /// <param name="correlationGuid">
        /// Guid ustawiany dla jednorazowego wywołania funkcji
        /// </param>
        /// <returns>
        /// The <see cref="IBasicProperties"/>.
        /// </returns>
        private IBasicProperties GetBasicProperties(string correlationGuid)
        {
            var props = this.channel.CreateBasicProperties();
            props.ReplyTo = this.replyQueueName;
            props.CorrelationId = correlationGuid;

            return props;
        }
    }
}
