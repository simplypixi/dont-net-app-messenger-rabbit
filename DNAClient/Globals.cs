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
    using System;
    using System.Collections.Generic;

    using DNAClient.View;
    using DNAClient.ViewModel;

    /// <summary>
    /// Klasa z globalnymi parametrami aplikacji
    /// </summary>
    public class GlobalsParameters
    {
        private static GlobalsParameters instance;


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
        /// Aktualnie zalogowany użytkownik
        /// </summary>
        public string CurrentUser { get; set; }

        /// <summary>
        /// Lista aktualnie otwartych okienek rozmów
        /// </summary>
        public static List<ConversationViewModel> openWindows { get; set; }

        /// <summary>
        /// Lista aktualnie wyświetlonych okienek powiadomien
        /// </summary>
        public static List<String> openNotifications { get; set; }

        /// <summary>
        /// 'Pamięć cache' dla rozmów, przechowująca tekst rozmów od czasu zalogowania do czasy wyłączenia komunikatora
        /// </summary>
        public static IDictionary<String, String> cache { get; set; }

        /// <summary>
        /// 'Pamięć cache' do trzymania wszystkich wiadomości od utowrzenia danego notification do jego zamknięcia
        /// </summary>
        public static IDictionary<String, String> notificationCache { get; set; }

        private GlobalsParameters()
        {
            
        }
    }
}
