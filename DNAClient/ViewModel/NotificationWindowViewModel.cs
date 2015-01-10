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
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Linq;

    using DNAClient.ViewModel.Base;
    using DNAClient.View;

    using DTO;

    public class NotificationWindowViewModel : ViewModelBase
    {
        private string message;
        private BasicDeliverEventArgs messageTMP;
        private string sender;

        public string type { get; set; }
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

        public NotificationWindowViewModel(string sender, BasicDeliverEventArgs mess, string type)
        {
            this.CloseWindowCommand = new RelayCommand(this.CloseWindow);

            if (type == "status")
                this.Message = sender + " zmienił status...";
            if (type == "message")
                this.Message = sender + " przesyła wiadomość...";
            if(type == "file")
                this.Message = sender + " przesyła plik...";

            this.type = type;
            this.messageTMP = mess;
            this.sender = sender;
            GlobalsParameters.openNotifications.Add(sender+type);
            this.NewConversationWindowCommand = new RelayCommand(this.NewConversationWindow);
        }

        public RelayCommand NewConversationWindowCommand { get; set; }
        private void NewConversationWindow(object parameter)
        {
            ConversationViewModel cvModel = ProductionWindowFactory.CreateConversationWindow(sender);
            GlobalsParameters.openWindows.Add(cvModel);
            var msg = this.messageTMP.Body.DeserializeMessageNotification();
            cvModel.AddToHistory(GlobalsParameters.notificationCache[msg.Sender + "message"]);
            this.CloseWindow(parameter);
        }
        

        public RelayCommand CloseWindowCommand { get; set; }

        private void CloseWindow(object parameter)
        {
            if (!GlobalsParameters.cache.ContainsKey(this.sender))
            {
                GlobalsParameters.cache.Add(this.sender, String.Empty);
            }
            var msg = this.messageTMP.Body.DeserializeMessageNotification();
            GlobalsParameters.openNotifications.Remove(msg.Sender + "message");
            GlobalsParameters.notificationCache.Remove(msg.Sender + "message");
            var window = parameter as Window;

            if (window != null)
            {
                window.Close();
            }
        }


    }
}

