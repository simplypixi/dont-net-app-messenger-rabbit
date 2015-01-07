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
