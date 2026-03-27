using Microsoft.UI.Xaml.Data;
using System;

namespace Kith.Converters
{
    public class BooleanToGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool liked && liked)
            {
                return "\uEB52";
            }
            return "\uEB51";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}