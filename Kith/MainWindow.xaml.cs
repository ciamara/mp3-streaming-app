using Kith.Sources;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TagLib;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.Services.Maps;
using Windows.Storage;
using Windows.Storage.Pickers;
using static System.Net.Mime.MediaTypeNames;


namespace Kith
{
    public sealed partial class MainWindow : Window
    {
        private object _windowSubclassingReference;
        private List<Song> Songs { get; set; } = new List<Song>();

        private List<Collection> Collections { get; set; } = new List<Collection>();

        private Collection CurrentCollection { get; set; }

        private Collection AllSongsCollection { get; set; }

        private Collection LikedSongsCollection { get; set; }

        private double Volume { get; set; }

        private bool Muted { get; set; }

        private bool repeatEnabled { get; set; }

        private bool shuffleEnabled { get; set; }

        private Queue queue { get; set; } = new Queue();

        private SongsView ViewModel { get; set; }

        private CollectionsView CollectionViewModel { get; set; }

        private Song selectedSongBeforeUpdate;
        private Song previousSong;
        private TimeSpan lastPlayedPosition;

        public MainWindow()
        {
            this.InitializeComponent();

            mediaPlayerElement.MediaPlayer.MediaEnded += MediaPlayer_MediaEnded;

            CollectionViewModel = new CollectionsView();
            ViewModel = new SongsView();
            LayoutRoot.DataContext = ViewModel;
            leftSection.DataContext = CollectionViewModel;
            collectionInfo.DataContext = CollectionViewModel;
            FlyoutCollectionsList.DataContext = CollectionViewModel;

            //setting topsection of grid to be titlebar (in order to be draggable)
            CustomizeWindow();
            RefreshSongs();
            InitializeCollections();
            ViewModel.SwapCurrentCollectionSelection(Songs);
            CollectionViewModel.SelectedCollection = CurrentCollection;

            //setting min window size
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            _windowSubclassingReference = new WindowsSubclass(hwnd, 69, new Size(930, 550));

            AppWindow.SetIcon("Assets/Tiles/GalleryIcon.ico");
            AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
            OverlappedPresenter presenter = OverlappedPresenter.Create();

            presenter.IsAlwaysOnTop = false;
            presenter.IsMaximizable = true;
            presenter.IsMinimizable = true;
            presenter.IsResizable = true;
            presenter.SetBorderAndTitleBar(false, false);

            AppWindow.SetPresenter(presenter);

            SizeChanged += MainWindow_SizeChanged;
        }
        private void CustomizeWindow()
        {
            if (AppWindow?.Presenter is not OverlappedPresenter presenter)
            {
                return;
            }

            presenter.SetBorderAndTitleBar(hasBorder: false, hasTitleBar: false);
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(TitleBarContainer);
        }
        private void MaximizeRestoreBtn_Click(object sender, RoutedEventArgs e)
        {
            OverlappedPresenter presenter = (OverlappedPresenter)AppWindow.Presenter;
            if (presenter.State == OverlappedPresenterState.Maximized)
            {
                presenter.Restore();
            }
            else
            {
                presenter.Maximize();
            }
        }
        private void MainWindow_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            OverlappedPresenter presenter = (OverlappedPresenter)AppWindow.Presenter;
            MaximizeRestoreBtn.Icon = presenter.State == OverlappedPresenterState.Maximized ? new SymbolIcon { Symbol = Symbol.BackToWindow } : new SymbolIcon { Symbol = Symbol.FullScreen };
        }
        private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
        {
            OverlappedPresenter presenter = (OverlappedPresenter)AppWindow.Presenter;
            presenter.Minimize();
        }
        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void GoBackBtn_Click(object sender, RoutedEventArgs e)
        {

        }
        private void GoForwardBtn_Click(object sender, RoutedEventArgs e)
        {

        }
        private void saveTags(object sender, RoutedEventArgs e)
        {
            lastPlayedPosition = mediaPlayerElement.MediaPlayer.Position;
            selectedSongBeforeUpdate = ViewModel.SelectedSong;

            string[] arrayArtists = artistsInput.Text.Split(',');
            string[] arrayGenres = genresInput.Text.Split(',');
            uint iyear = Convert.ToUInt32(yearInput.Text, 10);
            uint itrack = Convert.ToUInt32(trackInput.Text, 10);

            Song songToUpdate = ViewModel.SelectedSong;

            songToUpdate.Title = titleInput.Text;
            songToUpdate.Artists = arrayArtists;
            songToUpdate.Album = albumInput.Text;
            songToUpdate.Year = iyear;
            songToUpdate.Track = itrack;
            songToUpdate.Genres = arrayGenres;

            UpdateFile(songToUpdate);

            RefreshSongs();
        }
        private void MainSearch_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {

        }
        private void MainSearch_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)

        {

        }
        private void MainSearch_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {

        }
        private void RefreshSongs()
        {
            Songs.Clear();

            var song_files = Directory.EnumerateFiles("C:\\Users\\kotel\\Music", "*.*", SearchOption.TopDirectoryOnly)
            .Where(s => s.EndsWith(".mp3"));
            int index = 0;
            foreach (var song_file in song_files)
            {
                TagLib.File tfile = TagLib.File.Create(song_file);

                string currentTitle = tfile.Tag.Title;
                string[] currentArtists = tfile.Tag.Artists;
                string currentAlbum = tfile.Tag.Album;
                uint currentYear = tfile.Tag.Year;
                uint currentTrack = tfile.Tag.Track;
                string[] currentGenres = tfile.Tag.Genres;
                TimeSpan currentDuration = tfile.Properties.Duration;
                IPicture[] currentPicture = tfile.Tag.Pictures;

                Song currentSong = new Song(index, song_file, currentTitle, currentArtists, currentAlbum, currentYear, currentTrack, currentGenres, currentDuration, currentPicture);
                Songs.Add(currentSong);

                index += 1;

            }
            ViewModel.LoadSongs(Songs);
            ViewModel.SelectedSong = selectedSongBeforeUpdate;
        }

        private void InitializeCollections()
        {
            Collections.Clear();

            BitmapImage housecover = new BitmapImage(new Uri("ms-appx:///Assets/house.png"));
            BitmapImage heartcover = new BitmapImage(new Uri("ms-appx:///Assets/heart.png"));
            double duration = 0.0;
            uint num = 0;
            foreach (var song in Songs)
            {
                double dur = song.Duration.TotalMinutes;
                num += 1;
                duration += dur;
            }
            duration = Math.Ceiling(duration);
            AllSongsCollection = new Collection("All Songs", housecover, "all songs", duration, num, Songs, false);

            LikedSongsCollection = new Collection("Liked Songs", heartcover, "liked songs", false);

            //Collection Test2 = new Collection();
            //Collection Test1 = new Collection();
            //Collection Test3 = new Collection();
            //Collection Test4 = new Collection();
            //Collection Test5 = new Collection();
            //Collection Test6 = new Collection();
            //Collection Test7 = new Collection();
            //Collection Test8 = new Collection();

            //Collections.Add(Test1);
            //Collections.Add(Test2);
            //Collections.Add(Test3);
            //Collections.Add(Test4);
            //Collections.Add(Test5);
            //Collections.Add(Test6);
            //Collections.Add(Test7);
            //Collections.Add(Test8);


            //CollectionViewModel.LoadCollections(Collections);

            CurrentCollection = AllSongsCollection;
        }

        private async void UpdateFile(Song songToUpdate)
        {
            if (mediaPlayerElement.MediaPlayer.Source != null)
            {
                mediaPlayerElement.MediaPlayer.Pause();
                mediaPlayerElement.MediaPlayer.Source = null;
            }

            TagLib.File tfile = TagLib.File.Create(songToUpdate.FileName);

            tfile.Tag.Title = songToUpdate.Title;
            tfile.Tag.Artists = songToUpdate.Artists;
            tfile.Tag.Album = songToUpdate.Album;
            tfile.Tag.Year = songToUpdate.Year;
            tfile.Tag.Track = songToUpdate.Track;
            tfile.Tag.Genres = songToUpdate.Genres;
            tfile.Tag.Pictures = songToUpdate.Pictures;

            tfile.Save();

            selectedSongBeforeUpdate = songToUpdate;

            if (selectedSongBeforeUpdate != null)
            {
                await LoadAndPlaySong(selectedSongBeforeUpdate, TimeSpan.Zero);
            }
        }

        private async void SongsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel.SelectedSong != null && e.AddedItems.Count > 0)
            {
                lastPlayedPosition = TimeSpan.Zero;
                await LoadAndPlaySong(ViewModel.SelectedSong, TimeSpan.Zero);
            }
        }

        private void CollectionsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CollectionViewModel.SelectedCollection != null && e.AddedItems.Count > 0)
            {
                CurrentCollection = CollectionViewModel.SelectedCollection;
                List<Song> currentSongs = CurrentCollection.collection_songs;
                ViewModel.SwapCurrentCollectionSelection(currentSongs);
            }
        }
        public async Task LoadAndPlaySong(Song song, TimeSpan playFrom)
        {
            if (song == null) return;

            try
            {
                ViewModel.PlayingSong = song;

                Windows.Storage.StorageFile file = await Windows.Storage.StorageFile.GetFileFromPathAsync(song.FileName);

                Windows.Media.Core.MediaSource mediaSource = Windows.Media.Core.MediaSource.CreateFromStorageFile(file);

                mediaPlayerElement.MediaPlayer.Source = mediaSource;

                mediaPlayerElement.MediaPlayer.Position = lastPlayedPosition;
                mediaPlayerElement.MediaPlayer.Play();
            }
            catch (Exception ex)
            {
            }
        }

        private async void AlbumCoverImage_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;

            if (ViewModel.SelectedSong == null)
            {
                //System.Console.WriteLine("No song selected.");
                return;
            }

            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;

            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".jpeg");
            openPicker.FileTypeFilter.Add(".png");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hwnd);

            StorageFile file = await openPicker.PickSingleFileAsync();

            if (file != null)
            {
                await UpdateAlbumArt(ViewModel.SelectedSong, file);
            }
        }
        private async Task UpdateAlbumArt(Song song, StorageFile imageFile)
        {

            bool isCurrentlySelected = (ViewModel.SelectedSong?.FileName == song.FileName);

            if (isCurrentlySelected && mediaPlayerElement.MediaPlayer.Source != null)
            {
                lastPlayedPosition = mediaPlayerElement.MediaPlayer.Position;

                mediaPlayerElement.MediaPlayer.Pause();
                mediaPlayerElement.MediaPlayer.Source = null;
            }

            byte[] imageBytes;
            using (var stream = await imageFile.OpenStreamForReadAsync())
            {
                imageBytes = new byte[stream.Length];
                await stream.ReadAsync(imageBytes, 0, (int)stream.Length);
            }

            TagLib.Picture newPicture = new TagLib.Picture(new TagLib.ByteVector(imageBytes))
            {
                MimeType = imageFile.ContentType
            };

            Song songInstanceToUpdate = ViewModel.AllSongs.FirstOrDefault(s => s.FileName == song.FileName);

            if (songInstanceToUpdate != null)
            {
                songInstanceToUpdate.Pictures = new TagLib.IPicture[] { newPicture };

                using (TagLib.File tfile = TagLib.File.Create(songInstanceToUpdate.FileName))
                {
                    tfile.Tag.Pictures = songInstanceToUpdate.Pictures;
                    tfile.Save();
                }

                if (isCurrentlySelected)
                {
                    await LoadAndPlaySong(songInstanceToUpdate, lastPlayedPosition);
                }
            }
        }
        private void AddToQueue(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menu)
            {
                if (menu.Tag is Song song)
                {
                    //System.Console.WriteLine($"{song.Title}");
                    this.queue.add(song);
                    this.queue.print();
                    return;
                }
            }
        }
        private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (mediaPlayerElement != null && mediaPlayerElement.MediaPlayer != null)
            {
                Volume = e.NewValue / 100.0;
                if (Volume != 0)
                {
                    Muted = false;
                    VolumeIcon.Glyph = "\uE767";
                }
                if (Volume == 0)
                {
                    Muted = true;
                    VolumeIcon.Glyph = "\uE74F";
                }
                mediaPlayerElement.MediaPlayer.Volume = Volume;
            }
        }

        private void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayerElement != null && mediaPlayerElement.MediaPlayer != null)
            {
                if (Muted)
                {
                    Muted = false;
                    mediaPlayerElement.MediaPlayer.Volume = Volume;
                    VolumeIcon.Glyph = "\uE767";
                }
                else
                {
                    Muted = true;
                    Volume = mediaPlayerElement.MediaPlayer.Volume;
                    mediaPlayerElement.MediaPlayer.Volume = 0;
                    VolumeIcon.Glyph = "\uE74F";
                }

            }
        }

        private void QueueButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private void FullScreenButton_Click(object sender, RoutedEventArgs e)
        {

        }
        private void AllSongsButton_Click(object sender, RoutedEventArgs e)
        {
            CollectionsView.SelectedIndex = -1;
            CurrentCollection = AllSongsCollection;
            CollectionViewModel.ChangeSelectedCollection(CurrentCollection);
            ViewModel.SwapCurrentCollectionSelection(CurrentCollection.collection_songs);
        }
        private void LikedSongsButton_Click(object sender, RoutedEventArgs e)
        {
            CollectionsView.SelectedIndex = -1;
            CurrentCollection = LikedSongsCollection;
            CollectionViewModel.ChangeSelectedCollection(CurrentCollection);
            ViewModel.SwapCurrentCollectionSelection(CurrentCollection.collection_songs);
        }
        private void NewCollectionButton_Click(object sender, RoutedEventArgs e)
        {
            Collection n = new Collection();

            CollectionViewModel.AllCollections.Add(n);

            CollectionViewModel.SelectedCollection = n;
            CurrentCollection = n;

            ViewModel.SwapCurrentCollectionSelection(n.collection_songs);
        }

        private async void CollectionCoverImage_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            var target = CollectionViewModel.SelectedCollection;

            if (target != null && target.editable)
            {
                FileOpenPicker openPicker = new FileOpenPicker();
                openPicker.ViewMode = PickerViewMode.Thumbnail;
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hwnd);

                openPicker.FileTypeFilter.Add(".jpg");
                openPicker.FileTypeFilter.Add(".jpeg");
                openPicker.FileTypeFilter.Add(".png");

                StorageFile file = await openPicker.PickSingleFileAsync();

                if (file != null)
                {
                    try
                    {
                        StorageFolder localFolder = ApplicationData.Current.LocalFolder;

                        string extension = file.FileType;
                        string newFileName = $"cover_{Guid.NewGuid()}{extension}";

                        StorageFile copiedFile = await file.CopyAsync(localFolder, newFileName, NameCollisionOption.ReplaceExisting);

                        DispatcherQueue.TryEnqueue(() =>
                        {
                            BitmapImage bitmapImage = new BitmapImage(new Uri($"ms-appdata:///local/{newFileName}"));
                            target.collection_cover = bitmapImage;
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to copy or set image: {ex.Message}");
                    }
                }
            }
        }

        private async void CollectionName_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            var target = CollectionViewModel.SelectedCollection;

            if (target != null && target.editable)
            {
                Microsoft.UI.Xaml.Controls.TextBox inputTextBox = new Microsoft.UI.Xaml.Controls.TextBox
                {
                    Text = target.collection_name,
                    SelectionStart = 0,
                    SelectionLength = target.collection_name.Length
                };

                ContentDialog dialog = new ContentDialog
                {
                    Title = "Rename Playlist",
                    Content = inputTextBox,
                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.Content.XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(inputTextBox.Text))
                {
                    target.collection_name = inputTextBox.Text;
                }
            }
        }

        private async void CollectionDescription_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            var target = CollectionViewModel.SelectedCollection;

            if (target != null && target.editable)
            {
                Microsoft.UI.Xaml.Controls.TextBox inputTextBox = new Microsoft.UI.Xaml.Controls.TextBox
                {
                    Text = target.collection_description,
                    SelectionStart = 0,
                    SelectionLength = target.collection_description.Length
                };

                ContentDialog dialog = new ContentDialog
                {
                    Title = "New Description",
                    Content = inputTextBox,
                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.Content.XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(inputTextBox.Text))
                {
                    target.collection_description = inputTextBox.Text;
                }
            }
        }

        private void MediaPlayer_MediaEnded(Windows.Media.Playback.MediaPlayer sender, object args)
        {
            previousSong = ViewModel.PlayingSong;
            if (repeatEnabled)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    LoadAndPlaySong(ViewModel.PlayingSong, TimeSpan.Zero);
                });
            }
            else if (shuffleEnabled)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    LoadAndPlaySong(CurrentCollection.RandomNext(ViewModel.PlayingSong), TimeSpan.Zero);
                });
            }
            else
            {
                if (queue.queue.Count != 0)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        LoadAndPlaySong(queue.pop(), TimeSpan.Zero);
                    });
                }
                else
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        LoadAndPlaySong(CurrentCollection.Next(ViewModel.PlayingSong), TimeSpan.Zero);
                    });
                }
            }
        }

        private void LikeButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.PlayingSong != null)
            {
                ViewModel.PlayingSong.liked = !ViewModel.PlayingSong.liked;

                if (ViewModel.PlayingSong.liked)
                {
                    if (!LikedSongsCollection.collection_songs.Contains(ViewModel.PlayingSong))
                    {
                        LikedSongsCollection.Add(ViewModel.PlayingSong);

                        if (CurrentCollection == LikedSongsCollection)
                        {
                            ViewModel.SwapCurrentCollectionSelection(LikedSongsCollection.collection_songs);
                        }
                    }
                }
                else
                {
                    LikedSongsCollection.Remove(ViewModel.PlayingSong);
                    if (CurrentCollection == LikedSongsCollection)
                    {
                        ViewModel.SwapCurrentCollectionSelection(LikedSongsCollection.collection_songs);
                    }
                }
            }
        }

        private void ListLikeButton_Click(object sender, RoutedEventArgs e)
        {

            if (sender is Button clickedButton)
            {
                if (clickedButton.DataContext is Song clickedSong)
                {
                    clickedSong.liked = !clickedSong.liked;

                    if (clickedSong.liked)
                    {
                        if (!LikedSongsCollection.collection_songs.Contains(clickedSong))
                        {
                            LikedSongsCollection.Add(clickedSong);
                        }
                    }
                    else
                    {
                        LikedSongsCollection.Remove(clickedSong);
                    }

                    if (CurrentCollection == LikedSongsCollection)
                    {
                        ViewModel.SwapCurrentCollectionSelection(LikedSongsCollection.collection_songs);
                    }
                }
            }
        }

        private void DeleteCollection(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menu)
            {
                if (menu.Tag is Collection collection)
                {
                    CollectionViewModel.AllCollections.Remove(collection);

                    if(CurrentCollection == collection)
                    {
                        CollectionsView.SelectedIndex = -1;
                        CurrentCollection = AllSongsCollection;
                        CollectionViewModel.ChangeSelectedCollection(CurrentCollection);
                        ViewModel.SwapCurrentCollectionSelection(CurrentCollection.collection_songs);
                    }
                    return;
                }
            }
        }
        private void FlyoutCollectionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is Collection targetCollection)
            {
                if (ViewModel.PlayingSong != null)
                {
                    if (!targetCollection.collection_songs.Contains(ViewModel.PlayingSong))
                    {
                        targetCollection.Add(ViewModel.PlayingSong);

                        if (CurrentCollection == targetCollection)
                        {
                            ViewModel.SwapCurrentCollectionSelection(targetCollection.collection_songs);
                        }
                    }
                }

                FlyoutCollectionsList.SelectedIndex = -1;

                AddToCollectionFlyout.Hide();
            }
        }

        private void ListFlyoutCollectionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is Collection targetCollection)
            {
                if (sender is ListView listView && listView.DataContext is Song targetSong)
                {
                    if (!targetCollection.collection_songs.Contains(targetSong))
                    {
                        targetCollection.Add(targetSong);

                        if (CurrentCollection == targetCollection)
                        {
                            ViewModel.SwapCurrentCollectionSelection(targetCollection.collection_songs);
                        }
                    }

                    listView.SelectedIndex = -1;

                    var popups = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetOpenPopupsForXamlRoot(listView.XamlRoot);
                    foreach (var popup in popups)
                    {
                        popup.IsOpen = false;
                    }
                }
            }
        }

        private void PreviousTrackClicked(object sender, EventArgs e)
        {
            if (mediaPlayerElement.MediaPlayer != null)
            {
                if (mediaPlayerElement.MediaPlayer.Position.TotalSeconds > 2)
                {
                    mediaPlayerElement.MediaPlayer.Position = TimeSpan.Zero;
                }
                else
                {
                    if (previousSong != null)
                    {
                        LoadAndPlaySong(previousSong, TimeSpan.Zero);
                    }
                    else
                    {
                        mediaPlayerElement.MediaPlayer.Position = TimeSpan.Zero;
                    }
                }
            }
        }

        private void NextTrackClicked(object sender, EventArgs e)
        {
            if (mediaPlayerElement.MediaPlayer != null)
            {
                if (queue.queue.Count != 0)
                {
                    LoadAndPlaySong(queue.pop(), TimeSpan.Zero);
                }
                else
                {
                    mediaPlayerElement.MediaPlayer.Position = ViewModel.PlayingSong.Duration;
                }

            }
        }

        private void RepeatClicked(object sender, EventArgs e)
        {
            if (mediaPlayerElement.MediaPlayer != null && sender is CustomMediaTransportControls transportControls)
            {
                repeatEnabled = !repeatEnabled;

                if (repeatEnabled)
                {
                    shuffleEnabled = false;

                    transportControls.SetShuffleState(false);
                }

            }
        }

        private void ShuffleClicked(object sender, EventArgs e)
        {
            if (mediaPlayerElement.MediaPlayer != null && sender is CustomMediaTransportControls transportControls)
            {
                shuffleEnabled = !shuffleEnabled;

                if (shuffleEnabled)
                {
                    repeatEnabled = false;

                    transportControls.SetRepeatState(false);
                }

            }
        }

        private void DeleteFromCollection(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menu)
            {
                if (menu.Tag is Song song)
                {
                    if (CurrentCollection != AllSongsCollection)
                    {
                        if (CurrentCollection == LikedSongsCollection)
                        {
                            song.liked = false;
                            LikedSongsCollection.Remove(song);
                            if (CurrentCollection == LikedSongsCollection)
                            {
                                ViewModel.SwapCurrentCollectionSelection(LikedSongsCollection.collection_songs);
                            }
                        }
                        else
                        {
                            CurrentCollection.Remove(song);
                            ViewModel.SwapCurrentCollectionSelection(CurrentCollection.collection_songs);
                        }     
                    }
                    return;
                }
            }
        }

        private void SongFlyoutContext_Opening(object sender, object e)
        {
            if (sender is MenuFlyout flyout)
            {
                foreach (var item in flyout.Items)
                {
                    if (item is MenuFlyoutItem menuItem && menuItem.Text == "delete from collection")
                    {
                        if (CurrentCollection == AllSongsCollection)
                        {
                            menuItem.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            menuItem.Visibility = Visibility.Visible;
                        }
                    }
                }
            }
        }

        private void ListFlyoutCollectionsList_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ListView listView)
            {
                listView.ItemsSource = CollectionViewModel.AllCollections;
            }
        }

    }
}
