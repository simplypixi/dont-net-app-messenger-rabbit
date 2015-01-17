// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindowViewModel.cs" company="">
//   
// </copyright>
// <summary>
//   View model głównego okna
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DNAClient.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Media.Imaging;

    using DNAClient.RabbitFunctions;
    using DNAClient.ViewModel.Base;

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

        private Dictionary<string, string> _mappings = new Dictionary<string, string>();

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

            this.LoadEmoticons();

            List<string> friends = this.GetFriends();
            foreach (string friend in friends)
            {
                this.Contacts.Add(new Contact() { Name = friend });
            }

            // Uruchomienie zadania, które w tle będzie nasłuchiwać wiadomości przychodzących z serwera
            var ctx = SynchronizationContext.Current;
            Task.Factory.StartNew(() => this.GetChannel(ctx));

            this.SelectedStatus = "Zalogowany";

            var rpcClient = new RpcWay();

            // Podbranie z serwera wiadomości, które zostały wysłane, gdy użytkownik był offline
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
        public string CurrentUser
        {
            get
            {
                return this.currentUser.ToLower();
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
                return this.recipient.ToLower();
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
        /// Komenda otwarcia nowego okna konweracji
        /// </summary>
        public RelayCommand NewConversationWindowCommand { get; set; }

        /// <summary>
        /// Komenda otwarcia nowego okna konweracji
        /// </summary>
        public RelayCommand ContactRightClickCommand { get; set; }

        /// <summary>
        /// Komenda otwarcia historii rozmowy
        /// </summary>
        public RelayCommand OpenHistoryCommand { get; set; }

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
            var contact = this.Contacts.FirstOrDefault(c => c.Name == tmp.Name);

            if (contact != null)
            {
                var rpcClient = new RpcWay();
                var friendRequest = new FriendRequest
                                        {
                                            Login = this.currentUser,
                                            FriendLogin = contact.Name,
                                            RequestType = Request.Type.RemoveFriend,
                                        };

                var friendResponse = rpcClient.FriendCall(friendRequest.Serialize());
                rpcClient.Close();

                if (friendResponse.Status == Status.OK)
                {
                    this.SendStatusToQueue(contact.Name.ToLower(), "Niedostępny");
                    this.Contacts.Remove(contact);
                }
                else
                {
                    MessageBox.Show("Nie udało się usunąć kontaktu!", "Błąd usuwania kontaktu");
                }
            }
        }

        /// <summary>
        /// Metoda otwierająca historię rozmowy
        /// </summary>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        private void OpenHistory(object parameter)
        {
            var historyFile = this.userPath + "//" + this.SelectedContact.Name;
            Process.Start(@historyFile);
        }

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
        /// Metoda otwierająca nowe okno konwersacji
        /// </summary>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        private void NewConversationWindow(object parameter)
        {
            foreach (ConversationViewModel cvModel in GlobalsParameters.openWindows)
            {
                if (this.SelectedContact.Name.ToLower() == cvModel.Recipient)
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

        public RelayCommand addFriendCommand { get; set; }

        private string friendName;

        public ObservableCollection<Contact> contacts = new ObservableCollection<Contact>();

        /// <summary>
        /// Kontakty zalogowanego użytkownika - property do bindowania w xaml
        /// </summary>
        public ObservableCollection<Contact> Contacts
        {
            get
            {
                if (GlobalsParameters.Instance.Contacts != null)
                {
                    return GlobalsParameters.Instance.Contacts;
                }
                
                return new ObservableCollection<Contact>();
            }

            set
            {
                GlobalsParameters.Instance.Contacts = value;
                this.RaisePropertyChanged("Contacts");
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
                        break;
                    }
                }

                if (this.Friend.ToLower().Equals(currentUser))
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
                        this.SendStatusToQueue(this.Friend.ToLower(), "Zwroc");
                        this.SendStatus();
                        this.Friend = string.Empty;
                    }
                    else
                    {
                        MessageBox.Show("Dodanie użytkownika nie powiodło się!", "Błąd dodawania użytkowika");
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

                var openWindows = GlobalsParameters.openWindows;
                foreach (var openWindow in openWindows.ToList())
                {
                    openWindow.CloseConversationWindow();    
                }

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

                    this.SendStatusToQueue(element.Name.ToLower(), null);
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
        private void SendStatusToQueue(string recip, string status_to_send)
        {
            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(Constants.Exchange, "topic");
                    PresenceStatus status = PresenceStatus.Login;

                    if (!string.IsNullOrEmpty(status_to_send))
                    {
                        if (status_to_send == "Zwroc")  status = PresenceStatus.Demand;
                        if (status_to_send == "Niedostępny") status = PresenceStatus.Offline;
                    }
                    else
                    {
                        if (this.SelectedStatus == "Zajęty") status = PresenceStatus.Afk;
                        if (this.SelectedStatus == "Dostępny") status = PresenceStatus.Online;
                        if (this.SelectedStatus == "Niedostępny") status = PresenceStatus.Offline;

                        if (this.SelectedStatus == "Zalogowany")
                        {
                            status = PresenceStatus.Login;
                        }
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

        // Zmienia status użytkownika na liście kontaktów
        private void Receive(BasicDeliverEventArgs args)
        {
            var body = args.Body;
            var routingKey = args.RoutingKey;


            if (routingKey.StartsWith(Constants.keyClientNotification + ".status"))
            {
                var message = body.DeserializePresenceStatusNotification();
                var contact = Contacts.Where(X => X.Name.ToLower() == message.Login).FirstOrDefault();
                if (contact != null)
                {
                    if (message.PresenceStatus.Equals((PresenceStatus.Demand)))
                    {
                        this.SendStatusToQueue(contact.Name, null);
                    }
                    contact.PresenceStatus = message.PresenceStatus;
                    if (message.PresenceStatus.Equals(PresenceStatus.Login))
                    {
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
                        para = Emoticons(msg);
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
            GlobalsParameters.Instance.Contacts = new ObservableCollection<Contact>();
            List<string> friends = new List<string>();
            var rpcClient = new RpcWay();

            var friendRequest = new FriendRequest
            {
                Login = this.currentUser,
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

        /* 
        * Metoda sprawdzająca czy dany ciąg znaków znajduje się w słowniku emotikon 
        */
        private string GetEmoticonText(string text)
        {
            string match = string.Empty;
            int lowestPosition = text.Length;

            foreach (KeyValuePair<string, string> pair in _mappings)
            {
                if (text.Contains(pair.Key))
                {
                    int newPosition = text.IndexOf(pair.Key);
                    if (newPosition < lowestPosition)
                    {
                        match = pair.Key;
                        lowestPosition = newPosition;
                    }
                }
            }

            return match;

        }

        /* 
         * Metoda konwertująca ciągn znaków na emotikonę 
        */
        private Paragraph Emoticons(string msg)
        {
            Paragraph paragraph = new Paragraph();

            Run r = new Run(msg);

            paragraph.Inlines.Add(r);

            string emoticonText = GetEmoticonText(r.Text);

            if (string.IsNullOrEmpty(emoticonText))
            {
                return paragraph;
            }
            else
            {
                while (!string.IsNullOrEmpty(emoticonText))
                {

                    TextPointer tp = r.ContentStart;
                    if (emoticonText != null)
                        while (!tp.GetTextInRun(LogicalDirection.Forward).StartsWith(emoticonText))

                            tp = tp.GetNextInsertionPosition(LogicalDirection.Forward);
                    var tr = new TextRange(tp, tp.GetPositionAtOffset(emoticonText.Length)) { Text = " " };

                    //relative path to image smile file
                    Console.WriteLine(emoticonText);
                    string path = _mappings[emoticonText];

                    Image image = new Image
                    {
                        Source =
                            new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute)),
                        Width = 25,
                        Height = 25,
                    };

                    new InlineUIContainer(image, tp);

                    if (paragraph != null)
                    {
                        var endRun = paragraph.Inlines.LastInline as Run;

                        if (endRun == null)
                        {
                            break;
                        }
                        else
                        {
                            emoticonText = GetEmoticonText(endRun.Text);
                        }

                    }

                }

            }
            return paragraph;
        }

        /* 
         * Metoda wczytująca bazę emotikon 
        */
        public void LoadEmoticons()
        {
            _mappings.Add(@"-.-", @"../../emots/e1_25.gif");
            _mappings.Add(@"xD", @"../../emots/e2_25.gif");
            _mappings.Add(@"o.O", @"../../emots/e6_25.gif");
            _mappings.Add(@"oO", @"../../emots/e6_25.gif");
            _mappings.Add(@":(", @"../../emots/e7_25.gif");
            _mappings.Add(@":<", @"../../emots/e8_25.gif");
            _mappings.Add(@":O", @"../../emots/e5_25.gif");
        }

    }
}
