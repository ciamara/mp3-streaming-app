using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Kith.Sources
{
    public class CollectionsView : INotifyPropertyChanged
    {
        private Collection _selectedCollection;
        public ObservableCollection<Collection> AllCollections { get; set; }
        public ObservableCollection<Collection> playlists { get; set; }
        public ObservableCollection<Collection> albums { get; set; }

        public CollectionsView()
        {
            AllCollections = new ObservableCollection<Collection>();
            playlists = new ObservableCollection<Collection>();
            albums = new ObservableCollection<Collection>();
        }

        public Collection SelectedCollection
        {
            get => _selectedCollection;
            set
            {
                if (_selectedCollection != value)
                {
                    _selectedCollection = value;
                    OnPropertyChanged();
                }
            }
        }

        public void LoadCollections(List<Collection> collections)
        {
            AllCollections.Clear();
            foreach (var collection in collections)
            {
                AllCollections.Add(collection);
            }
        }

        public void ChangeSelectedCollection(Collection c)
        {
            SelectedCollection = c;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}