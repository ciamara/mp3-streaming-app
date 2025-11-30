using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Kith.Sources
{
    internal class SongsView : INotifyPropertyChanged
    {
        private Song _selectedItem;
        public ObservableCollection<Song> AllSongs { get; set; }

        public SongsView()
        {
            AllSongs = new ObservableCollection<Song>();
        }

        public Song SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    OnPropertyChanged(nameof(SelectedItem));
                }
            }
        }

        public void LoadSongs(List<Song> songs)
        {
            AllSongs.Clear();

            foreach (var song in songs)
            {
                AllSongs.Add(song);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}