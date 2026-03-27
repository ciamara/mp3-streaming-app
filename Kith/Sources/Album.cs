using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Kith.Converters;

namespace Kith.Sources
{
    internal class Album : Collection
    {
        public Album(List<Song> albumSongs)
        {
            var imageConverter = new IPictureImageConverter();
            object convertedImage = imageConverter.Convert(
                value: albumSongs[0].Pictures,
                targetType: typeof(BitmapImage),
                parameter: null,
                language: string.Empty);

            this.collection_name = albumSongs[0].Album;
            this.collection_cover = convertedImage as BitmapImage;
            this.collection_description = albumSongs[0].stringArtists(albumSongs[0].Artists) + " | " + albumSongs[0].Year.ToString();
            this.collection_duration = 0.0;
            this.collection_size = 0;
            this.editable = false;
            this.collection_songs = albumSongs;

            RecalculateStats();
        }
    }
}
