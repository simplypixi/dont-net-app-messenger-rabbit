// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoginWindow.xaml.cs" company="">
//   
// </copyright>
// <summary>
//   Okno logowania
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DNAClient.View
{
    using System.Windows.Input;
    using System.Windows.Controls;

    /// <summary>
    /// Okno logowania
    /// </summary>
    public partial class LoginWindow
    {
        public LoginWindow()
        {
            InitializeComponent();

            RepeatPassword.Password = "Powtórz hasło";
            Password.Password = "Wpisz hasło";
            Login.Text = "Wpisz login";

            this.Stop_Loading();
        }

        private void LoginWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }


        private void Login_GotFocus(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Console.WriteLine(sender.GetType().ToString());
            if (sender.GetType() == typeof(System.Windows.Controls.TextBox))
            {
                var field = sender as System.Windows.Controls.TextBox;

                if (field.Text == "Wpisz login")
                {
                    field.Text = "";
                }
            } else if (sender.GetType() == typeof(System.Windows.Controls.PasswordBox)){
                var field = sender as System.Windows.Controls.PasswordBox;

                if (field.Password == "Wpisz hasło" || field.Password == "Powtórz hasło")
                {
                    field.Password = "";
                }
            }
        }

        public void Start_Loading()
        {
            this.loader.IsBusy = true;
            this.win.Visibility = System.Windows.Visibility.Collapsed;
        }

        public void Stop_Loading()
        {
            this.loader.IsBusy = false;
            this.win.Visibility = System.Windows.Visibility.Visible;
        }


    }
}
