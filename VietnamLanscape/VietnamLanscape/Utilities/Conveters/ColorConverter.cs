using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace VietnamLanscape.Utilities.Conveters
{
    public class ColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            //Colors result;

            bool temp = (bool)value;

            if (temp)
            {
                return Colors.Red;
            }
            else
            {
                return Colors.Transparent;
            }

            //return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return new SolidColorBrush(Colors.Transparent);
        }
    }

    public class DonwloadedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            //Colors result;

            bool temp = (bool)value;

            if (temp)
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }

            //return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return false;
        }
    }
}
