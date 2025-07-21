using GroupDocs.Metadata.Formats.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kith.Sources
{
    public class Song
    {
        public string FileName { get; private set; }
        public string Title { get; private set; }
        public string Artist { get; private set; }
        public string Album { get; private set; }
        public string Year { get; private set; }
        public string Track { get; private set; }
        public ID3V1Genre Genre { get; private set; }
        public string Composer { get; private set; }
        //public TimeSpan Length { get; private set; }


        public Song(string filename, string title, string artist, string album, string year, string track, ID3V1Genre genre, string composer)
        {
            FileName = filename;
            Title = title;
            Artist = artist;
            Album = album;
            Year = year;
            Track = track;
            Genre = genre;    
            Composer = composer;
            //Length = length;
        }
    }
}
