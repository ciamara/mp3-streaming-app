using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using TagLib.Riff;

namespace Kith.Sources
{
    public class SongsView : INotifyPropertyChanged
    {
        private Song _selectedSong;

        public ObservableCollection<Song> AllSongs { get; set; }

        public SongsView()
        {
            AllSongs = new ObservableCollection<Song>();
        }

        public Song SelectedSong
        {
            get { return _selectedSong; }
            set
            {
                if (_selectedSong != value)
                {
                    _selectedSong = value;
                    OnPropertyChanged(nameof(SelectedSong));
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