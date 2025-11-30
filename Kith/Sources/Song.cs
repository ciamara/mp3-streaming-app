using GroupDocs.Metadata.Formats.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagLib;

namespace Kith.Sources
{
    public class Song
    {
        public Guid ID { get; private set; }

        public int Index { get; private set; }
        public string FileName { get; private set; }
        public string Title { get; private set; }
        public string[] Artists { get; private set; }
        public string Album { get; private set; }
        public uint Year { get; private set; }
        public uint Track { get; private set; }
        public string[] Genres { get; private set; }
        public TimeSpan Duration { get; private set; }
        public IPicture[] Pictures { get; private set; }


        public Song(int index, string filename, string title, string[] artists, string album, uint year, uint track, string[] genres, TimeSpan duration, IPicture[] pictures)
        {
            ID = Guid.NewGuid();
            Index = index;
            FileName = filename;
            Title = title;
            Artists = artists;
            Album = album;
            Year = year;
            Track = track;
            Genres = genres;    
            Duration = duration;
            Pictures = pictures;
        }
    }
}
