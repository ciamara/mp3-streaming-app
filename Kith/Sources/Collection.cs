using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Kith.Sources
{
    public class Collection : INotifyPropertyChanged
    {
        private BitmapImage _collection_cover;
        public string collection_cover_filename { get; set; } = "";
        private string _collection_name;
        private string _collection_description;
        private double _collection_duration;
        private uint _collection_size;

        public string collection_name
        {
            get => _collection_name;
            set { _collection_name = value; OnPropertyChanged(); }
        }

        public BitmapImage collection_cover
        {
            get => _collection_cover;
            set
            {
                if (_collection_cover != value)
                {
                    _collection_cover = value;
                    OnPropertyChanged();
                }
            }
        }

        public string collection_description
        {
            get => _collection_description;
            set { _collection_description = value; OnPropertyChanged(); }
        }

        public double collection_duration
        {
            get => _collection_duration;
            set { _collection_duration = value; OnPropertyChanged(); }
        }

        public uint collection_size
        {
            get => _collection_size;
            set { _collection_size = value; OnPropertyChanged(); }
        }

        public bool editable { get; set; }
        public List<Song> _collection_songs { get; set; }

        public List<Song> collection_songs
        {
            get => _collection_songs;
            set { _collection_songs = value; OnPropertyChanged(); }
        }

        public Collection()
        {
            this.collection_name = "new playlist";
            this.collection_cover = new BitmapImage(new Uri("ms-appx:///Assets/albumplaceholder.png"));
            this.collection_description = "your playlist description";
            this.collection_duration = 0.0;
            this.collection_size = 0;
            this.editable = true;
            this.collection_songs = new List<Song>();
        }

        public Collection(string name, BitmapImage cover, string desc, double duration, uint size, List<Song> songs, bool editable)
        {
            this.collection_name = name;
            this.collection_cover = cover;
            this.collection_description = desc;
            this.collection_duration = duration;
            this.collection_size = size;
            this.collection_songs = songs;
            this.editable = editable;
        }

        public Collection(string name, BitmapImage cover, string desc, bool editable)
        {
            this.collection_name = name;
            this.collection_cover = cover;
            this.collection_description = desc;
            this.collection_duration = 0.0;
            this.collection_size = 0;
            this.collection_songs = new List<Song>();
            this.editable = editable;
        }

        public void Add(Song song)
        {
            collection_songs.Add(song);
            RecalculateStats();
        }

        public void Remove(Song song)
        {
            if (collection_songs.Remove(song))
            {
                RecalculateStats();
            }
        }

        public Song Next(Song song)
        {
            if (collection_songs == null || collection_songs.Count == 0)
            {
                return null;
            }

            int currentIndex = collection_songs.IndexOf(song);

            if (currentIndex == -1)
            {
                return null;
            }

            int nextIndex = (currentIndex + 1) % collection_songs.Count;

            return collection_songs[nextIndex];
        }

        public Song RandomNext(Song currentSong)
        {
            if (collection_songs == null || collection_songs.Count <= 1)
            {
                return currentSong;
            }

            int excludeIndex = collection_songs.IndexOf(currentSong);
            Random rnd = new Random();

            if (excludeIndex == -1)
            {
                return collection_songs[rnd.Next(collection_songs.Count)];
            }

            int randomIndex = rnd.Next(0, collection_songs.Count - 1);

            if (randomIndex >= excludeIndex)
            {
                randomIndex++;
            }

            return collection_songs[randomIndex];
        }

        public void addSongs(List<Song> songs)
        {
            foreach (Song song in songs)
            {
                this.collection_songs.Add(song);
            }
            OnPropertyChanged(nameof(collection_songs));
        }

        public void Print()
        {
            int index = 0;
            System.Diagnostics.Debug.WriteLine("Queue:");
            foreach (Song song in collection_songs)
            {
                System.Diagnostics.Debug.WriteLine($"{index++} {song.Title}");
            }
        }

        private void RecalculateStats()
        {
            collection_duration = Math.Ceiling(collection_songs.Sum(s => s.Duration.TotalMinutes));
            collection_size = (uint)collection_songs.Count;

            OnPropertyChanged(nameof(collection_duration));
            OnPropertyChanged(nameof(collection_size));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}