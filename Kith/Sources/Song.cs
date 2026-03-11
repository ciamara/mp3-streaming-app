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
        public string FileName { get; set; }

        private string _title;
        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged();
                }
            }
        }

        private string[] _artists;
        public string[] Artists
        {
            get => _artists;
            set
            {
                if (_artists != value)
                {
                    _artists = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _album;
        public string Album
        {
            get => _album;
            set
            {
                if (_album != value)
                {
                    _album = value;
                    OnPropertyChanged();
                }
            }
        }

        private uint _year;
        public uint Year
        {
            get => _year;
            set
            {
                if (_year != value)
                {
                    _year = value;
                    OnPropertyChanged();
                }
            }
        }

        private uint _track;
        public uint Track
        {
            get => _track;
            set
            {
                if (_track != value)
                {
                    _track = value;
                    OnPropertyChanged();
                }
            }
        }

        private string[] _genres;
        public string[] Genres
        {
            get => _genres;
            set
            {
                if (_genres != value)
                {
                    _genres = value;
                    OnPropertyChanged();
                }
            }
        }

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

        public Song(string filename, string title, string[] artists, string album, uint year, uint track, string[] genres, TimeSpan duration, IPicture[] pictures)
        {
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
