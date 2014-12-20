// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ConversationViewModel.cs" company="">
//   
// </copyright>
// <summary>
//   View model okna konwersacji
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DNAClient.ViewModel
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;

    using DNAClient.ViewModel.Base;

    using DTO;

    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;

    /// <summary>
    /// View model okna konwersacji
    /// </summary>
    public class ConversationViewModel : ViewModelBase
    {
        /// <summary>
        /// Coś od zarządzania eventami i taskami, jeszcze nie ogarnąłem do końca
        /// </summary>
        private static readonly ManualResetEvent FinishEvent = new ManualResetEvent(false);

        // Wiadomość, użytkownik(nadawca), odbiorca 
        
        private string message;

        private string user;

        private string recipient;

        // odebrane wiadomości (do przerobienia na listę lub coś w ten deseń)
        private string received;
        private static ConnectionFactory factory = Constants.ConnectionFactory;
        public ConversationViewModel()
        {
            this.User = GlobalsParameters.Instance.CurrentUser;
            this.SendMessageCommand = new RelayCommand(this.SendMessage);
            this.CloseWindowCommand = new RelayCommand(this.CloseWindow);

            // Uruchomienie zadania, które w tle będzie nasłuchiwać wiadomości przychodzących z serwera
            var ctx = SynchronizationContext.Current;
            Task.Factory.StartNew(() => GetChannel(this, ctx));
        }

        /// <summary>
        /// Konstruktor ustawiający konkretnego odbiorcę
        /// </summary>
        /// <param name="recipient">
        /// Odbiorca
        /// </param>
        public ConversationViewModel(string recipient)
            : this()
        {
            this.Recipient = recipient;
        }

        public string Message
        {
            get
            {
                return this.message;
            }

            set
            {
                this.message = value;
                this.RaisePropertyChanged("Message");
            }
        }

        public string User
        {
            get
            {
                return this.user;
            }

            set
            {
                this.user = value;
                this.RaisePropertyChanged("User");
            }
        }

        public string Recipient
        {
            get
            {
                return this.recipient;
            }

            set
            {
                this.recipient = value;
                this.RaisePropertyChanged("Recipient");
            }
        }

        public string Received
        {
            get
            {
                return this.received;
            }

            set
            {
                this.received = value;
                this.RaisePropertyChanged("Received");
            }
        }

        public RelayCommand SendMessageCommand { get; set; }

        public RelayCommand CloseWindowCommand { get; set; }

        private void CloseWindow(object parameter)
        {
            var window = parameter as Window;

            if (window != null)
            {
                FinishEvent.Set();
                window.Close();
            }
        }

        private void SendMessage(object parameter)
        {
            if (this.Message != null)
            {
                this.Message = this.Message.Trim();
            }

            if (this.Message != String.Empty)
            {
                this.Received += DateTimeOffset.Now + " przez Ja:\n" + this.Message + "\n\n";
                this.SendMessageToQueue();
                this.Message = String.Empty;
            }
        }

        /// <summary>
        /// Metoda tworząca wiadomość z ustawionych property i wysyłąjąca ją do rabbita
        /// Pewnie da się to przerobić tak, żeby to factory i channel tworzyć raz w konstryktorze,a nie za każdy razem tutaj
        /// </summary>
        private void SendMessageToQueue()
        {
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(Constants.Exchange, "topic");

                    var message = new MessageReq
                                      {
                                          Login = this.User,
                                          Message = this.Message,
                                          Recipient = this.Recipient,
                                          SendTime = DateTimeOffset.Now
                                      };

                    var body = message.Serialize();
                    channel.BasicPublish(Constants.Exchange, Constants.keyServerRequestMessage, null, body);
                    Debug.WriteLine("{0} wysłał \"{1}\" do: {2}", this.User, this.Message, this.Recipient);
                }
            }
        }

        /// <summary>
        /// Metoda do obierania wiadomości z serwera
        /// </summary>
        /// <param name="conversationViewModel">
        /// Przekazuje tutaj view model, ponieważ ta metoda musi być statyczna, a trzeba jakoś 
        /// ustawić property od odebranych wiadomości (pewnie nie jest to zbyt dobra praktyka, ale póki co działa :P)
        /// </param>
        public static void GetChannel(ConversationViewModel conversationViewModel, SynchronizationContext ctx)
        {
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(Constants.Exchange, "topic");
                    var queueName = channel.QueueDeclare();

                    Debug.WriteLine(" [Clt] Waiting for request.");

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (_, msg) => ctx.Post( foo_ => Receive(msg, conversationViewModel), null);

                    channel.QueueBind(queueName, Constants.Exchange, string.Format(Constants.keyClientNotification + ".*.{0}", conversationViewModel.User));
                    channel.BasicConsume(queueName, true, consumer);

                    FinishEvent.WaitOne();
                }
            }
        }

        /// <summary>
        /// Metoda wywoływana za każdyn razen gdy serwer coś doda do kolejki i klient to przeczyta
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        /// <param name="conversationViewModel">
        /// The conversation view model.
        /// </param>
        private static void Receive(BasicDeliverEventArgs args, ConversationViewModel conversationViewModel)
        {
            var body = args.Body;
            var routingKey = args.RoutingKey;

            if (routingKey.StartsWith(Constants.keyClientNotification))
            {
                var message = body.DeserializeMessageNotification();
                conversationViewModel.Received += message.SendTime + " przez " + message.Sender + ":\n" + message.Message + "\n\n";

            }
        }
    }

}
