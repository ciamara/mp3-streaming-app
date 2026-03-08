using GroupDocs.Metadata.Formats.Audio;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TagLib;

namespace Kith.Sources
{
    public class Song : INotifyPropertyChanged
    {
        public int Index { get; private set; }
        public string FileName { get; set; }
        public string Title { get; set; }
        public string[] Artists { get; set; }
        public string Album { get; set; }
        public uint Year { get; set; }
        public uint Track { get; set; }
        public string[] Genres { get; set; }
        public TimeSpan Duration { get; set; }
        public IPicture[] Pictures
        {
            get => _pictures;
            set
            {
                if (_pictures != value)
                {
                    _pictures = value;
                    OnPropertyChanged();
                }
            }
        }
        private IPicture[] _pictures;

        public bool _liked { get; set; } = false;
        public bool liked
        {
            get => _liked;
            set
            {
                if (_liked != value)
                {
                    _liked = value;
                    OnPropertyChanged();
                }
            }
        }

        public Song(int index, string filename, string title, string[] artists, string album, uint year, uint track, string[] genres, TimeSpan duration, IPicture[] pictures)
        {
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
