﻿// --------------------------------------------------------------------------------------------------------------------
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
    /// ViewModel głonego okna
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        
        private static readonly ManualResetEvent FinishEvent = new ManualResetEvent(false);
        private string recipient, currentUser;
        private string selectedStatus;
        private Contact selectedContact = new Contact() { Name = null };
        private bool justLogin = true;
        private bool loadedContacts = false;

        private static ConnectionFactory factory = Constants.ConnectionFactory;

        public MainWindowViewModel()
        {
            this.CurrentUser = GlobalsParameters.Instance.CurrentUser;
            this.NewConversationWindowCommand = new RelayCommand(this.NewConversationWindow);
            this.addFriendCommand = new RelayCommand(this.addNewFriend);
            this.CloseWindowCommand = new RelayCommand(this.CloseWindow);

            //Dodanie na sztywno kontaktów; Później trzeba dodać wczytywanie kontaktów z bazy lokalnej lub zdalnej MSSQL
                Contacts.Add(new Contact() { Name = "Maciek" });
                Contacts.Add(new Contact() { Name = "Maciej" });
                Contacts.Add(new Contact() { Name = "Mariusz" });
                Contacts.Add(new Contact() { Name = "Darek" });

                var contact = Contacts.Where(X => X.Name == this.CurrentUser).FirstOrDefault();
                Contacts.Remove(contact);

            // Uruchomienie zadania, które w tle będzie nasłuchiwać wiadomości przychodzących z serwera
            var ctx = SynchronizationContext.Current;
            Task.Factory.StartNew(() => this.GetChannelStatus(ctx));

            this.SelectedStatus = "Zalogowany";
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
                this.RaisePropertyChanged("SelectedStatus");
                this.SendStatus();
            }

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
                Console.WriteLine(this.SelectedStatus);
                this.SelectedStatus = "Niedostępny";
                window.Close();
            }
        }

        private void SendStatus()
        {
            string state = "";
         
                if (!String.IsNullOrEmpty(this.SelectedStatus))
                    state = this.SelectedStatus.Trim();
                if (!String.IsNullOrEmpty(state))
                {
                    foreach (Contact element in this.Contacts)
                    {
                       
                           this.SendStatusToQueue(element.Name);
                    }
                }
                if (this.SelectedStatus == "Zalogowany")
                {
                    this.SelectedStatus = "Dostępny";
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
                   PresenceStatus status = PresenceStatus.Login;

                    if (this.SelectedStatus == "Zajęty")
                        status = PresenceStatus.Afk;
                    if (this.SelectedStatus == "Dostępny")
                        status = PresenceStatus.Online;
                    if (this.SelectedStatus == "Niedostępny")
                        status = PresenceStatus.Offline;

                    if (this.SelectedStatus == "Zalogowany")
                    {
                        status = PresenceStatus.Login;
                    }

                    Console.WriteLine("Dodano w takcie wysyłanie" + status);
                    var message = new PresenceStatusNotification
                    {
                        Login = this.CurrentUser,
                        PresenceStatus = status,
                        Recipient = recip
                    };

                    var body = message.Serialize();
                    channel.BasicPublish(Constants.Exchange, Constants.keyServerRequestStatus, null, body);
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
        private void GetChannelStatus(SynchronizationContext ctx)
        {
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(Constants.Exchange, "topic");
                    var queueName = channel.QueueDeclare();

                    Debug.WriteLine(" [Clt] Waiting for request.");

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (_, msg) => ctx.Post(foo_ => ReceiveStatus(msg), null);

                    channel.QueueBind(queueName, Constants.Exchange, string.Format(Constants.keyClientNotification + ".*.{0}", this.CurrentUser));
                    channel.BasicConsume(queueName, true, consumer);

                    FinishEvent.WaitOne();
                }
            }
        }

        // Zmienia status użytkownika na liście kontaktów
        private void ReceiveStatus(BasicDeliverEventArgs args)
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
                    if (message.PresenceStatus.Equals(PresenceStatus.Login))
                    {
                        contact.State = "Green";
                        this.SelectedStatus = "Dostępny";
                    }
                }
            }
        }

    }
}
