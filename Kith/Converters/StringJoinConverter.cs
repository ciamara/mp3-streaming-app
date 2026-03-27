using Microsoft.UI.Xaml.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kith.Converters
{
    internal class StringJoinConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is IEnumerable collection)
            {
                string[] items = collection.Cast<object>()
                                          .Select(x => x?.ToString() ?? string.Empty)
                                          .ToArray();

                string separator = parameter?.ToString() ?? ", ";

                return string.Join(separator, items);
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
