using Microsoft.UI.Xaml.Data;
using System;
using Windows.UI.Xaml.Data;

namespace Kith.Converters
{
    public class DurationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is TimeSpan time)
            {
                return time.ToString(@"mm\:ss");
            }
            return "00:00";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}