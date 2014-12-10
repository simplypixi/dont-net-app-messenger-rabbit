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
    using DNAClient.ViewModel;
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void lista1_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var item = ItemsControl.ContainerFromElement(lista1, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (item != null)
            {
                Contact st = item.DataContext as Contact;
                ProductionWindowFactory.CreateConversationWindow(st.Name);
            }
        }

    }
}
