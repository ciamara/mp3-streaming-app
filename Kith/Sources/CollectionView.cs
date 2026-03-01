using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using TagLib.Riff;

namespace Kith.Sources
{
    public class CollectionsView : INotifyPropertyChanged
    {
        private Collection _selectedCollection;

        public ObservableCollection<Collection> AllCollections { get; set; }

        public CollectionsView()
        {
            AllCollections = new ObservableCollection<Collection>();
        }

        public Collection SelectedCollection
        {
            get { return _selectedCollection; }
            set
            {
                if (_selectedCollection != value)
                {
                    _selectedCollection = value;
                    OnPropertyChanged(nameof(SelectedCollection));
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void ChangeSelectedCollection(Collection c)
        {
            SelectedCollection = c;
        }
    }
}