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

    using DTO;

    /// <summary>
    /// View model głonego okna
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        /// <summary>
        /// Odbiorca (później będzie to lista znajomych)
        /// </summary>
        private string recipient;

        private Contact selectedContact;
        private string userPath;

        public MainWindowViewModel()
        {
            this.userPath = Constants.userPath;
            this.NewConversationWindowCommand = new RelayCommand(this.NewConversationWindow);
            this.OpenHistoryCommand = new RelayCommand(this.OpenHistory);
            this.addFriendCommand = new RelayCommand(this.addNewFriend);

            Contacts.Add(new Contact() { Name = "Maciek" });
            Contacts.Add(new Contact() { Name = "Maciej" });
            Contacts.Add(new Contact() { Name = "Mariusz" });
            Contacts.Add(new Contact() { Name = "Darek" });
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

        public Contact SelectedContact {
            get
            {
                return this.selectedContact;
            }

            set
            {
                this.selectedContact = value;
                this.RaisePropertyChanged("SelectedContact");
            }

        }

        /// <summary>
        /// Komenda otwarcia historii rozmowy
        /// </summary>
        public RelayCommand OpenHistoryCommand { get; set; }

        /// <summary>
        /// Metoda otwierająca historię rozmowy
        /// </summary>
        /// <param name="parameter">
        /// The parameter.
        /// </param>
        private void OpenHistory(object parameter)
        {
            var historyFile = this.userPath + "//" + this.SelectedContact.Name;
            System.Diagnostics.Process.Start(@historyFile);
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
           ProductionWindowFactory.CreateConversationWindow(this.SelectedContact.Name);
        }



         public RelayCommand addFriendCommand { get; set; }
        private string friendName;

        public ObservableCollection<Contact> contacts =
            new ObservableCollection<Contact>();

        public ObservableCollection<Contact> Contacts
        {
            get {
                return contacts; }
            set { 
                contacts = value;
                RaisePropertyChanged("Contacts");
            }
        }

        public string Friend
        {
            get
            {
                return this.friendName;
            }

            set
            {
                this.friendName = value;
                this.RaisePropertyChanged("Friend");
            }
        }

        private void addNewFriend(object parameter){
            Contacts.Add(new Contact() { Name = this.Friend });
        }
    }
}
