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
    using System.ComponentModel;
    using System.IO;

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
        /// <summary>
        /// The finish event.
        /// </summary>
        private static readonly ManualResetEvent FinishEvent = new ManualResetEvent(false);

        /// <summary>
        /// Fabryka połączeń rabbitMQ
        /// </summary>
        private static readonly ConnectionFactory ConnectionFactory = Constants.ConnectionFactory;

        /// <summary>
        /// Aktualnie zalogowany użytkownik
        /// </summary>
        private string currentUser;

        /// <summary>
        /// Wybrany status
        /// </summary>
        private string selectedStatus;

        private RabbitRpcConnection rpcClient;

        /// <summary>
        /// Nazwa nowego kontaktu
        /// </summary>
        private string newFriendName;

        /// <summary>
        /// Wybrany kontakt z listy
        /// </summary>
        private Contact selectedContact;// = new Contact() { Name = null };

        /// <summary>
        /// Aktualna ścieżka użytkownika
        /// </summary>
        private string userPath;

        /// <summary>
        /// Słownik mapowania znaków specjalnych na emotikony
        /// </summary>
        private Dictionary<string, string> emoticonsMappings = new Dictionary<string, string>();

        /// <summary>
        /// Konstruktor view modelu głównego okna
        /// </summary>
        public MainWindowViewModel()
        {
            this.userPath = Constants.userPath;
            this.CurrentUser = GlobalsParameters.Instance.CurrentUser;
            this.NewConversationWindowCommand = new RelayCommand(this.NewConversationWindow);
            this.OpenHistoryCommand = new RelayCommand(this.OpenHistory);
            this.AddFriendCommand = new RelayCommand(this.AddNewFriend);
            this.CloseWindowCommand = new RelayCommand(this.CloseWindow);
            this.DeleteContactCommand = new RelayCommand(this.DeleteContact);

            // Uruchomienie zadania, które w tle będzie nasłuchiwać wiadomości przychodzących z serwera
            var ctx = SynchronizationContext.Current;
            Task.Factory.StartNew(() => this.GetChannel(ctx));
            this.rpcClient = new RabbitRpcConnection();
            this.LoadEmoticons();
            this.LoadFriendsList();
            this.LoadOldMessages();

            // Ustawienie statusu jako zalogowany. Informacja ta zostanie wysłana do wszystkich kontaktów na liście,
            // dzięki temu użytkownik otrzyma wiadomość o aktualnych statusach swoich znajomych
            this.SelectedStatus = "Zalogowany";
        }

        /// <summary>
        /// Property odbiorcy do zbindowania w xamlu
        /// </summary>
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

        /// <summary>
        /// Zaznaczony kontakt na liście
        /// </summary>
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

        /// <summary>
        /// Wybrany status użytkownika
        /// </summary>
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
        /// Nazwa nowego kontaktu
        /// </summary>
        public string NewFriendName
        {
            get
            {
                return this.newFriendName;
            }

            set
            {
                this.newFriendName = value;
                this.RaisePropertyChanged("NewFriendName");
            }
        }

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

        /// <summary>
        /// Komenda otwarcia nowego okna konweracji
        /// </summary>
        public RelayCommand NewConversationWindowCommand { get; set; }

        /// <summary>
        /// Komenda otwarcia historii rozmowy
        /// </summary>
        public RelayCommand OpenHistoryCommand { get; set; }

        /// <summary>
        /// Komenda usunięcia kontaktu z listy
        /// </summary>
        public RelayCommand DeleteContactCommand { get; set; }

        /// <summary>
        /// Komenda dodania nowego kontaktu do listy
        /// </summary>
        public RelayCommand AddFriendCommand { get; set; }

        /// <summary>
        /// Komenda zamknięcia głównego okna
        /// </summary>
        public RelayCommand CloseWindowCommand { get; set; }

        /// <summary>
        /// Metoda usuwająca kontakt z listy
        /// </summary>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        private void DeleteContact(object parameter)
        {
            if (this.SelectedContact != null)
            {
                var friendRequest = new FriendRequest
                                        {
                                            Login = this.currentUser,
                                            FriendLogin = this.SelectedContact.Name,
                                            RequestType = Request.Type.RemoveFriend,
                                        };

                var friendResponse = this.rpcClient.FriendCall(friendRequest.Serialize());

                if (friendResponse.Status == Status.OK)
                {
                    this.SendStatusToQueue(this.SelectedContact.Name.ToLower(), "Niedostępny");
                    this.Contacts.Remove(this.SelectedContact);
                }
                else
                {
                    MessageBox.Show(friendResponse.Message, "Błąd usuwania kontaktu");
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
            if (this.selectedContact != null)
            {
                var historyFile = this.userPath + "//" + this.SelectedContact.Name;
                if (File.Exists(historyFile))
                {
                    Process.Start("notepad.exe", @historyFile);
                }
                else
                {
                    MessageBox.Show(
                    "Historia rozmów z tym użytkownikiem nie istnieje.",
                    "Błąd");
                }
            }
        }

        /// <summary>
        /// Metoda sprawdza czy dla danego kontaktu jest już otwarte okno konwersacji,
        /// jeżeli nie to otwiera takowe
        /// </summary>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        private void NewConversationWindow(object parameter)
        {
            foreach (var conversationViewModel in GlobalsParameters.OpenWindows)
            {
                if (this.SelectedContact.Name.ToLower() == conversationViewModel.Recipient)
                {
                    return;
                }
            }

            if (!string.IsNullOrEmpty(this.SelectedContact.Name))
            {
                var conversationViewModel =
                    ProductionWindowFactory.CreateConversationWindow(this.SelectedContact.Name);

                GlobalsParameters.OpenWindows.Add(conversationViewModel);
            }
        }

        /// <summary>
        /// Metoda dodawania nowego kontaktu do listy
        /// </summary>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        private void AddNewFriend(object parameter)
        {
            var MainWindow = parameter as MainWindow;

            if (string.IsNullOrEmpty(this.NewFriendName))
            {
                MessageBox.Show(
                    "Aby dodać kontakt, należy wpisać jego nazwę. Spróbuj ponownie.",
                    "Błąd dodawania nowego kontaktu");
            }
            else
            {
                bool isContactAlreadyOnList = false;
                foreach (Contact element in this.Contacts)
                {
                    if (element.Name.ToLower().Equals(this.NewFriendName.ToLower()))
                    {
                        isContactAlreadyOnList = true;
                        break;
                    }
                }

                if (this.NewFriendName.ToLower().Equals(this.CurrentUser.ToLower()))
                {
                    MessageBox.Show("Nie możesz dodać siebie do znajomych!", "Znajdź sobie znajomych");
                }
                else if (isContactAlreadyOnList)
                {
                    MessageBox.Show("Taki kontakt już istnieje!", "Błąd dodawania użytkowika");
                }
                else
                {
                    DTO.FriendResponse friendResponse = new  FriendResponse();
                    BackgroundWorker worker = new BackgroundWorker();
                    worker.DoWork += (o, ea) =>
                    {

                        var friendRequest = new FriendRequest
                        {
                            Login = this.CurrentUser.ToLower(),
                            FriendLogin = this.NewFriendName,
                            RequestType = Request.Type.AddFriend,
                        };
                    

                        friendResponse = rpcClient.FriendCall(friendRequest.Serialize());
                    };

                    worker.RunWorkerCompleted += (o, ea) =>
                    {
                        MainWindow.Stop_Loading();
                    };

                    MainWindow.Start_Loading();
                    worker.RunWorkerAsync();

                    if (friendResponse.Status == Status.OK)
                    {
                        this.Contacts.Add(new Contact() { Name = this.NewFriendName });
                        this.SendStatusToQueue(this.NewFriendName.ToLower(), "Zwroc");
                        this.SendStatus();
                        this.NewFriendName = string.Empty;
                    }
                    else
                    {
                        MessageBox.Show(friendResponse.Message, "Błąd dodawania użytkowika");
                    }
                }
            }


        }

        /// <summary>
        /// Metoda zamyka główne okno programu oraz wszystkie otwarte okna rozmowy
        /// </summary>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        private void CloseWindow(object parameter)
        {
            var window = parameter as Window;

            if (window != null)
            {
                FinishEvent.Set();
                this.SelectedStatus = "Niedostępny";

                var openWindows = GlobalsParameters.OpenWindows;
                foreach (var openWindow in openWindows.ToList())
                {
                    openWindow.CloseConversationWindow();    
                }
                this.LogOff();
                this.rpcClient.Close();
                window.Close();
            }
        }

        /// <summary>
        /// Wysyłanie aktualnego statusu użytkownika
        /// </summary>
        private void SendStatus()
        {
            string state = string.Empty;

            if (!string.IsNullOrEmpty(this.SelectedStatus))
            {
                state = this.SelectedStatus.Trim();
            }

            if (!string.IsNullOrEmpty(state))
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
        /// <param name="recipient">
        /// Odbiorca komunikatu o zmianie statusu
        /// </param>
        /// <param name="statusToSend">
        /// Dodatkowy status 
        /// </param>
        private void SendStatusToQueue(string recipient, string statusToSend)
        {
            using (var connection = ConnectionFactory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(Constants.Exchange, "topic");
                    var status = PresenceStatus.Login;

                    if (!string.IsNullOrEmpty(statusToSend))
                    {
                        if (statusToSend == "Zwroc")
                        {
                            status = PresenceStatus.Demand;
                        }

                        if (statusToSend == "Niedostępny")
                        {
                            status = PresenceStatus.Offline;
                        }
                    }
                    else
                    {
                        switch (this.SelectedStatus)
                        {
                            case "Zajęty":
                                status = PresenceStatus.Afk;
                                break;
                            case "Dostępny":
                                status = PresenceStatus.Online;
                                break;
                            case "Niedostępny":
                                status = PresenceStatus.Offline;
                                break;
                        }

                        if (this.SelectedStatus == "Zalogowany")
                        {
                            status = PresenceStatus.Login;
                        }
                    }

                    var message = new PresenceStatusNotification
                                      {
                                          Login = this.CurrentUser.ToLower(),
                                          PresenceStatus = status,
                                          Recipient = recipient
                                      };

                    var body = message.Serialize();
                    channel.BasicPublish(Constants.Exchange, Constants.keyServerRequestStatus, null, body);
                }
            }
        }

        /// <summary>
        /// Metoda do obierania wiadomości z serwera
        /// </summary>
        /// <param name="ctx">
        /// Synchronizacja wątków
        /// </param>
        private void GetChannel(SynchronizationContext ctx)
        {
            using (var connection = ConnectionFactory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(Constants.Exchange, "topic");
                    var queueName = channel.QueueDeclare();

                    Debug.WriteLine(" [Clt] Waiting for request.");

                    var consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (_, msg) => ctx.Post(foo => this.Receive(msg), null);

                    channel.QueueBind(
                        queueName,
                        Constants.Exchange,
                        string.Format(Constants.keyClientNotification + ".*.{0}.*", this.CurrentUser.ToLower()));
                    channel.BasicConsume(queueName, true, consumer);

                    FinishEvent.WaitOne();
                }
            }
        }

        /// <summary>
        /// Metoda otwierania nowego okna notyfikacji
        /// </summary>
        /// <param name="sender">
        /// Wysyłający notyfikację
        /// </param>
        /// <param name="mess">
        /// Dane o wiadomości z rabbita
        /// </param>
        /// <param name="notificationType">
        /// Typ notyfikacji
        /// </param>
        private void NewNotificationWindow(string sender, BasicDeliverEventArgs mess, NotificationType notificationType)
        {
            ProductionWindowFactory.CreateNotificationWindow(sender, mess, notificationType);
        }

        /// <summary>
        /// Metoda obsługująca przychodzące wiadomości z serwera
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private void Receive(BasicDeliverEventArgs args)
        {
            var body = args.Body;
            var routingKey = args.RoutingKey;

            if (routingKey.StartsWith(Constants.keyClientNotification + ".status"))
            {
                var message = body.DeserializePresenceStatusNotification();
                var contact = this.Contacts.FirstOrDefault(x => x.Name.ToLower() == message.Login);
                if (contact != null)
                {
                    if (message.PresenceStatus.Equals(PresenceStatus.Demand))
                    {
                        this.SendStatusToQueue(contact.Name.ToLower(), null);
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
                bool conversationWindowExist = false;
                var message = body.DeserializeMessageNotification();
                DateTimeOffset date = message.SendTime;
                var msg = (date == new DateTime(2000, 1, 1)) ? string.Empty : date.ToString("dd.MM.yyyy (HH:mm:ss)") + " przez " + message.Sender + ":\n";
                msg += message.Message;

                Paragraph para = new Paragraph();

                foreach (ConversationViewModel conversationViewModel in GlobalsParameters.OpenWindows)
                {
                    if (conversationViewModel.Recipient == message.Sender)
                    {
                        conversationWindowExist = true;
                        conversationViewModel.Receive(args);
                    }
                }

                if (!conversationWindowExist)
                {
                    if (!GlobalsParameters.TextCache.ContainsKey(message.Sender))
                    {
                        FlowDocument flowD = new FlowDocument();
                        GlobalsParameters.TextCache.Add(message.Sender, flowD);
                    }

                    if (!string.IsNullOrEmpty(message.Message))
                    {
                        para = this.Emoticons(msg);
                        GlobalsParameters.TextCache[message.Sender].Blocks.Add(para);
                    }

                    if (!GlobalsParameters.OpenNotifications.Contains(message.Sender) && !string.IsNullOrEmpty(message.Message))
                    {
                        this.NewNotificationWindow(message.Sender, args, NotificationType.Message);   
                        GlobalsParameters.NotificationCache.Add(message.Sender, msg);
                    }
                    else if (!string.IsNullOrEmpty(message.Message))
                    {
                        GlobalsParameters.NotificationCache[message.Sender] += msg;
                    }
                }

                if (message.Attachment != null)
                {
                    this.NewNotificationWindow(message.Sender, args, NotificationType.File);
                }
            }
        }

        /// <summary>
        /// Metoda sprawdzająca czy dany ciąg znaków znajduje się w słowniku emotikon 
        /// </summary>
        /// <param name="text">
        /// Sprawdzany ciąg znaków
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private string GetEmoticonText(string text)
        {
            string match = string.Empty;
            int lowestPosition = text.Length;

            foreach (KeyValuePair<string, string> pair in this.emoticonsMappings)
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

        /// <summary>
        /// Metoda konwertująca ciąg znaków na emotikonę 
        /// </summary>
        /// <param name="msg">
        /// Ciąg znaków do przekonwertowania
        /// </param>
        /// <returns>
        /// The <see cref="Paragraph"/>.
        /// </returns>
        private Paragraph Emoticons(string msg)
        {
            Paragraph paragraph = new Paragraph();

            Run r = new Run(msg);

            paragraph.Inlines.Add(r);

            string emoticonText = this.GetEmoticonText(r.Text);

            if (string.IsNullOrEmpty(emoticonText))
            {
                return paragraph;
            }
            while (!string.IsNullOrEmpty(emoticonText))
            {
                TextPointer tp = r.ContentStart;
                if (emoticonText != null)
                    while (!tp.GetTextInRun(LogicalDirection.Forward).StartsWith(emoticonText))

                        tp = tp.GetNextInsertionPosition(LogicalDirection.Forward);
                var tr = new TextRange(tp, tp.GetPositionAtOffset(emoticonText.Length)) { Text = " " };

                //relative path to image smile file
                Console.WriteLine(emoticonText);
                string path = this.emoticonsMappings[emoticonText];

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

                    emoticonText = this.GetEmoticonText(endRun.Text);
                }
            }
            return paragraph;
        }

        /// <summary>
        /// Metoda wczytująca emotikony
        /// </summary>
        private void LoadEmoticons()
        {
            this.emoticonsMappings.Add(@"-.-", @"../../emots/e1_25.gif");
            this.emoticonsMappings.Add(@"xD", @"../../emots/e2_25.gif");
            this.emoticonsMappings.Add(@"o.O", @"../../emots/e6_25.gif");
            this.emoticonsMappings.Add(@"oO", @"../../emots/e6_25.gif");
            this.emoticonsMappings.Add(@":(", @"../../emots/e7_25.gif");
            this.emoticonsMappings.Add(@":<", @"../../emots/e8_25.gif");
            this.emoticonsMappings.Add(@":O", @"../../emots/e5_25.gif");
        }

        /// <summary>
        /// Pobranie z serwera wiadomości, które zostały wysłane, gdy użytkownik był offline
        /// </summary>
        private void LoadOldMessages()
        {
            using (var connection = ConnectionFactory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(Constants.Exchange, "topic");
                    
                    var message = new Request()
                    {
                        Login = this.CurrentUser.ToLower(),
                    };

                    var body = message.Serialize();
                    channel.BasicPublish(Constants.Exchange, Constants.keyServerRequestGetOld, null, body);
                }
            }
        }

        /// <summary>
        /// Poinformowanie serwera o zamknięciu komunikatora
        /// </summary>
        private void LogOff()
        {
            using (var connection = ConnectionFactory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.ExchangeDeclare(Constants.Exchange, "topic");

                    var message = new Request()
                    {
                        Login = this.CurrentUser.ToLower(),
                    };

                    var body = message.Serialize();
                    channel.BasicPublish(Constants.Exchange, Constants.keyServerRequestLogOff, null, body);
                }
            }
        }

        /// <summary>
        /// Załadowanie znajomych użytkownika do głównej listy w programie
        /// </summary>
        private void LoadFriendsList()
        {
            IEnumerable<string> friends = this.GetFriends();
            foreach (string friend in friends)
            {
                this.Contacts.Add(new Contact() { Name = friend });
            }
        }

        /// <summary>
        /// Pobranie z serwera listy przyjaciół zalogowanego użytkownika
        /// </summary>
        /// <returns>
        /// Lista przyjaciół zalogowanego użytkownika
        /// </returns>
        private IEnumerable<string> GetFriends()
        {
            GlobalsParameters.Instance.Contacts = new ObservableCollection<Contact>();
            var friends = new List<string>();

            var friendRequest = new FriendRequest
            {
                Login = this.currentUser,
                RequestType = Request.Type.GetFriends,
            };

            var friendResponse = rpcClient.FriendCall(friendRequest.Serialize());

            if (friendResponse.Status == Status.OK)
            {
                if (friendResponse.friendsList != null)
                {
                    friends = friendResponse.friendsList;
                }
            }
            else
            {
                MessageBox.Show(friendResponse.Message, "Błąd ładowania listy znajomych");
            }

            return friends;
        }
    }
}
