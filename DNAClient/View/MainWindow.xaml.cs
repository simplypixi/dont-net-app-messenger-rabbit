using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DNAClient.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();

            Console.WriteLine("Dodano 1.");
            ContactList.Contacts.Add(new Contact() { Name = "Darek" });
            Console.WriteLine("Dodano 2.");
            ContactList.Contacts.Add(new Contact() { Name = "Maciek" });
            ContactList.Contacts.Add(new Contact() { Name = "Maciej" });
            ContactList.Contacts.Add(new Contact() { Name = "Mariusz" });
        }

        private void button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            //    Tworzę kontakt.
            Contact contact = new Contact() { Name = loginBox.Text };
            //    Dodaję do listy.
          
            ContactList.Contacts.Add(contact);
            Console.WriteLine("Dodano: " + ContactList.Contacts[ContactList.Contacts.Count-1].Name);

        }

        private void lista1_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var item = ItemsControl.ContainerFromElement(lista1, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (item != null)
            {
                Contact st = item.DataContext as Contact;
            }
        }
    }
}
