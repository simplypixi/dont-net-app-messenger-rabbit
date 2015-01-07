// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindowViewModel.cs" company="">
//   
// </copyright>
// <summary>
//   View model głównego okna
// </summary>
// --------------------------------------------------------------------------------------------------------------------
using System.Collections.ObjectModel;

namespace DNAClient.ViewModel
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Linq;

    using DNAClient.ViewModel.Base;
    using DNAClient.View;

    using DTO;

    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;
    /// <summary>
    /// ViewModel głownego okna
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        
        private static readonly ManualResetEvent FinishEvent = new ManualResetEvent(false);
        private string recipient, currentUser;
        private string selectedStatus;
        private Contact selectedContact = new Contact() { Name = null };
        private string userPath;

        private static ConnectionFactory factory = Constants.ConnectionFactory;

        public MainWindowViewModel()
        {
            this.userPath = Constants.userPath;
            this.CurrentUser = GlobalsParameters.Instance.CurrentUser;
            this.NewConversationWindowCommand = new RelayCommand(this.NewConversationWindow);
            this.OpenHistoryCommand = new RelayCommand(this.OpenHistory);
            this.addFriendCommand = new RelayCommand(this.addNewFriend);
            this.CloseWindowCommand = new RelayCommand(this.CloseWindow);


            //Dodanie na sztywno kontaktów; Później trzeba dodać wczytywanie kontaktów z bazy lokalnej lub zdalnej MSSQL
            Contacts.Add(new Contact() { Name = "Maciek" });
            Contacts.Add(new Contact() { Name = "Maciej" });
            Contacts.Add(new Contact() { Name = "Mariusz" });
            Contacts.Add(new Contact() { Name = "Darek" });

            // Uruchomienie zadania, które w tle będzie nasłuchiwać wiadomości przychodzących z serwera
            var ctx = SynchronizationContext.Current;
            Task.Factory.StartNew(() => GetChannel(ctx));
        }



        /// <summary>
        /// Property odbiorcy do zbindowania w xamlu
        /// </summary>
        /// 
        public string CurrentUser
        {
            get
            {
                return this.currentUser;
            }

            set
            {
                this.currentUser = value;
                this.RaisePropertyChanged("CurrentUser");
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

        public Contact SelectedContact {
            get
            {
                return this.selectedContact;
            }

            set
            {
                this.selectedContact = value;
                this.RaisePropertyChanged("SelectedContact");
            }

        }

        public string SelectedStatus
        {
            get
            {
                return this.selectedStatus;
            }

            set
            {
                this.selectedStatus = value;
                Console.WriteLine(selectedStatus);
                this.RaisePropertyChanged("SelectedStatus");
                this.SendStatus();
            }

        }

        /// <summary>
        /// Komenda otwarcia historii rozmowy
        /// </summary>
        public RelayCommand OpenHistoryCommand { get; set; }

        /// <summary>
        /// Metoda otwierająca historię rozmowy
        /// </summary>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        private void OpenHistory(object parameter)
        {
            var historyFile = this.userPath + "//" + this.SelectedContact.Name;
            System.Diagnostics.Process.Start(@historyFile);
        }

        /// <summary>
        /// Komenda otwarcia nowego okna konweracji
        /// </summary>
        public RelayCommand NewConversationWindowCommand { get; set; }

        /// <summary>
        /// Metoda otwierająca nowe okno konwersacji
        /// </summary>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        private void NewConversationWindow(object parameter)
        {
            if (!String.IsNullOrEmpty(this.SelectedContact.Name))
            {
                ProductionWindowFactory.CreateConversationWindow(this.SelectedContact.Name);
            }
        }

         public RelayCommand addFriendCommand { get; set; }
        private string friendName;

        public ObservableCollection<Contact> contacts =
            new ObservableCollection<Contact>();

        public ObservableCollection<Contact> Contacts
        {
            get {
                return contacts; }
            set { 
                contacts = value;
                RaisePropertyChanged("Contacts");
            }
        }

        public string Friend
        {
            get
            {
                return this.friendName;
            }

            set
            {
                this.friendName = value;
                this.RaisePropertyChanged("Friend");
            }
        }

        private void addNewFriend(object parameter){
            if (String.IsNullOrEmpty(this.Friend)) { 
                MessageBox.Show("Aby dodać kontakt, należy wpisać jego nazwę. Spróbuj ponownie.", "Błąd dodawania użytkowika");
            } else {
                bool check = false;
                foreach(Contact element in this.Contacts){
                    if (element.Name == this.Friend)
                        check = true;
                }

                if (!check)
                {
                    Contacts.Add(new Contact() { Name = this.Friend });
                }
                else
                {
                   MessageBox.Show("Taki kontakt już istnieje!", "Błąd dodawania użytkowika");
                }
            }

        }

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

        private void SendStatus()
        {
            if (!String.IsNullOrEmpty(this.SelectedStatus.Trim()))
            {
                foreach (Contact element in this.Contacts)
                {
                    this.SendStatusToQueue(element.Name);
                }
            }
        }

        /// <summary>
        /// Metoda tworząca informację o zmianie stanu i wysyłająca ją do rabbita
        /// </summary>
        private void SendStatusToQueue(string recip)
        {
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(Constants.Exchange, "topic");

                    PresenceStatus status = PresenceStatus.Offline;
                    if(this.SelectedStatus == "Zajęty")
                        status = PresenceStatus.Afk;
                    if(this.SelectedStatus == "Dostępny")
                        status = PresenceStatus.Online;

                    var message = new PresenceStatusNotification
                    {
                        Login = this.CurrentUser,
                        PresenceStatus = status,
                        Recipient = recip
                    };

                    var body = message.Serialize();
                    channel.BasicPublish(Constants.Exchange, Constants.keyServerRequestStatus, null, body);
                    //Debug.WriteLine("{0} zmienił status na \"{1}\" i poinformował o tym: {2}", this.CurrentUser, this.SelectedStatus, recip);
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
        public void GetChannel(SynchronizationContext ctx)
        {
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(Constants.Exchange, "topic");
                    var queueName = channel.QueueDeclare();

                    Debug.WriteLine(" [Clt] Waiting for request.");

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (_, msg) => ctx.Post(foo_ => Receive(msg), null);

                    channel.QueueBind(queueName, Constants.Exchange, string.Format(Constants.keyClientNotification + ".*.{0}", this.CurrentUser));
                    channel.BasicConsume(queueName, true, consumer);

                    FinishEvent.WaitOne();
                }
            }
        }

        // Zmienia status użytkownika na liście kontaktów
        private void Receive(BasicDeliverEventArgs args)
        {
            var body = args.Body;
            var routingKey = args.RoutingKey;

            if (routingKey.StartsWith(Constants.keyClientNotification))
            {
                var message = body.DeserializePresenceStatusNotification();

                var contact = Contacts.Where(X => X.Name == message.Login).FirstOrDefault();
                if (contact != null)
                {
                    if (message.PresenceStatus.Equals(PresenceStatus.Offline))
                        contact.State = "#FFD1D1D1";
                    if (message.PresenceStatus.Equals(PresenceStatus.Online))
                        contact.State = "Green";
                    if (message.PresenceStatus.Equals(PresenceStatus.Afk))
                        contact.State = "Red";
                }
                //Console.WriteLine("dupa {0}, od {1}, do {2}", message.PresenceStatus, message.Login, message.Recipient);

            }
        }
    }
}
