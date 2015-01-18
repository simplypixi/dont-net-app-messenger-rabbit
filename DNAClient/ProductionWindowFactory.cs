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

    using RabbitMQ.Client.Events;

    /// <summary>
    /// Fabryka nowych okien
    /// </summary>
    public class ProductionWindowFactory
    {
        /// <summary>
        /// Metoda tworząca nowe okno rozmowy. ViewModel jest zwracany, po to by
        /// dodać go do listy otwartych okien rozmowy
        /// </summary>
        /// <param name="recipient">
        /// Odbiorca z listy znajomych
        /// </param>
        /// <returns>
        /// The <see cref="ConversationViewModel"/>.
        /// </returns>
        public static ConversationViewModel CreateConversationWindow(string recipient)
        {
            ConversationWindow window = new ConversationWindow();

            ConversationViewModel conversationViewModelModel = new ConversationViewModel(recipient, window);
            conversationViewModelModel.LoadEmoticons();
            window.DataContext = conversationViewModelModel;
            window.Show();
            return conversationViewModelModel;
        }

        /// <summary>
        /// Metoda tworząca okno powiadomień
        /// </summary>
        /// <param name="sender">
        /// Nazwa wysyłającego notyfikację
        /// </param>
        /// <param name="mess">
        /// Dane o wiadomości z rabbita
        /// </param>
        /// <param name="notificationType">
        /// Typ notyfikacji
        /// </param>
        public static void CreateNotificationWindow(string sender, BasicDeliverEventArgs mess, NotificationType notificationType)
        {
            NotificationWindow window = new NotificationWindow
            {
                DataContext = new NotificationWindowViewModel(sender, mess, notificationType)
            };
            window.Show();
        }

        /// <summary>
        /// Metoda tworząca główne okno programu
        /// </summary>
        public static void CreateMainWindow()
        {
            MainWindow window = new MainWindow();
            window.Show();
        }
    }
}
