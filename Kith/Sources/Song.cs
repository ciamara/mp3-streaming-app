using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kith.Sources
{
    public class Song
    {
        public string Title { get; private set; }
        public string Artist { get; private set; }
        public string Album { get; private set; }
        public double Length { get; private set; }

        public Song(string title, string artist, string album, double length)
        {
            Title = title;
            Artist = artist;
            Album = album;
            Length = length;
        }
    }
}
