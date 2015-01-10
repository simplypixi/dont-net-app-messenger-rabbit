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

    using RabbitMQ.Client;
    using RabbitMQ.Client.Events;

    /// <summary>
    /// Fabryka nowych okien
    /// </summary>
    public class ProductionWindowFactory
    {
        public static ConversationViewModel CreateConversationWindow(string recipient)
        {
            ConversationViewModel cvModel = new ConversationViewModel(recipient);
            ConversationWindow window = new ConversationWindow
            {
                DataContext = cvModel,
            };
            window.Show();
            return cvModel;
        }

        public static void CreateNotificationWindow(string sender, BasicDeliverEventArgs mess, NotificationType notificationType)
        {
            NotificationWindow window = new NotificationWindow
            {
                DataContext = new NotificationWindowViewModel(sender, mess, notificationType)
            };
            window.Show();
        }

        public static void CreateMainWindow()
        {
            MainWindow window = new MainWindow();
            window.Show();
        }
    }
}
