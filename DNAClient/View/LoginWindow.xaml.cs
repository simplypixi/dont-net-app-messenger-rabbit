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
        }

        private void LoginWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}
