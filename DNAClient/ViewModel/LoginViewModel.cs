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
    using DNAClient.RabbitFunctions;
    using DNAClient.View;
    using DNAClient.ViewModel.Base;

    /// <summary>
    /// View model okna logowania
    /// </summary>
    public class LoginViewModel : ViewModelBase
    {
        /// <summary>
        /// Login użytkownika
        /// </summary>
        private string login;

        /// <summary>
        /// Hasło użytkownika
        /// </summary>
        private string password;

        /// <summary>
        /// Potwierdzenie hasła użytkownika
        /// </summary>
        private string confirmedPassword;

        /// <summary>
        /// Konstruktor viewmodelu okna logowania 
        /// </summary>
        public LoginViewModel()
        {
            this.LoginCommand = new RelayCommand(this.LoginToServer);
            this.RegistrationCommand = new RelayCommand(this.RegistrationOnServer);
        }

        /// <summary>
        /// Property z loginem użytkownika
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

        /// <summary>
        /// Property z hasłem użytkownika
        /// </summary>
        public string Password
        {
            get
            {
                return this.password;
            }

            set
            {
                this.password = value;
                this.RaisePropertyChanged("Password");
            }
        }

        /// <summary>
        /// Property z potwierdzeniem hasła
        /// </summary>
        public string ConfirmedPassword
        {
            get
            {
                return this.confirmedPassword;
            }

            set
            {
                this.confirmedPassword = value;
                this.RaisePropertyChanged("ConfirmedPassword");
            }
        }

        /// <summary>
        /// Komenda logowania użytkownika, do zbindowania w xamlu
        /// </summary>
        public RelayCommand LoginCommand { get; set; }

        /// <summary>
        /// Komenda rejestracji użytkownika, do zbindowania w xamlu
        /// </summary>
        public RelayCommand RegistrationCommand { get; set; }

        /// <summary>
        /// Metoda logowania uzytkownika
        /// </summary>
        /// <param name="parameter">
        /// Parametr funkcji
        /// </param>
        private void LoginToServer(object parameter)
        {
            GlobalsParameters.Instance.CurrentUser = this.Login;

            var rpcClient = new RpcLogin();

            var response = rpcClient.Call(this.Login, this.Login);

            rpcClient.Close();

            if (response.IsAuthenticated)
            {
                ProductionWindowFactory.CreateMainWindow();

                var loginWindow = parameter as LoginWindow;
                if (loginWindow != null)
                {
                    loginWindow.Close();
                } 
            }
        }

        /// <summary>
        /// Metoda rejestracji uzytkownika
        /// </summary>
        /// <param name="parameter">
        /// Parametr funkcji
        /// </param>
        private void RegistrationOnServer(object parameter)
        {
            GlobalsParameters.Instance.CurrentUser = this.Login;

            var rpcClient = new RpcRegistration();

            var response = rpcClient.Call(this.Login, this.Password, this.ConfirmedPassword);

            rpcClient.Close();

            if (response.CreatedSuccessfully)
            {
                ProductionWindowFactory.CreateMainWindow();

                var loginWindow = parameter as LoginWindow;
                if (loginWindow != null)
                {
                    loginWindow.Close();
                }
            }
        }
    }
}
