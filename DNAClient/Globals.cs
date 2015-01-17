// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Globals.cs" company="">
//   
// </copyright>
// <summary>
//   Klasa z globalnymi parametrami aplikacji
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DNAClient
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Windows.Documents;

    using DNAClient.ViewModel;

    /// <summary>
    /// Klasa z globalnymi parametrami aplikacji
    /// </summary>
    public class GlobalsParameters
    {
        /// <summary>
        /// Prywatna instancja klasy 
        /// </summary>
        private static GlobalsParameters instance;

        /// <summary>
        /// Prywatny konstruktor klasy
        /// </summary>
        private GlobalsParameters()
        {
            OpenWindows = new List<ConversationViewModel>();
            TextCache = new Dictionary<string, FlowDocument>();
            OpenNotifications = new List<string>();
            NotificationCache = new Dictionary<string, string>();
        }

        /// <summary>
        /// Property zapewniające dostęp do instancji klasy z parametrami - singleton
        /// </summary>
        public static GlobalsParameters Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new GlobalsParameters();
                }

                return instance;
            }
        }

        /// <summary>
        /// Lista aktualnie otwartych okienek rozmów
        /// </summary>
        public static List<ConversationViewModel> OpenWindows { get; set; }

        /// <summary>
        /// Lista aktualnie wyświetlonych okienek powiadomien
        /// </summary>
        public static List<string> OpenNotifications { get; set; }

        /// <summary>
        /// 'Pamięć cache' dla rozmów, przechowująca tekst rozmów od czasu zalogowania do czasu wyłączenia komunikatora
        /// </summary>
        public static IDictionary<string, FlowDocument> TextCache { get; set; }

        /// <summary>
        /// 'Pamięć cache' do trzymania wszystkich wiadomości od utowrzenia danego notification do jego zamknięcia
        /// </summary>
        public static IDictionary<string, string> NotificationCache { get; set; }

        /// <summary>
        /// Aktualnie zalogowany użytkownik
        /// </summary>
        public string CurrentUser { get; set; }

        /// <summary>
        /// Lista kontaktów zalogowanego użytkownika
        /// </summary>
        public ObservableCollection<Contact> Contacts { get; set; }
    }
}
