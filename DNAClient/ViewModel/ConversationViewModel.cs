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
    using System.IO;
    using System.Text;

    using DNAClient.ViewModel.Base;
    using DNAClient.View;

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
        private string userPath;
        public ConversationViewModel()
        {
            this.userPath = Constants.userPath;
            this.User = GlobalsParameters.Instance.CurrentUser;
            this.SendMessageCommand = new RelayCommand(this.SendMessage);
            this.CloseWindowCommand = new RelayCommand(this.CloseWindow);

            // Uruchomienie zadania, które w tle będzie nasłuchiwać wiadomości przychodzących z serwera
            var ctx = SynchronizationContext.Current;
            Task.Factory.StartNew(() => GetChannel(ctx));
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

        private void AddToHistory(string message)
        {
            if (this.Recipient == null)
            {
                return;
            }
            var historyFile = this.userPath + "//" + this.Recipient;
            if (!File.Exists(historyFile))
            {
                FileStream aFile = new FileStream(historyFile, FileMode.Create, FileAccess.Write);
                StreamWriter sw = new StreamWriter(aFile);
                sw.WriteLine(message);
                sw.Close();
                aFile.Close();
            }
            else
            {
                FileStream aFile = new FileStream(historyFile, FileMode.Append, FileAccess.Write);
                StreamWriter sw = new StreamWriter(aFile);
                sw.WriteLine(message);
                sw.Close();
                aFile.Close();
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
                var msg = DateTimeOffset.Now + " przez Ja:\n" + this.Message + "\n";
                this.Received += msg + "\n";
                this.SendMessageToQueue();
                AddToHistory(msg);
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
        private void GetChannel(SynchronizationContext ctx)
        {
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(Constants.Exchange, "topic");
                    var queueName = channel.QueueDeclare();

                    Debug.WriteLine(" [Clt] Waiting for request.");

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (_, msg) => ctx.Post( foo_ => Receive(msg), null);

                    channel.QueueBind(queueName, Constants.Exchange, string.Format(Constants.keyClientNotification + ".*.{0}.{1}", this.User, this.Recipient));
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
        /// 
        private void NewNotificationWindow(string sender, string type)
        {
            ProductionWindowFactory.CreateNotificationWindow(sender, type);
        }
        private void Receive(BasicDeliverEventArgs args)
        {
            var body = args.Body;
            var routingKey = args.RoutingKey;

            if (routingKey.StartsWith(Constants.keyClientNotification + ".message"))
            {
                var message = body.DeserializeMessageNotification();
                var msg = message.SendTime + " przez " + message.Sender + ":\n" + message.Message + "\n";
                this.AddToHistory(msg);
                this.Received += msg + "\n";

                //Testowe odpalenie okna powiadomień
                this.NewNotificationWindow(message.Sender, "message");
            }
        }
    }

}
