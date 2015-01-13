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
    using System.Windows.Documents;

    using DNAClient.RabbitFunctions;
    using DNAClient.View;
    using DNAClient.ViewModel.Base;
    using DTO;

    /// <summary>
    /// View model okna logowania
    /// </summary>
    public class LoginViewModel : ViewModelBase
    {
        /// <summary>
        /// Login użytkownika
        /// </summary>
        /// 
        private string login;

        private static readonly ManualResetEvent FinishEvent = new ManualResetEvent(false);

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
            GlobalsParameters.Instance.CurrentUser = this.Login;
            GlobalsParameters.openWindows = new List<ConversationViewModel>();
            GlobalsParameters.cache = new Dictionary<string, FlowDocument>();
            GlobalsParameters.openNotifications = new List<String>();
            GlobalsParameters.notificationCache = new Dictionary<string, string>();
            this.LoginCommand = new RelayCommand(this.LoginToServer);
            this.CloseWindowCommand = new RelayCommand(this.CloseWindow);
            this.RegistrationCommand = new RelayCommand(this.RegistrationOnServer);
            this.ToLogCommand = new RelayCommand(this.ToLog);
            this.ToRegistrationCommand = new RelayCommand(this.ToRegistration);
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
        /// Komenda zmiany okna na logowanie
        /// </summary>
        public RelayCommand ToLogCommand { get; set; }
        /// <summary>
        /// Komenda zmiany okna na rejestrację
        /// </summary>
        public RelayCommand ToRegistrationCommand { get; set; }

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
            var rpcClient = new RpcWay();

            var loginWindow = parameter as LoginWindow;

            var authRequest = new AuthRequest
            {
                Login = this.login,
                Password = this.password,
                RequestType = Request.Type.Login,
            };

            var response = rpcClient.AuthCall(authRequest.Serialize());

            rpcClient.Close();

            response.Status = Status.OK;

            if (response.Status == Status.OK)
            {
                ProductionWindowFactory.CreateMainWindow();

                if (loginWindow != null)
                {
                    loginWindow.Close();
                }
            }
            else
            {
                MessageBox.Show("Błąd logowania", "Wpisano błędne dane logowania. Upewnij się czy wpisałeś poprawne dane, a następnie spróbuj ponownie.");
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

            var rpcClient = new RpcWay();

            var loginWindow = parameter as LoginWindow;
            var authRequest = new AuthRequest
            {
                Login = login,
                Password = password,
                RequestType = Request.Type.Register,
            };

            var response = rpcClient.AuthCall(authRequest.Serialize());

            rpcClient.Close();

            if (response.Status == Status.OK)
            {
                ProductionWindowFactory.CreateMainWindow();
                if (loginWindow != null)
                {
                    loginWindow.Close();
                }
            }
            else
            {
                MessageBox.Show("Błąd rejestracji", "Wpisano błędne dane rejestracji lub istnieje już użytkownik o nazwie:" + this.Login + ".\nUpewnij się czy hasło i jego potwierdzenie są identyczne lub spróbuj wybrać inną nazwę użytkownika.");
            }
        }

        /// <summary>
        /// Metoda zmiany na rejestrację
        /// </summary>
        /// <param name="parameter">
        /// Parametr funkcji
        /// </param>
        private void ToRegistration(object parameter)
        {
            var loginWindow = parameter as LoginWindow;
            loginWindow.Hide();
            loginWindow.Height = 365;
            loginWindow.repeatPassword.Visibility = Visibility.Visible;
            loginWindow.buttonCreate.Visibility = Visibility.Visible;
            loginWindow.buttonLog.Visibility = Visibility.Visible;
            loginWindow.buttonRegister.Visibility = Visibility.Collapsed;
            loginWindow.buttonLogin.Visibility = Visibility.Collapsed;
            loginWindow.Show();

        }

        /// <summary>
        /// Metoda zmiany na logowanie
        /// </summary>
        /// <param name="parameter">
        /// Parametr funkcji
        /// </param>
        private void ToLog(object parameter)
        {
            var loginWindow = parameter as LoginWindow;
            loginWindow.Hide();
            loginWindow.Height = 323;
            loginWindow.repeatPassword.Visibility = Visibility.Collapsed;
            loginWindow.buttonCreate.Visibility = Visibility.Collapsed;
            loginWindow.buttonLog.Visibility = Visibility.Collapsed;
            loginWindow.buttonRegister.Visibility = Visibility.Visible;
            loginWindow.buttonLogin.Visibility = Visibility.Visible;
            loginWindow.Show();

        }
    }
}
