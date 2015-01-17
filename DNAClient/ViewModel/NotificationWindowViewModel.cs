using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;


namespace DNAClient.ViewModel
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Linq;
    using System.Windows.Documents;

    using DNAClient.ViewModel.Base;
    using DNAClient.View;

    using DTO;

    using Microsoft.Win32;

    public class NotificationWindowViewModel : ViewModelBase
    {
        private string message;
        private BasicDeliverEventArgs messageTMP;
        private string sender;

        /// <summary>
        /// Typ notyfikacji jaka przyszła do użytkownika
        /// </summary>
        private NotificationType notificationType;

        public string Message
        {
            get
            {
                return message;
            }
            set
            {
                this.message = value;
                RaisePropertyChanged("Message");
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

        public RelayCommand NewConversationWindowCommand { get; set; }
        private void NewConversationWindow(object parameter)
        {
            if (this.notificationType == NotificationType.Message)
            {
                ConversationViewModel cvModel = ProductionWindowFactory.CreateConversationWindow(sender);
                GlobalsParameters.OpenWindows.Add(cvModel);
                var msg = this.messageTMP.Body.DeserializeMessageNotification();
               
                cvModel.AddToHistory(GlobalsParameters.NotificationCache[msg.Sender]);
            }

            if (this.notificationType == NotificationType.File)
            {
                this.GetFile();
            }

            this.CloseWindow(parameter);
            
        }

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

        public RelayCommand CloseWindowCommand { get; set; }

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

