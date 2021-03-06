﻿using System;
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


        public void Start_Loading()
        {
            this.loader.IsBusy = true;
            this.win.Visibility = System.Windows.Visibility.Collapsed;
        }

        public void Stop_Loading()
        {
            this.loader.IsBusy = false;
            this.win.Visibility = System.Windows.Visibility.Visible;
        }

    }
}
