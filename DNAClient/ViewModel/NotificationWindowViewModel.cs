// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotificationWindowViewModel.cs" company="">
//   
// </copyright>
// <summary>
//   ViewModel okna notyfikacji
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DNAClient.ViewModel
{
    using System.IO;
    using System.Windows;
    using System.Windows.Documents;

    using DNAClient.ViewModel.Base;

    using DTO;

    using Microsoft.Win32;

    using RabbitMQ.Client.Events;

    /// <summary>
    /// ViewModel okna notyfikacji
    /// </summary>
    public class NotificationWindowViewModel : ViewModelBase
    {
        private string message;
        private BasicDeliverEventArgs messageTMP;
        private string sender;

        /// <summary>
        /// Typ notyfikacji jaka przyszła do użytkownika
        /// </summary>
        private NotificationType notificationType;

        /// <summary>
        /// Polecenie otwarcia nowego okna rozmowy
        /// </summary>
        public RelayCommand NewConversationWindowCommand { get; set; }

        /// <summary>
        /// Polecenie zamknięcia okna notyfikacji
        /// </summary>
        public RelayCommand CloseWindowCommand { get; set; }

        public string Message
        {
            get
            {
                return message;
            }
            set
            {
                this.message = value;
                this.RaisePropertyChanged("Message");
            }
        }

        public NotificationWindowViewModel()
        {
        }

        public NotificationWindowViewModel(string sender, BasicDeliverEventArgs mess, NotificationType notificationType)
        {
            this.CloseWindowCommand = new RelayCommand(this.CloseWindow);
            this.notificationType = notificationType;

            switch (notificationType)
            {
                case NotificationType.Message:
                    this.Message = sender + " przesyła wiadomość...";
                    break;
                case NotificationType.File:
                    this.Message = sender + " przesyła plik...";
                    break;
                default:
                    this.Message = sender + " ...";
                    break;
            }

            this.messageTMP = mess;
            this.sender = sender;
            this.NewConversationWindowCommand = new RelayCommand(this.NewConversationWindow);
            if (this.notificationType == NotificationType.Message)
            {
                GlobalsParameters.OpenNotifications.Add(this.sender);
            }
        }

        /// <summary>
        /// Otwarcie nowego okna rozmowy
        /// </summary>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        private void NewConversationWindow(object parameter)
        {
            if (this.notificationType == NotificationType.Message)
            {
                ConversationViewModel conversationViewModel = ProductionWindowFactory.CreateConversationWindow(sender);
                GlobalsParameters.OpenWindows.Add(conversationViewModel);
                var msg = this.messageTMP.Body.DeserializeMessageNotification();
               
                conversationViewModel.AddToHistory(GlobalsParameters.NotificationCache[msg.Sender]);
            }

            if (this.notificationType == NotificationType.File)
            {
                this.GetFile();
            }

            this.CloseWindow(parameter);
        }

        /// <summary>
        /// Odbieranie pliku z załącznika wiadomości
        /// </summary>
        private void GetFile()
        {
            if (this.messageTMP != null)
            {
                var body = this.messageTMP.Body;
                var message = body.DeserializeMessageNotification();
                var attachment = message.Attachment;

                if (attachment != null)
                {
                    var saveFileDialog = new SaveFileDialog
                                             {
                                                 FileName = attachment.Name,
                                                 Filter = "Wszystkie pliki|*.*"
                                             };

                    if (saveFileDialog.ShowDialog().HasValue)
                    {
                        var fileName = saveFileDialog.FileName;

                        File.WriteAllBytes(fileName, attachment.Data);
                    }
                }
            }
        }

        private void CloseWindow(object parameter)
        {
            if (this.notificationType == NotificationType.Message)
            {
                if (!GlobalsParameters.TextCache.ContainsKey(this.sender))
                {
                    GlobalsParameters.TextCache.Add(this.sender, new FlowDocument());
                }

                var msg = this.messageTMP.Body.DeserializeMessageNotification();
                GlobalsParameters.NotificationCache.Remove(msg.Sender);
                GlobalsParameters.OpenNotifications.Remove(msg.Sender);
            }
            
            var window = parameter as Window;

            if (window != null)
            {
                window.Close();
            }
        }
    }
}

