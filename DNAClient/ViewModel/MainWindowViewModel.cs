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
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Linq;
    using System.Windows.Documents;

    using DNAClient.RabbitFunctions;
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
            this.ContactRightClickCommand = new RelayCommand(this.ContactRightClick);
            this.OpenHistoryCommand = new RelayCommand(this.OpenHistory);
            this.addFriendCommand = new RelayCommand(this.addNewFriend);
            this.CloseWindowCommand = new RelayCommand(this.CloseWindow);
            this.DeleteContactCommand = new RelayCommand(this.DeleteContact);

            List<string> friends = new List<string>();
            friends = this.GetFriends();
            foreach (string friend in friends)
            {
                this.Contacts.Add(new Contact() { Name = friend });
            }

            var contact = Contacts.Where(X => X.Name == this.CurrentUser).FirstOrDefault();
            Contacts.Remove(contact);

            // Uruchomienie zadania, które w tle będzie nasłuchiwać wiadomości przychodzących z serwera
            var ctx = SynchronizationContext.Current;
            Task.Factory.StartNew(() => this.GetChannel(ctx));

            this.SelectedStatus = "Zalogowany";

            var rpcClient = new RpcWay();

            var request = new Request
            {
                Login = this.CurrentUser,
                RequestType = Request.Type.OldMessages
            };

            rpcClient.OldMessagesCall(request.Serialize());
            rpcClient.Close();

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

        public Contact SelectedContact
        {
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
        /// Komenda usunięcia kontaktu z listy
        /// </summary>
        public RelayCommand DeleteContactCommand { get; set; }

        /// <summary>
        /// Metoda usuwająca kontakt z listy
        /// </summary>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        private void DeleteContact(object parameter)
        {
            var tmp = parameter as Contact;
            var contact = Contacts.Where(X => X.Name == tmp.Name).FirstOrDefault();

            var rpcClient = new RpcWay();

            var friendRequest = new FriendRequest
            {
                Login = currentUser,
                FriendLogin = contact.Name,
                RequestType = Request.Type.RemoveFriend,
            };

            var friendResponse = rpcClient.FriendCall(friendRequest.Serialize());
            rpcClient.Close();

            if (friendResponse.Status == Status.OK)
            {
                
                this.Contacts.Remove(contact);
            }
            else
            {
                MessageBox.Show("Nie udało się usunąć kontaktu!", "Błąd usuwania kontaktu");
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
        public RelayCommand ContactRightClickCommand { get; set; }

        /// <summary>
        /// Metoda otwierająca nowe okno konwersacji
        /// </summary>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        private void ContactRightClick(object parameter)
        {
            if (!String.IsNullOrEmpty(this.SelectedContact.Name))
            {
                //ProductionWindowFactory.CreateConversationWindow(this.SelectedContact.Name);
                Console.WriteLine("dupa");
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
            foreach (ConversationViewModel cvModel in GlobalsParameters.openWindows)
            {
                if (this.SelectedContact.Name == cvModel.Recipient)
                {
                    return;
                }
            }
            if (!String.IsNullOrEmpty(this.SelectedContact.Name))
            {
                ConversationViewModel cvModel =
                    ProductionWindowFactory.CreateConversationWindow(this.SelectedContact.Name);

                GlobalsParameters.openWindows.Add(cvModel);
            }
        }


        /// <summary>
        /// Metoda otwierająca nowe okno powiadomień
        /// </summary>
        /// <param name="parameter">
        /// The parameter.
        /// </param>


        public RelayCommand addFriendCommand { get; set; }

        private string friendName;

        public ObservableCollection<Contact> contacts = new ObservableCollection<Contact>();

        public ObservableCollection<Contact> Contacts
        {
            get
            {
                return contacts;
            }
            set
            {
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

        private void addNewFriend(object parameter)
        {
            if (String.IsNullOrEmpty(this.Friend))
            {
                MessageBox.Show(
                    "Aby dodać kontakt, należy wpisać jego nazwę. Spróbuj ponownie.",
                    "Błąd dodawania użytkowika");
            }
            else
            {
                bool isContactAlreadyOnList = false;
                foreach (Contact element in this.Contacts)
                {
                    if (element.Name.ToLower() == this.Friend.ToLower())
                    {
                        isContactAlreadyOnList = true;
                    }
                }

                if (this.Friend.Equals(currentUser))
                {
                    MessageBox.Show("Nie możesz dodać siebie do znajomych!", "Znajdź sobie znajomych");
                }
                else if (isContactAlreadyOnList)
                {
                    MessageBox.Show("Taki kontakt już istnieje!", "Błąd dodawania użytkowika");
                }
                else
                {
                    var rpcClient = new RpcWay();

                    var friendRequest = new FriendRequest
                    {
                        Login = currentUser,
                        FriendLogin = this.Friend,
                        RequestType = Request.Type.AddFriend,
                    };

                    var friendResponse = rpcClient.FriendCall(friendRequest.Serialize());
                    rpcClient.Close();

                    if (friendResponse.Status == Status.OK)
                    {
                        Contacts.Add(new Contact() { Name = this.Friend });
                    }
                    else
                    {
                        MessageBox.Show(
                    "Dodanie użytkownika nie powiodło się!",
                    "Błąd dodawania użytkowika");
                    }
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

            if (!String.IsNullOrEmpty(this.SelectedStatus)) state = this.SelectedStatus.Trim();
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

                    if (this.SelectedStatus == "Zajęty") status = PresenceStatus.Afk;
                    if (this.SelectedStatus == "Dostępny") status = PresenceStatus.Online;
                    if (this.SelectedStatus == "Niedostępny") status = PresenceStatus.Offline;

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
                    consumer.Received += (_, msg) => ctx.Post(foo_ => Receive(msg), null);

                    channel.QueueBind(
                        queueName,
                        Constants.Exchange,
                        string.Format(Constants.keyClientNotification + ".*.{0}.*", this.CurrentUser));
                    channel.BasicConsume(queueName, true, consumer);

                    FinishEvent.WaitOne();
                }
            }
        }
        private void NewNotificationWindow(string sender, BasicDeliverEventArgs mess, NotificationType notificationType)
        {
            ProductionWindowFactory.CreateNotificationWindow(sender, mess, notificationType);
        }

        bool check = true;
        // Zmienia status użytkownika na liście kontaktów
        private void Receive(BasicDeliverEventArgs args)
        {
            var body = args.Body;
            var routingKey = args.RoutingKey;


            if (routingKey.StartsWith(Constants.keyClientNotification + ".status"))
            {
                var message = body.DeserializePresenceStatusNotification();
                var contact = Contacts.Where(X => X.Name == message.Login).FirstOrDefault();
                if (contact != null)
                {
                    if (message.PresenceStatus.Equals(PresenceStatus.Offline)) contact.State = "#FFD1D1D1";
                    if (message.PresenceStatus.Equals(PresenceStatus.Online)) contact.State = "Green";
                    if (message.PresenceStatus.Equals(PresenceStatus.Afk)) contact.State = "Red";
                    if (message.PresenceStatus.Equals(PresenceStatus.Login))
                    {
                        contact.State = "Green";
                        this.SelectedStatus = "Dostępny";
                    }
                }
            }
            if (routingKey.StartsWith(Constants.keyClientNotification + ".message"))
            {
                bool ConversationWindowExist = false;
                var message = body.DeserializeMessageNotification();
                DateTimeOffset date = message.SendTime;
                var msg = (date == new DateTime(2000, 1, 1)) ? String.Empty : date.ToString("dd.MM.yyyy (HH:mm:ss)") + " przez " + message.Sender + ":\n";
                msg += message.Message;

                Paragraph para = new Paragraph();

                foreach (ConversationViewModel cvModel in GlobalsParameters.openWindows)
                {
                    if (cvModel.Recipient == message.Sender)
                    {
                        ConversationWindowExist = true;
                        cvModel.Receive(args);
                    }
                }
                if (!ConversationWindowExist)
                {
                    if (!GlobalsParameters.cache.ContainsKey(message.Sender))
                    {
                        FlowDocument flowD = new FlowDocument();
                        GlobalsParameters.cache.Add(message.Sender, flowD);
                    }
                    if (!string.IsNullOrEmpty(message.Message))
                    {
                        para.Inlines.Add(msg);
                        GlobalsParameters.cache[message.Sender].Blocks.Add(para);
                    }
                    if (!GlobalsParameters.openNotifications.Contains(message.Sender) && !string.IsNullOrEmpty(message.Message))
                    {
                        this.NewNotificationWindow(message.Sender, args, NotificationType.message);   
                        GlobalsParameters.notificationCache.Add(message.Sender, msg);
                    }
                    else if (!string.IsNullOrEmpty(message.Message))
                    {
                        GlobalsParameters.notificationCache[message.Sender] += msg;
                    }
                }

                if (message.Attachment != null)
                {
                    this.NewNotificationWindow(message.Sender, args, NotificationType.file);
                }
            }
        }

        // pobieranie listy znajomych
        private List<string> GetFriends()
        {
            List<string> friends = new List<string>();
            var rpcClient = new RpcWay();

            var friendRequest = new FriendRequest
            {
                Login = currentUser,
                RequestType = Request.Type.GetFriends,
            };

            var friendResponse = rpcClient.FriendCall(friendRequest.Serialize());
            rpcClient.Close();

            if (friendResponse.friendsList != null && friendResponse.Status == Status.OK)
            {
                friends = friendResponse.friendsList;
            }

            return friends;
        }

    }
}
