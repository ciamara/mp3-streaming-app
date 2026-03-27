using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Printers;

namespace Kith.Sources
{
    public class Queue
    {
        public ObservableCollection<Song> queue { get; set; }

        public Queue()
        {
            queue = new ObservableCollection<Song>();
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

        public void Remove(Song s)
        {
            this.queue.Remove(s);
        }

        public void Clear()
        {
            queue.Clear();
        }
    }
}
