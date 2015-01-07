using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DNAClient.RabbitFunctions
{
    using DTO;

    using RabbitMQ.Client;

    class RpcRegistration
    {
        private readonly IConnection connection;
        private readonly IModel channel;
        private readonly string replyQueueName;
        private readonly QueueingBasicConsumer consumer;

        /// <summary>
        /// Konstruktor inicjalizujący paramtery połączenia
        /// </summary>
        public RpcRegistration()
        {
            var factory = Constants.ConnectionFactory;
            this.connection = factory.CreateConnection();
            this.channel = this.connection.CreateModel();
            this.replyQueueName = this.channel.QueueDeclare();
            this.consumer = new QueueingBasicConsumer(this.channel);
            this.channel.BasicConsume(this.replyQueueName, true, this.consumer);
        }

        /// <summary>
        /// Metoda wywołująca próbę rejestracji
        /// </summary>
        /// <param name="login">
        /// Login użytkownika
        /// </param>
        /// <param name="password">
        /// Hasło użytkownika
        /// </param>
        /// <param name="confirmedPassword">
        /// Potwierdzenie hasła
        /// </param>
        /// <returns>
        /// True/False w zależności od tego,
        /// czy logowanie na serwer się powiodło
        /// </returns>
        public CreateUserResponse Call(string login, string password, string confirmedPassword)
        {
            var corrId = Guid.NewGuid().ToString();
            var props = this.channel.CreateBasicProperties();
            props.ReplyTo = this.replyQueueName;
            props.CorrelationId = corrId;

            var createUserRequest = new CreateUserRequest
            {
                Login = login,
                Password = password,
                ConfirmedPassword = confirmedPassword
            };

            var body = createUserRequest.Serialize();
            this.channel.BasicPublish("", Constants.Exchange, props, body);

            while (true)
            {
                var ea = this.consumer.Queue.Dequeue();
                if (ea.BasicProperties.CorrelationId == corrId)
                {
                    var authResponse = ea.Body.DeserializeCreateUserResponse();
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
