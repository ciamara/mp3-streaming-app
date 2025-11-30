using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kith.Converters
{
    internal class IndexFinderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DependencyObject element)
            {
                ListViewItem container = FindParent<ListViewItem>(element);

                if (container != null)
                {
                    ListView listview = FindParent<ListView>(container);

                    if (listview != null)
                    {
                        int index = listview.IndexFromContainer(container);

                        if (index >= 0)
                        {
                            return (index + 1).ToString();
                        }
                    }
                }
            }
            return "-";
        }
        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            if (parent == null) return null;

            T parentT = parent as T;
            if (parentT != null)
            {
                return parentT;
            }
            return FindParent<T>(parent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
