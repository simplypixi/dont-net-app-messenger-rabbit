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
        /// Konstruktor viewmodelu okna logowania 
        /// </summary>
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
                this.RaisePropertyChanged("Login");
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
    }
}
