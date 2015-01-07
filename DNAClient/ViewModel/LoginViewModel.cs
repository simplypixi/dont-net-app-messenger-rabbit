// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoginViewModel.cs" company="">
//   
// </copyright>
// <summary>
//   View model okna logowania
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DNAClient.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;

    using DNAClient.View;
    using DNAClient.ViewModel.Base;

    /// <summary>
    /// View model okna logowania
    /// </summary>
    class LoginViewModel : ViewModelBase
    {
        /// <summary>
        /// Login użytkownika
        /// </summary>
        /// 
        private string login;

        private static readonly ManualResetEvent FinishEvent = new ManualResetEvent(false);

        public LoginViewModel()
        {
            this.LoginCommand = new RelayCommand(this.LoginToServer);
            this.CloseWindowCommand = new RelayCommand(this.CloseWindow);
        }

        /// <summary>
        /// Property z loginem użytkownikiem
        /// </summary>
        public string Login
        {
            get
            {
                return this.login;
            }

            set
            {
                this.login = value;
                this.RaisePropertyChanged("Login");
            }
        }

        private void Button_Click(object s, System.Windows.RoutedEventArgs e)
        {
            this.CloseWindow();
        }
        /// <summary>
        /// Komenda zamykania okna, do zbindowania w xamlu
        /// </summary>
        public RelayCommand CloseWindowCommand { get; set; }

        /// <summary>
        /// Metoda zamykania okna
        /// </summary>
        private void CloseWindow(object parameter)
        {
            var window = parameter as Window;

            if (window != null)
            {
                FinishEvent.Set();
                window.Close();
            }
        }
        /// <summary>
        /// Komenda logowania użytkownika, do zbindowania w xamlu
        /// </summary>
        public RelayCommand LoginCommand { get; set; }

        /// <summary>
        /// Metoda logowania uzytkownika
        /// </summary>
        /// <param name="parameter">
        /// The parameter
        /// </param>
        private void LoginToServer(object parameter)
        {
            GlobalsParameters.Instance.CurrentUser = this.Login;
            GlobalsParameters.openWindows = new List<ConversationViewModel>();
            ProductionWindowFactory.CreateMainWindow();

            var loginWindow = parameter as LoginWindow;
            if (loginWindow != null)
            {
                loginWindow.Close();
            }
        }
    }
}
