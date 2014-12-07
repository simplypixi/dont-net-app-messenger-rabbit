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
        private string login;

        public LoginViewModel()
        {
            this.LoginCommand = new RelayCommand(this.LoginToServer);
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
                this.RaisePropertyChanged("Podaj login...");
            }
        }

        private void Button_Click(object s, System.Windows.RoutedEventArgs e)
        {
            this.CloseWindow();
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
            ProductionWindowFactory.CreateMainWindow();

            var loginWindow = parameter as LoginWindow;
            if (loginWindow != null)
            {
                loginWindow.Close();
            }
        }
    }
}
