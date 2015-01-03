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
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;

    using DNAClient.ViewModel.Base;
    using DNAClient.View;
    /// <summary>
    /// View model głonego okna
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        
        private static readonly ManualResetEvent FinishEvent = new ManualResetEvent(false);
        private string recipient;
        private string currentuser;

        private Contact selectedContact;

        public MainWindowViewModel()
        {
            this.currentUser = GlobalsParameters.Instance.CurrentUser;
            this.NewConversationWindowCommand = new RelayCommand(this.NewConversationWindow);
            this.addFriendCommand = new RelayCommand(this.addNewFriend);
            this.CloseWindowCommand = new RelayCommand(this.CloseWindow);

            Contacts.Add(new Contact() { Name = "Maciek" });
            Contacts.Add(new Contact() { Name = "Maciej" });
            Contacts.Add(new Contact() { Name = "Mariusz" });
            Contacts.Add(new Contact() { Name = "Darek" });
        }

        /// <summary>
        /// Property odbiorcy do zbindowania w xamlu
        /// </summary>
        /// 
        public string currentUser
        {
            get
            {
                return this.currentuser;
            }

            set
            {
                this.currentuser = value;
                this.RaisePropertyChanged("currentUser");
            }
        }
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
            if (String.IsNullOrEmpty(this.Friend)) { 
                MessageBox.Show("Aby dodać kontakt, należy wpisać jego nazwę. Spróbuj ponownie.", "Błąd dodawania użytkowika");
            } else {
                bool check = false;
                foreach(Contact element in this.Contacts){
                    if (element.Name == this.Friend)
                        check = true;
                }

                if (!check)
                {
                    Contacts.Add(new Contact() { Name = this.Friend });
                }
                else
                {
                   MessageBox.Show("Taki kontakt już istnieje!", "Błąd dodawania użytkowika");
                }
            }

        }

        public RelayCommand CloseWindowCommand { get; set; }

        private void CloseWindow(object parameter)
        {
            var window = parameter as Window;

            if (window != null)
            {
                FinishEvent.Set();
                window.Close();
            }
        }
    }
}
