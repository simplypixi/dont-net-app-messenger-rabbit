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
    using System.Threading;
    using System.Windows;

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
            var loginWindow = parameter as LoginWindow;

            if (loginWindow != null && !string.IsNullOrEmpty(this.Login))
            {
                GlobalsParameters.Instance.CurrentUser = this.Login.ToLower();
                var rpcClient = new RabbitRpcConnection();

                var authRequest = new AuthRequest
                                      {
                                          Login = this.Login.ToLower(),
                                          Password = loginWindow.Password.Password,
                                          RequestType = Request.Type.Login,
                                      };

                var response = rpcClient.AuthCall(authRequest.Serialize());
                rpcClient.Close();

                response.Status = Status.OK;
                if (response.Status == Status.OK)
                {
                    ProductionWindowFactory.CreateMainWindow();
                    this.CloseWindow(parameter);
                }
                else
                {
                    MessageBox.Show(response.Message, "Błąd logowania");
                }
            }
            else
            {
                MessageBox.Show("Należy podać login.", "Błąd logowania");
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
            var loginWindow = parameter as LoginWindow;

            if (loginWindow != null && !string.IsNullOrEmpty(this.Login))
            {
                if (!string.IsNullOrEmpty(loginWindow.Password.Password) && loginWindow.Password.Password.Equals(loginWindow.RepeatPassword.Password))
                {
                    GlobalsParameters.Instance.CurrentUser = this.Login.ToLower();

                    var rpcClient = new RabbitRpcConnection();
                    var authRequest = new AuthRequest
                                          {
                                              Login = this.Login.ToLower(),
                                              Password = loginWindow.Password.Password,
                                              RequestType = Request.Type.Register,
                                          };

                    var response = rpcClient.AuthCall(authRequest.Serialize());

                    rpcClient.Close();

                    if (response.Status == Status.OK)
                    {
                        ProductionWindowFactory.CreateMainWindow();
                        this.CloseWindow(parameter);
                    }
                    else
                    {
                        MessageBox.Show(response.Message, "Błąd rejestracji");
                    }
                }
                else
                {
                    MessageBox.Show("Nie wprowadzono haseł lub wprowadzone hasło i jego potwierdzenie są różne.", "Błąd rejestracji");
                }
            }
            else
            {
                MessageBox.Show("Należy podać login.", "Błąd rejestracji");
            }
        }

        /// <summary>
        /// Metoda zmiany wyglądu okna na tryb rejestracja
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
        /// Metoda zmiany wyglądu okna na tryb logowanie
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
