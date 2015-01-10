﻿// --------------------------------------------------------------------------------------------------------------------
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

        public static List<ConversationViewModel> openWindows { get; set; }

        private GlobalsParameters()
        {
            
        }
    }
}
