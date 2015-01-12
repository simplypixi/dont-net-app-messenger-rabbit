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
    using System.Configuration;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.IO;
    using System.Text;
    using System.Windows.Documents;
    using System.Windows.Media.Animation;
    using System.Windows.Controls;
    using System.Windows.Markup;

    using DNAClient.ViewModel.Base;

    using DNAClient.View;

    using DTO;

    using global::RabbitMQ.Client;
    using global::RabbitMQ.Client.Events;


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

        private Attachment attachment;

        private ConversationWindow talkWindowGUI;
        private RichTextBox talkWindow;

        // odebrane wiadomości (do przerobienia na listę lub coś w ten deseń)
        private FlowDocument received;
        private static ConnectionFactory factory = Constants.ConnectionFactory;
        private string userPath;
        public ConversationViewModel()
        {
            this.userPath = Constants.userPath;
            this.User = GlobalsParameters.Instance.CurrentUser;
            this.SendMessageCommand = new RelayCommand(this.SendMessage);
            this.CloseWindowCommand = new RelayCommand(this.CloseWindow);
            this.AttachFileCommand = new RelayCommand(this.AttachFile);
        }

        /// <summary>
        /// Konstruktor ustawiający konkretnego odbiorcę
        /// </summary>
        /// <param name="recipient">
        /// Odbiorca
        /// </param>
        public ConversationViewModel(string recipient, ConversationWindow flowD)
            : this()
        {
            this.talkWindowGUI = flowD;
            this.talkWindow = this.talkWindowGUI.talk;
            this.talkWindow.Document = new FlowDocument();

            this.Recipient = recipient;
            if (GlobalsParameters.cache.ContainsKey(this.Recipient))
            {
                //this.talkWindow.Document = GlobalsParameters.cache[this.Recipient];

                MemoryStream ms = new MemoryStream();

                XamlWriter.Save(GlobalsParameters.cache[this.Recipient], ms);

                ms.Seek(0, SeekOrigin.Begin);

                this.talkWindow.Document = XamlReader.Load(ms) as FlowDocument;
            }
            else
            {

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

        private void CloseWindow(object parameter)
        {
            var window = parameter as Window;
            this.talkWindow = null;
            if (window != null)
            {
                foreach (ConversationViewModel cvModel in GlobalsParameters.openWindows)
                {
                    if (cvModel.Recipient == this.Recipient)
                    {
                        GlobalsParameters.openWindows.Remove(cvModel);
                        break;
                    }
                }
                FinishEvent.Set();
                window.Close();
            }
        }

        public void AddToHistory(string message)
        {
            if (this.Recipient == null)
            {
                return;
            }
            var historyFile = this.userPath + "//" + this.Recipient;
            Functions.saveFile(historyFile, message);
        }

        private void SendMessage(object parameter)
        {
            if (this.Message != null || this.attachment != null)
            {
                if (this.Message != null)
                {
                    this.Message = this.Message.Trim();
                }
                if (this.attachment != null || !string.IsNullOrEmpty(this.Message))
                {
                    var msg = string.Empty;
                    if (!string.IsNullOrEmpty(this.Message))
                    {
                        msg = DateTimeOffset.Now.ToString("dd.MM.yyyy (HH:mm:ss)") + " przez Ja:\n" + this.Message;
                    }
                    if (this.attachment != null)
                    {
                        if (!string.IsNullOrEmpty(this.Message))
                        {
                            msg += "\n";
                        }
                        msg += DateTimeOffset.Now.ToString("dd.MM.yyyy (HH:mm:ss)") + ": WYSŁANO ZAŁĄCZNIK";
                    }
                    FlowDocument flowD = new FlowDocument();
                    flowD = this.talkWindow.Document;
                    Paragraph para = new Paragraph();
                    para.Inlines.Add(msg);
                    flowD.Blocks.Add(para);
                    //this.Received = flowD;
                    this.talkWindow.Document = flowD;
                    if (!GlobalsParameters.cache.ContainsKey(this.Recipient))
                    {
                        GlobalsParameters.cache.Add(this.Recipient, new FlowDocument());
                    }
                    GlobalsParameters.cache[this.Recipient].Blocks.Add(para);
                    try
                    {
                        this.talkWindow.Document = GlobalsParameters.cache[this.Recipient];
                    }
                    catch
                    {
                        MemoryStream ms = new MemoryStream();

                        XamlWriter.Save(GlobalsParameters.cache[this.Recipient], ms);

                        ms.Seek(0, SeekOrigin.Begin);

                        this.talkWindow.Document = XamlReader.Load(ms) as FlowDocument;
                    }
                    this.SendMessageToQueue();
                    AddToHistory(msg);
                    this.Message = String.Empty;
                }
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
                                          Message = !string.IsNullOrEmpty(this.Message) ? this.Message : string.Empty,
                                          Recipient = this.Recipient,
                                          SendTime = DateTimeOffset.Now,
                                          Attachment = this.attachment
                                      };

                    var body = message.Serialize();
                    channel.BasicPublish(Constants.Exchange, Constants.keyServerRequestMessage, null, body);
                    Debug.WriteLine("{0} wysłał \"{1}\" do: {2}", this.User, this.Message, this.Recipient);
                }
            }

            this.attachment = null;
        }

        /// <summary>
        /// Metoda wywoływana za każdym razem gdy serwer coś doda do kolejki i klient to przeczyta
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        /// <param name="conversationViewModel">
        /// The conversation view model.
        /// </param>
        /// </summary>

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
                    FlowDocument flowD = new FlowDocument();
                    flowD = this.talkWindow.Document;
                    Paragraph para = new Paragraph();
                    para.Inlines.Add(msg);
                    flowD.Blocks.Add(para);
                    this.talkWindow.Document = flowD;
                    GlobalsParameters.cache[message.Sender].Blocks.Add(para);
                    if (!GlobalsParameters.cache.ContainsKey(this.Recipient))
                    {
                        GlobalsParameters.cache.Add(this.Recipient, new FlowDocument());
                    }
                    GlobalsParameters.cache[message.Sender].Blocks.Add(para);
                    try
                    {
                        this.talkWindow.Document = GlobalsParameters.cache[this.Recipient];
                    }
                    catch
                    {
                        MemoryStream ms = new MemoryStream();

                        XamlWriter.Save(GlobalsParameters.cache[this.Recipient], ms);

                        ms.Seek(0, SeekOrigin.Begin);

                        this.talkWindow.Document = XamlReader.Load(ms) as FlowDocument;
                    }
                }
            }
        }

        void AttachFile(object param)
        {

            var window = param as ConversationWindow;
            string sendFileText = window.SendFile.Content.ToString();
            if (sendFileText.Equals("Dodaj plik"))
            {
                Microsoft.Win32.OpenFileDialog win = new Microsoft.Win32.OpenFileDialog();
                win.Multiselect = false;

                Nullable<bool> result = win.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    string filePath = win.FileName;
                    Debug.Print(filePath);

                    this.attachment = new Attachment();

                    byte[] bytes = File.ReadAllBytes(filePath);

                    this.attachment.Data = bytes;
                    this.attachment.Name = win.SafeFileName;
                    this.attachment.MimeType = string.Empty;


                }
                window.SendFile.Content = "Usuń";
            }
            else if (sendFileText.Equals("Usuń"))
            {
                this.attachment = null;
                window.SendFile.Content = "Dodaj plik";
            }
        }
    }

}
