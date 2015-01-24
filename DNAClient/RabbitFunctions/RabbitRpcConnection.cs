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
    using RabbitMQ.Client.Events;

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
            var authResponse = new AuthResponse();

            this.channel.BasicPublish("", Constants.Exchange, props, serializedRequest);

            try
            {
                BasicDeliverEventArgs ea;
                if (!this.consumer.Queue.Dequeue(Constants.milisecondsTimeout, out ea))
                {
                    this.channel.QueuePurge(Constants.Exchange);
                    authResponse.Message = "Serwer nie odpowiada, prosimy spróbować ponownie.";
                    authResponse.Status = Status.Error;
                    return authResponse;
                }

                if (ea != null && ea.BasicProperties.CorrelationId == corrId)
                {
                    authResponse = ea.Body.DeserializeAuthResponse();
                    return authResponse;
                }
            }
            catch (Exception e)
            {
                authResponse.Message = "Niespodziewany wyjątek: \n\n" + e.Message;
                authResponse.Status = Status.Error;
            }

            return authResponse;
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
            var friendsResponse = new FriendResponse();

            this.channel.BasicPublish("", Constants.Exchange, properties, serializedRequest);

            try
            {
                BasicDeliverEventArgs ea;
                if (!this.consumer.Queue.Dequeue(Constants.milisecondsTimeout, out ea))
                {
                    this.channel.QueuePurge(Constants.Exchange);
                    friendsResponse.Message = "Serwer nie odpowiada, prosimy spróbować ponownie.";
                    friendsResponse.Status = Status.Error;
                    return friendsResponse;
                }

                if (ea != null && ea.BasicProperties.CorrelationId == corrId)
                {
                    friendsResponse = ea.Body.DeserializeFriendResponse();
                    return friendsResponse;
                }
            }
            catch (Exception e)
            {
                friendsResponse.Message = "Niespodziewany wyjątek: \n\n" + e.Message;
                friendsResponse.Status = Status.Error;
            }

            return friendsResponse;
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
