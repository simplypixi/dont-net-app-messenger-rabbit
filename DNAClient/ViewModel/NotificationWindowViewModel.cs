using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

    public class NotificationWindowViewModel : ViewModelBase
    {
        private string message;
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

        public NotificationWindowViewModel(string sender, string type)
        {
            this.CloseWindowCommand = new RelayCommand(this.CloseWindow);

            if (type == "status")
                this.Message = sender + " zmienił status...";
            if (type == "message")
                this.Message = sender + " przesyła wiadomość...";
            if(type == "file")
                this.Message = sender + " przesyła plik...";
        }

        public RelayCommand CloseWindowCommand { get; set; }

        private void CloseWindow(object parameter)
        {
            var window = parameter as Window;

            if (window != null)
            {
                window.Close();
            }
        }


    }
}

