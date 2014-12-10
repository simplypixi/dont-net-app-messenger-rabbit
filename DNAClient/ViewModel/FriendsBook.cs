using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;


namespace DNAClient.ViewModel
{
    using DNAClient.ViewModel.Base;
    public class FriendsBook : ViewModelBase
    {
        public RelayCommand addFriendCommand { get; set; }
        private string friendName;

        public FriendsBook()
        {
           this.addFriendCommand = new RelayCommand(this.addNewFriend);

           Contacts.Add(new Contact() { Name = "Maciek" });
           Contacts.Add(new Contact() { Name = "Maciej" });
           Contacts.Add(new Contact() { Name = "Mariusz" });
           Contacts.Add(new Contact() { Name = "Darek" });

        }

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
