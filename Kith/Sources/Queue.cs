using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Printers;

namespace Kith.Sources
{
    internal class Queue
    {
        public List<Song> queue { get; set; }

        public Queue()
        {
            queue = new List<Song>();
        }

        public void add(Song song)
        {
            this.queue.Add(song);
        }

        public void addSongs(List<Song> songs)
        {
            foreach ( Song song in songs) {
                this.queue.Add(song);
            }
        }
        public Song pop()
        {
            Song song = this.queue.First();
            this.queue.Remove(song);
            return song;
        }

        public void print()
        {
            int index = 0;
            System.Console.WriteLine("Queue:");
            foreach (Song song in queue)
            {
                System.Console.Write(index++ + " ");
                System.Console.WriteLine(song.Title);
            }
        }
    }
}
