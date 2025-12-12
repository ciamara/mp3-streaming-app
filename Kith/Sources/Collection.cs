using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagLib;
using Windows.UI.Core;

namespace Kith.Sources
{
    class Collection
    {
        private String collection_name { get; set; }
        private Bitmap collection_cover { get; set; }
        private String collection_description { get; set; }
        private Double collection_duration { get; set; }
        private uint collection_size { get; set; }
        private List<Song> collection_songs { get; set; }

        public Collection()
        {
            this.collection_name = "your playlist";
            this.collection_cover = new Bitmap("ms - appx:///Assets/albumplaceholder.png", true);
            this.collection_description = "your playlist description";
            this.collection_duration = 0.0;
            this.collection_size = 0;

            this.collection_songs = new List<Song>();
        }
        public void Add(Song song)
        {
            collection_songs.Add(song);
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

    }
}
