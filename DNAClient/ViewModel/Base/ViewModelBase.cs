// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ViewModelBase.cs" company="">
//   
// </copyright>
// <summary>
//   Klasa bazowa dla wszystkich view modeli
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DNAClient.ViewModel.Base
{
    using System;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Threading;

    /// <summary>
    /// Klasa bazowa dla wszystkich view modeli
    /// </summary>
    public class ViewModelBase : INotifyPropertyChanged
    {
        

        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string prop)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
            }
        }

        bool? _CloseWindowFlag;
        public bool? CloseWindowFlag
        {
            get { return _CloseWindowFlag; }
            set
            {
                _CloseWindowFlag = value;
                RaisePropertyChanged("CloseWindowFlag");
            }
        }

        public virtual void CloseWindow(bool? result = true)
        {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                CloseWindowFlag = CloseWindowFlag == null
                    ? true
                    : !CloseWindowFlag;
            }));
        }
    }
}
