using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;


namespace DNAClient.View
{
    using DNAClient.ViewModel.Base;
    public class FriendsBook : ViewModelBase
    {
        public FriendsBook()
        {
        }

        public ObservableCollection<Contact> contacts =
            new ObservableCollection<Contact>();

        public ObservableCollection<Contact> Contacts
        {
            get {
                return contacts; }
            set { 
                contacts = value; }
        }



    }
}
