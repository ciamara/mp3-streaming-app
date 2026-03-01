using ABI.Microsoft.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TagLib;
using Windows.UI.Core;

namespace Kith.Sources
{
    public class Collection : INotifyPropertyChanged
    {
        private BitmapImage _collection_cover;
        public String collection_name { get; set; }
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
        public String collection_description { get; set; }
        public double collection_duration { get; set; }
        public uint collection_size { get; set; }
        public bool editable { get; set; }
        public List<Song> collection_songs { get; set; }

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

        public Collection(String name, BitmapImage cover, String desc, double duration, uint size, List<Song> songs, bool editable)
        {
            this.collection_name = name;
            this.collection_cover = cover;
            this.collection_description = desc;
            this.collection_duration = duration;
            this.collection_size = size;

            this.collection_songs = songs;

            this.editable = editable;
        }
        public Collection(String name, BitmapImage cover, String desc, bool editable)
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
            double dur = song.Duration.TotalMinutes;
            collection_duration += dur;
            collection_duration = Math.Ceiling(collection_duration);
        }
        public void addSongs(List<Song> songs)
        {
            foreach (Song song in songs)
            {
                this.collection_songs.Add(song);
            }
        }
        public void Print()
        {
            int index = 0;
            System.Console.WriteLine("Queue:");
            foreach (Song song in collection_songs)
            {
                System.Console.Write(index++ + " ");
                System.Console.WriteLine(song.Title);
            }
        }

        public void ExtractCoverColors()
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
