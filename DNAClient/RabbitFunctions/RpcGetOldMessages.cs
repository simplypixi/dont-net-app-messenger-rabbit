// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RpcLogin.cs" company="">
//   
// </copyright>
// <summary>
//   Klasa zawierająca metody służące do logowania na serwer DNA
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DNAClient.RabbitFunctions
{
    using System;
    using DTO;
    using RabbitMQ.Client;

    /// <summary>
    /// Klasa zawierająca metody służące do logowania na serwer DNA
    /// </summary>
    class RpcGetOldMessages
    {
        private readonly IConnection connection;
        private readonly IModel channel;
        private readonly string replyQueueName;
        private readonly QueueingBasicConsumer consumer;

        /// <summary>
        /// Konstruktor inicjalizujący paramtery połączenia
        /// </summary>
        public RpcGetOldMessages()
        {
            var factory = Constants.ConnectionFactory;
            this.connection = factory.CreateConnection();
            this.channel = this.connection.CreateModel();
            this.replyQueueName = this.channel.QueueDeclare();
            this.consumer = new QueueingBasicConsumer(this.channel);
            this.channel.BasicConsume(this.replyQueueName, true, this.consumer);
        }

        /// <summary>
        /// Metoda wywołująca próbę logowania
        /// </summary>
        /// <param name="login">
        /// Login użytkownika
        /// </param>
        /// <param name="password">
        /// Hasło użytkownika
        /// </param>
        /// <returns>
        /// True/False w zależności od tego,
        /// czy logowanie na serwer się powiodło
        /// </returns>
        public AuthResponse Call(string login)
        {
            var corrId = Guid.NewGuid().ToString();
            var props = this.channel.CreateBasicProperties();
            props.ReplyTo = this.replyQueueName;
            props.CorrelationId = corrId;

            var authRequest = new AuthRequest
            {
                Login = login,
                Type = AuthRequest.AuthorizationType.GetOldMessages
            };

            var body = authRequest.Serialize();
            this.channel.BasicPublish("", Constants.Exchange, props, body);

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
        /// Metoda zamykająca rabbitowe połączenie 
        /// </summary>
        public void Close()
        {
            this.connection.Close();
        }
    }
}
