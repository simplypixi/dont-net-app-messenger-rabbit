// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BoolToVisibilityConverter.cs" company="">
//   
// </copyright>
// <summary>
//   Klasa konwertująca wartość bool na visibility do bindowania w XAML.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DNAClient.View.Converters
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Data;

    /// <summary>
    /// Klasa konwertująca wartość bool na visibility do bindowania w XAML.
    /// </summary>
    public class InvertedBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }

            if (value is bool)
            {
                return (bool)value ? Visibility.Collapsed : Visibility.Visible;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (Visibility)value != Visibility.Visible;
        }
    }
}