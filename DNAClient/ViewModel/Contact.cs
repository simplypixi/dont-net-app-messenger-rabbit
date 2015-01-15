// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Contact.cs" company="">
//   
// </copyright>
// <summary>
//   Klasa reprezentująca kontakt z listy znajomych użytkownika
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DNAClient.ViewModel
{
    using DNAClient.ViewModel.Base;

    using DTO;

    /// <summary>
    /// Klasa reprezentująca kontakt z listy znajomych użytkownika
    /// </summary>
    public class Contact : ViewModelBase
    {
        public Contact()
        {
            this.PresenceStatus = PresenceStatus.Offline;
        }

        protected string name;
        protected bool log;
        private PresenceStatus presenceStatus;

        public string Name
        {
            get { return name; }
            set
            {
                if (name != value)
                {
                    name = value;
                    RaisePropertyChanged("Name");
                }
            }
        }

        public string State
        {
            get
            {
                switch (this.PresenceStatus)
                {
                    case PresenceStatus.Afk:
                        return "Red";
                    case PresenceStatus.Online:
                    case PresenceStatus.Login:
                        return "Green";
                    case PresenceStatus.Offline:
                        return "#FFD1D1D1";
                    default:
                        return "#FFD1D1D1";
                }
            }
            
        }

        public PresenceStatus PresenceStatus
        {
            get
            {
                return this.presenceStatus;
            }

            set
            {
                this.presenceStatus = value;
                this.RaisePropertyChanged("PresenceStatus");
                this.RaisePropertyChanged("State");
            }
        }
    }
}
