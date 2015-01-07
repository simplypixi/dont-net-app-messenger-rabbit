// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProductionWindowFactory.cs" company="">
//   
// </copyright>
// <summary>
//   Fabryka nowych okien
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DNAClient
{
    using DNAClient.View;
    using DNAClient.ViewModel;

    /// <summary>
    /// Fabryka nowych okien
    /// </summary>
    public class ProductionWindowFactory
    {
        public static void CreateConversationWindow(string recipient)
        {
            ConversationWindow window = new ConversationWindow
            {
                DataContext = new ConversationViewModel(recipient)
            };
            window.Show();
        }

        public static void CreateNotificationWindow(string sender, string type)
        {
            NotificationWindow window = new NotificationWindow
            {
                DataContext = new NotificationWindowViewModel(sender, type)
            };
            window.Show();
        }

        public static void CreateMainWindow()
        {
            MainWindow window = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };
            window.Show();
        }
    }
}
