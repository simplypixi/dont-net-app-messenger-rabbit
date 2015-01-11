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
    using DTO;

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
        /// Słownik przechowujący aktualne statusy użytkowników
        /// </summary>
        public IDictionary<String, PresenceStatus> status;

        private GlobalsParameters()
        {
            
        }
    }
}
