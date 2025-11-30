using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Kith.Converters
{
    internal class IPictureImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is TagLib.IPicture[] pictures && pictures.Length > 0)
            {
                byte[] imageData = pictures[0].Data.Data;

                if (imageData != null && imageData.Length > 0)
                {
                    try
                    {
                        BitmapImage bitmap = new BitmapImage();

                        using (var stream = new MemoryStream(imageData))
                        {
                            stream.Seek(0, SeekOrigin.Begin);
                            var winrtStream = stream.AsRandomAccessStream();

                            bitmap.SetSource(winrtStream);
                        }
                        return bitmap;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error converting image: {ex.Message}");
                    }
                }
            }
            return null;
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
