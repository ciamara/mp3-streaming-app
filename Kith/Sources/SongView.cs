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

        public ObservableCollection<Song> filtered { get; set; }

        public ObservableCollection<Song> CurrentCollectionSongs { get; set; }

        public Queue SongQueue { get; set; }

        private Song _playingSong;
        public Song PlayingSong
        {
            get => _playingSong;
            set
            {
                if (_playingSong != value)
                {
                    _playingSong = value;
                    OnPropertyChanged(nameof(PlayingSong));
                }
            }
        }

        private bool _isQueueVisible = false;
        public bool IsQueueVisible
        {
            get => _isQueueVisible;
            set
            {
                if (_isQueueVisible != value)
                {
                    _isQueueVisible = value;
                    OnPropertyChanged(nameof(IsQueueVisible));
                    OnPropertyChanged(nameof(QueueVisibility));
                    OnPropertyChanged(nameof(TagEditorVisibility));
                }
            }
        }

        public Microsoft.UI.Xaml.Visibility QueueVisibility => _isQueueVisible ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
        public Microsoft.UI.Xaml.Visibility TagEditorVisibility => !_isQueueVisible ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

        public SongsView()
        {
            AllSongs = new ObservableCollection<Song>();
            CurrentCollectionSongs = new ObservableCollection<Song>();
            SongQueue = new Queue();
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

        public void LoadCurrentCollectionSongs(List<Song> songs)
        {
            CurrentCollectionSongs.Clear();

            foreach (var song in songs)
            {
                CurrentCollectionSongs.Add(song);
            }
        }

        public void SwapCurrentCollectionSelection(List<Song> currentSongs)
        {
            CurrentCollectionSongs.Clear();

            if (currentSongs != null )
            {
                foreach (var song in currentSongs)
                {
                    CurrentCollectionSongs.Add(song);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}