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
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Documents;
    using System.Windows.Markup;
    using System.Windows.Media.Imaging;

    using DNAClient.View;
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
        /// Manual reset event
        /// </summary>
        private static readonly ManualResetEvent FinishEvent = new ManualResetEvent(false);
        
        /// <summary>
        /// Fabryka połączeń rabbitMQ
        /// </summary>
        private static ConnectionFactory factory = Constants.ConnectionFactory;

        /// <summary>
        /// Treść wiadomości do wysłania
        /// </summary>
        private string message;

        /// <summary>
        /// Aktualnie zalogowany użytkownik
        /// </summary>
        private string currentUser;

        /// <summary>
        /// Odbiorca wiadomości
        /// </summary>
        private string recipient;

        /// <summary>
        /// Załącznik do wiadomości
        /// </summary>
        private Attachment attachment;

        /// <summary>
        /// Okno rozmowy (obejście dla dostępu do niektórych kontrolek)
        /// </summary>
        private ConversationWindow conversationWindow;

        /// <summary>
        /// Kontrolka odebranych wiadomości
        /// </summary>
        private RichTextBox talkWindow;

        /// <summary>
        /// Dokument przechowujący odebrane wiadomości wraz z emotikonami
        /// </summary>
        private FlowDocument received;

        /// <summary>
        /// Aktualna ścieżka użytkownika
        /// </summary>
        private string userPath;

        /// <summary>
        /// Słownik mapowania znaków specjalnych na emotikony
        /// </summary>
        private Dictionary<string, string> emoticonsMappings = new Dictionary<string, string>();

        /// <summary>
        /// Kontruktor klasy <see cref="ConversationViewModel"/>
        /// </summary>
        public ConversationViewModel()
        {
            this.userPath = Constants.userPath;
            this.CurrentUser = GlobalsParameters.Instance.CurrentUser;
            this.SendMessageCommand = new RelayCommand(this.SendMessage);
            this.CloseWindowCommand = new RelayCommand(this.CloseWindow);
            this.AttachFileCommand = new RelayCommand(this.AttachFile);
        }

        /// <summary>
        /// Konstruktor ustawiający konkretnego odbiorcę
        /// </summary>
        /// <param name="recipient">
        /// Odbiorca wiadomości
        /// </param>
        /// <param name="conversationWindow">
        /// Okno rozmowy
        /// </param>
        public ConversationViewModel(string recipient, ConversationWindow conversationWindow)
            : this()
        {
            this.conversationWindow = conversationWindow;
            this.talkWindow = this.conversationWindow.Talk;
            this.talkWindow.Document = new FlowDocument();
            this.LoadEmoticons();

            this.Recipient = recipient;
            if (GlobalsParameters.TextCache.ContainsKey(this.Recipient))
            {
                MemoryStream ms = new MemoryStream();

                XamlWriter.Save(GlobalsParameters.TextCache[this.Recipient], ms);

                ms.Seek(0, SeekOrigin.Begin);

                this.talkWindow.Document = XamlReader.Load(ms) as FlowDocument;
            }
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
                if (this.recipient != null)
                {
                    return this.recipient.ToLower();
                }
                return this.recipient;
            }

            set
            {
                this.recipient = value;
                this.RaisePropertyChanged("Recipient");
            }
        }

        public FlowDocument Received
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

        public RelayCommand AttachFileCommand { get; set; }

        /// <summary>
        /// Metoda wywoływana za każdym razem gdy serwer coś doda do kolejki i klient to przeczyta
        /// </summary>
        /// <param name="args">
        /// Argumenty rabbitowe
        /// </param>
        public void Receive(BasicDeliverEventArgs args)
        {
            var body = args.Body;
            var routingKey = args.RoutingKey;

            if (routingKey.StartsWith(Constants.keyClientNotification + ".message"))
            {
                var message = body.DeserializeMessageNotification();
                if (!string.IsNullOrEmpty(message.Message))
                {
                    DateTimeOffset date = message.SendTime;
                    var msg = date.ToString("dd.MM.yyyy (HH:mm:ss)") + " przez " + message.Sender + ":\n" + message.Message;
                    this.AddToHistory(msg);

                    Paragraph para = new Paragraph();

                    /* Konwertowanie tekstu na emotki */
                    para = this.Emoticons(msg);

                    if (!GlobalsParameters.TextCache.ContainsKey(this.Recipient))
                    {
                        GlobalsParameters.TextCache.Add(this.Recipient, new FlowDocument());
                    }

                    GlobalsParameters.TextCache[message.Sender].Blocks.Add(para);
                    try
                    {
                        this.talkWindow.Document = GlobalsParameters.TextCache[this.Recipient];
                    }
                    catch
                    {
                        MemoryStream ms = new MemoryStream();

                        XamlWriter.Save(GlobalsParameters.TextCache[this.Recipient], ms);

                        ms.Seek(0, SeekOrigin.Begin);

                        this.talkWindow.Document = XamlReader.Load(ms) as FlowDocument;
                    }
                }
            }
        }

        /// <summary>
        /// Metoda dodająca wpis do historii 
        /// </summary>
        /// <param name="historyMessage">
        /// Wiadomość dorzucana do historii
        /// </param>
        public void AddToHistory(string historyMessage)
        {
            if (this.Recipient == null)
            {
                return;
            }

            var historyFile = this.userPath + "//" + this.Recipient;
            Functions.saveFile(historyFile, historyMessage + "\n");
        }

        /// <summary>
        /// Metoda zamykająca okno rozmowy, wywoływana z główego okna programu
        /// </summary>
        public void CloseConversationWindow()
        {
            this.CloseWindow(this.conversationWindow);
        }

        /// <summary>
        /// Główna metoda wysyłania wiadomości
        /// </summary>
        /// <param name="parameter">
        /// Parametr z xaml'a
        /// </param>
        private void SendMessage(object parameter)
        {
            if (string.IsNullOrEmpty(this.Message) && parameter.GetType().Name != "ConversationWindow")
            {
                this.Message = parameter.ToString();   
            }

            if (!string.IsNullOrEmpty(this.Message) || this.attachment != null)
            {
                var attachmentInfo = string.Empty;
                var messageInfo = string.Empty;
                this.Message = this.Message.Trim();
                if (this.attachment != null)
                {
                    var contactPresenceStatus =
                        GlobalsParameters.Instance.Contacts
                            .Where(c => c.Name.ToLower().Equals(this.Recipient))
                            .Select(c => c.PresenceStatus)
                            .FirstOrDefault();

                    if (contactPresenceStatus != PresenceStatus.Online)
                    {
                        MessageBox.Show(
                            "Pliki można wysyłać tylko do dostępnych użytkowników!",
                            "Błąd wysyłania wiadomości");
                        return;
                    }
                    
                    attachmentInfo = string.Format(
                        "{0}: WYSŁANO ZAŁĄCZNIK",
                        DateTimeOffset.Now.ToString("dd.MM.yyyy (HH:mm:ss)"));
                    if (!string.IsNullOrEmpty(this.Message))
                    {
                        attachmentInfo = "\n\n" + attachmentInfo;
                    }

                    messageInfo = attachmentInfo;
                }

                if (!string.IsNullOrEmpty(this.Message))
                {
                    messageInfo = string.Format(
                        "{0} przez Ja:\n{1} {2}",
                        DateTimeOffset.Now.ToString("dd.MM.yyyy (HH:mm:ss)"),
                        this.Message,
                        attachmentInfo);
                }

                /* Konwertowanie tekstu na emotki */
                Paragraph paragraph = this.Emoticons(messageInfo);

                if (!GlobalsParameters.TextCache.ContainsKey(this.Recipient))
                {
                    GlobalsParameters.TextCache.Add(this.Recipient, new FlowDocument());
                }

                GlobalsParameters.TextCache[this.Recipient].Blocks.Add(paragraph);
                try
                {
                    this.talkWindow.Document = GlobalsParameters.TextCache[this.Recipient];
                }
                catch
                {
                    MemoryStream ms = new MemoryStream();

                    XamlWriter.Save(GlobalsParameters.TextCache[this.Recipient], ms);

                    ms.Seek(0, SeekOrigin.Begin);

                    this.talkWindow.Document = XamlReader.Load(ms) as FlowDocument;
                }

                this.SendMessageToQueue();
                this.AddToHistory(messageInfo);
                this.Message = string.Empty;
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

                    var messageToSend = new MessageReq
                                      {
                                          Login = this.CurrentUser,
                                          Message = !string.IsNullOrEmpty(this.Message) ? this.Message : string.Empty,
                                          Recipient = this.Recipient,
                                          SendTime = DateTimeOffset.Now,
                                          Attachment = this.attachment
                                      };

                    var body = messageToSend.Serialize();
                    channel.BasicPublish(Constants.Exchange, Constants.keyServerRequestMessage, null, body);
                    Debug.WriteLine("{0} wysłał \"{1}\" do: {2}", this.CurrentUser, this.Message, this.Recipient);
                }
            }

            this.attachment = null;
            this.conversationWindow.SendFile.Content = "Dodaj plik";
        }

        /// <summary>
        /// Metoda załączająca lub usuwająca plik z wiadomości
        /// </summary>
        /// <param name="param">
        /// Paramter przekazany z xaml'a
        /// </param>
        private void AttachFile(object param)
        {
            var conversationWindow = param as ConversationWindow;
            if (conversationWindow != null)
            {
                string sendFileText = conversationWindow.SendFile.Content.ToString();
                if (sendFileText.Equals("Dodaj plik"))
                {
                    var contactPresenceStatus =
                        GlobalsParameters.Instance.Contacts
                            .Where(c => c.Name.ToLower().Equals(this.Recipient))
                            .Select(c => c.PresenceStatus)
                            .FirstOrDefault();

                    if (contactPresenceStatus != PresenceStatus.Online)
                    {
                        MessageBox.Show("Pliki można wysyłać tylko do dostępnych użytkowników!", "Błąd dodawania pliku");
                        return;
                    }

                    var win = new Microsoft.Win32.OpenFileDialog { Multiselect = false };

                    var result = win.ShowDialog();

                    if (result.HasValue && result.Value)
                    {
                        string filePath = win.FileName;
                        Debug.Print(filePath);

                        this.attachment = new Attachment();

                        byte[] bytes = File.ReadAllBytes(filePath);

                        this.attachment.Data = bytes;
                        this.attachment.Name = win.SafeFileName;
                        this.attachment.MimeType = string.Empty;
                        conversationWindow.SendFile.Content = "Usuń";
                    }
                }
                else if (sendFileText.Equals("Usuń"))
                {
                    this.attachment = null;
                    conversationWindow.SendFile.Content = "Dodaj plik";
                }
            }
        }

        /// <summary>
        /// Zamknięcie okna rozmowy i zakończenie 
        /// </summary>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        private void CloseWindow(object parameter)
        {
            var window = parameter as Window;
            this.talkWindow = null;
            if (window != null)
            {
                foreach (ConversationViewModel conversationViewModel in GlobalsParameters.OpenWindows)
                {
                    if (conversationViewModel.Recipient == this.Recipient)
                    {
                        GlobalsParameters.OpenWindows.Remove(conversationViewModel);
                        break;
                    }
                }

                FinishEvent.Set();
                window.Close();
            }
        }

        /// <summary>
        /// Metoda sprawdzająca czy dany ciąg znaków znajduje się w słowniku emotikon 
        /// </summary>
        /// <param name="text">
        /// Tekst do sprawdzenia
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
        /// Metoda konwertująca ciągn znaków na emotikonę
        /// </summary>
        /// <param name="msg">
        /// The msg.
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
                if(emoticonText!=null)
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

    }
}
