using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DNAClient.ViewModel
{
    using DNAClient.ViewModel.Base;
    public class Contact : ViewModelBase
    {
        public Contact()
        {
            this.State = "#FFD1D1D1"; 
        }

        protected string name;
        protected string state;
        protected bool log;

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
            get { return state;  }
            set
            {
                    state = value;
                    RaisePropertyChanged("State");
            }
        }
        public bool Log
        {
            get { return log; }
            set
            {
                    log = value;
                    RaisePropertyChanged("Log");
            }
        }

    }
}
