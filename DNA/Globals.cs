// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Globals.cs" company="DONTNET">
//   
// </copyright>
// <summary>
//   Klasa z globalnymi parametrami aplikacji
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DNA
{
    using System.Collections.Generic;

    using DTO;

    /// <summary>
    /// Klasa z globalnymi parametrami aplikacji
    /// </summary>
    public class GlobalsParameters
    {
        /// <summary>
        /// The instance.
        /// </summary>
        private static GlobalsParameters instance;

        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static GlobalsParameters Instance
        {
            get
            {
                return instance ?? (instance = new GlobalsParameters(new Dictionary<string, PresenceStatus>()));
            }
        }

        /// <summary>
        /// Słownik przechowujący aktualne statusy użytkowników
        /// </summary>
        public IDictionary<string, PresenceStatus> Status { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalsParameters"/> class. 
        /// Prevents a default instance of the <see cref="GlobalsParameters"/> class from being created.
        /// </summary>
        /// <param name="status">
        /// The status.
        /// </param>
        public GlobalsParameters(IDictionary<string, PresenceStatus> status)
        {
            this.Status = status;
        }
    }
}
