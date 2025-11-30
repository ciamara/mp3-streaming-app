using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using TagLib.Riff;
using System.Linq;

namespace Kith.Sources
{
    internal class SongsView : INotifyPropertyChanged
    {
        public Song _selectedSong;

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

        public Song UpdateSelectedSong(Guid id, string title, string artists, string album, string year, string track, string genres)
        {
            string[] arrayArtists = artists.Split(',');
            string[] arrayGenres = genres.Split(',');
            uint iyear = Convert.ToUInt32(year, 16);
            uint itrack = Convert.ToUInt32(track, 16);

            var songToUpdate = AllSongs.Single(s => s.ID == id);
            Song updatedSong = new Song(songToUpdate.Index, songToUpdate.FileName, title, arrayArtists, album, iyear, itrack, arrayGenres, songToUpdate.Duration, songToUpdate.Pictures);
            
            this._selectedSong = updatedSong;
            AllSongs.Remove(songToUpdate);
            AllSongs.Add(updatedSong);
            return updatedSong;
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