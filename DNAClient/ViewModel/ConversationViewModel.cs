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
            this.AttachFileCommand = new RelayCommand(this.AttachFile);
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
            if (GlobalsParameters.cache.ContainsKey(this.Recipient))
            {
                this.Received = GlobalsParameters.cache[this.Recipient];
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

        public RelayCommand AttachFileCommand { get; set; }

        private void CloseWindow(object parameter)
        {
            var window = parameter as Window;

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
                        msg = DateTimeOffset.Now + " przez Ja:\n" + this.Message + "\n";
                    }
                    if (this.attachment != null)
                    {
                        if (!string.IsNullOrEmpty(this.Message))
                        {
                            msg += "\n";
                        }
                        msg += DateTimeOffset.Now + ": WYSŁANO ZAŁĄCZNIK\n";
                    }
                    this.Received += msg + "\n";
                    if (!GlobalsParameters.cache.ContainsKey(this.Recipient))
                    {
                        GlobalsParameters.cache.Add(this.Recipient, String.Empty);
                    }
                    GlobalsParameters.cache[this.Recipient] += msg + "\n";
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
                                          Message = !string.IsNullOrEmpty(this.Message) ? this.Message + "\n" : string.Empty,
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
                    var msg = message.SendTime + " przez " + message.Sender + ":\n" + message.Message + "\n";
                    this.AddToHistory(msg);
                    this.Received += msg;
                    if (!GlobalsParameters.cache.ContainsKey(this.Recipient))
                    {
                        GlobalsParameters.cache.Add(this.Recipient, String.Empty);
                    }
                    GlobalsParameters.cache[message.Sender] += msg;
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
