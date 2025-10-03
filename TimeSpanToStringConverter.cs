using System;
using System.Globalization;
using System.Windows.Data;

namespace practice.Converters
{
    public class TimeSpanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TimeSpan ts)
                return ts.ToString(@"hh\:mm");
            return "00:00"; // default display
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && TimeSpan.TryParse(str, out TimeSpan ts))
                return ts;

            return null; // fallback if invalid text
        }
    }
}
