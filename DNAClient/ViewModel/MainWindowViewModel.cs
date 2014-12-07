// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindowViewModel.cs" company="">
//   
// </copyright>
// <summary>
//   View model głównego okna
// </summary>
// --------------------------------------------------------------------------------------------------------------------
using System.Collections.ObjectModel;

namespace DNAClient.ViewModel
{
    using DNAClient.ViewModel.Base;
    using DNAClient.View;
    /// <summary>
    /// View model głonego okna
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        /// <summary>
        /// Odbiorca (później będzie to lista znajomych)
        /// </summary>
        private string recipient;

        public MainWindowViewModel()
        {
            this.NewConversationWindowCommand = new RelayCommand(this.NewConversationWindow);
        }

        /// <summary>
        /// Property odbiorcy do zbindowania w xamlu
        /// </summary>
        public string Recipient
        {
            get
            {
                return this.recipient;
            }

            set
            {
                this.recipient = value;
                this.RaisePropertyChanged("Recipient");
            }
        }

        /// <summary>
        /// Komenda otwarcia nowego okna konweracji
        /// </summary>
        public RelayCommand NewConversationWindowCommand { get; set; }

        /// <summary>
        /// Metoda otwierająca nowe okno konwersacji
        /// </summary>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        private void NewConversationWindow(object parameter)
        {
           ProductionWindowFactory.CreateConversationWindow(this.Recipient);
        }

        public ObservableCollection<Contact> contacts =
            new ObservableCollection<Contact>();

        public ObservableCollection<Contact> Contacts
        {
            get { return contacts; }
            set { contacts = value; }
        }
    }
}
