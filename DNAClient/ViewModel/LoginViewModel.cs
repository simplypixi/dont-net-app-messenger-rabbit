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
    using System.Collections.Generic;
    using System.Threading;
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
        /// Manual reset event
        /// </summary>
        private static readonly ManualResetEvent FinishEvent = new ManualResetEvent(false);

        /// <summary>
        /// Login użytkownika
        /// </summary>
        private string login;

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
            GlobalsParameters.openNotifications = new List<string>();
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
                if (this.login != null)
                {
                    return this.login.ToLower();
                }
                return string.Empty;
            }

            set
            {
                this.login = value;
                this.RaisePropertyChanged("Login");
            }
        }

        /// <summary>
        /// Komenda zamykania okna, do zbindowania w xamlu
        /// </summary>
        public RelayCommand CloseWindowCommand { get; set; }

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

            if (loginWindow != null)
            {
                var authRequest = new AuthRequest
                                      {
                                          Login = this.Login,
                                          Password = loginWindow.Password.Password,
                                          RequestType = Request.Type.Login,
                                      };

                var response = rpcClient.AuthCall(authRequest.Serialize());

                rpcClient.Close();

                if (response.Status == Status.OK)
                {
                    ProductionWindowFactory.CreateMainWindow();
                    loginWindow.Close();
                }
                else
                {
                    MessageBox.Show(
                        "Błąd logowania",
                        "Wpisano błędne dane logowania. Upewnij się czy wpisałeś poprawne dane, a następnie spróbuj ponownie.");
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

            var rpcClient = new RpcWay();

            var loginWindow = parameter as LoginWindow;

            if (loginWindow != null)
            {
                if (loginWindow.Password.Password.Equals(loginWindow.RepeatPassword.Password))
                {
                    var authRequest = new AuthRequest
                                          {
                                              Login = this.Login,
                                              Password = loginWindow.Password.Password,
                                              RequestType = Request.Type.Register,
                                          };

                    var response = rpcClient.AuthCall(authRequest.Serialize());

                    rpcClient.Close();

                    if (response.Status == Status.OK)
                    {
                        ProductionWindowFactory.CreateMainWindow();
                        loginWindow.Close();
                    }
                    else
                    {
                        MessageBox.Show(
                            "Wpisano błędne dane rejestracji lub istnieje już użytkownik o nazwie:" + this.Login
                            + ".\nUpewnij się czy hasło i jego potwierdzenie są identyczne lub spróbuj wybrać inną nazwę użytkownika.",
                            "Błąd rejestracji");
                    }
                }
                else
                {
                    MessageBox.Show(
                            "Wprowadzone hasło i jego potwierdzenie są różne.",
                            "Błąd rejestracji");
                }
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
            if (loginWindow != null)
            {
                loginWindow.Hide();
                loginWindow.Height = 365;
                loginWindow.RepeatPassword.Visibility = Visibility.Visible;
                loginWindow.buttonCreate.Visibility = Visibility.Visible;
                loginWindow.buttonLog.Visibility = Visibility.Visible;
                loginWindow.buttonRegister.Visibility = Visibility.Collapsed;
                loginWindow.buttonLogin.Visibility = Visibility.Collapsed;
                loginWindow.Show();
            }
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
            if (loginWindow != null)
            {
                loginWindow.Hide();
                loginWindow.Height = 323;
                loginWindow.RepeatPassword.Visibility = Visibility.Collapsed;
                loginWindow.buttonCreate.Visibility = Visibility.Collapsed;
                loginWindow.buttonLog.Visibility = Visibility.Collapsed;
                loginWindow.buttonRegister.Visibility = Visibility.Visible;
                loginWindow.buttonLogin.Visibility = Visibility.Visible;
                loginWindow.Show();
            }
        }

        /// <summary>
        /// Metoda zamykania okna
        /// </summary>
        /// <param name="parameter">
        /// Parametr przekazany z xamla
        /// </param>
        private void CloseWindow(object parameter)
        {
            var window = parameter as Window;

            if (window != null)
            {
                FinishEvent.Set();
                window.Close();
            }
        }
    }
}
