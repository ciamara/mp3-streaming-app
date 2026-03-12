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
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using TagLib;
using Windows.Foundation;
using Windows.Graphics.Imaging;
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

        private Collection CurrentCollection { get; set; }

        private Collection AllSongsCollection { get; set; }

        private Collection LikedSongsCollection { get; set; }

        private double Volume { get; set; }

        private bool Muted { get; set; }

        private bool repeatEnabled { get; set; }

        private bool shuffleEnabled { get; set; }

        bool queueVisible { get; set; } = false;
        bool tagEditorVisible { get; set; } = true;

        //private Queue queue { get; set; } = new Queue();

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

            LoadState();

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
            this.Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            SaveState();
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
        private void SongsView_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            if (args.InRecycleQueue) return;

            if (args.ItemContainer.ContentTemplateRoot is Grid rootGrid)
            {
                if (rootGrid.FindName("Index") is TextBlock textBlock)
                {
                    textBlock.Text = (args.ItemIndex + 1).ToString();
                }
            }
        }

        private void saveTags(object sender, RoutedEventArgs e)
        {

            if (ViewModel.PlayingSong == null) return;

            lastPlayedPosition = mediaPlayerElement.MediaPlayer.Position;
            selectedSongBeforeUpdate = ViewModel.SelectedSong;

            string[] arrayArtists = artistsInput.Text.Split(',');
            string[] arrayGenres = genresInput.Text.Split(',');

            uint.TryParse(yearInput.Text, out uint iyear);
            uint.TryParse(trackInput.Text, out uint itrack);

            Song songToUpdate = ViewModel.PlayingSong;

            songToUpdate.Title = titleInput.Text;
            songToUpdate.Artists = arrayArtists;
            songToUpdate.Album = albumInput.Text;
            songToUpdate.Year = iyear;
            songToUpdate.Track = itrack;
            songToUpdate.Genres = arrayGenres;

            UpdateFile(songToUpdate);

            RefreshSongs();
            ViewModel.SwapCurrentCollectionSelection(CurrentCollection.collection_songs);
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

        private void CollectionCoverGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (CollectionCoverImage != null)
            {
                double minSize = Math.Min(e.NewSize.Width, e.NewSize.Height);
                CollectionCoverImage.Width = minSize;
                CollectionCoverImage.Height = minSize;
            }
        }
        private void RefreshSongs()
        {
            Songs.Clear();

            string musicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

            if (!Directory.Exists(musicPath)) return;

            try
            {
                var song_files = Directory.EnumerateFiles(musicPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(s => s.EndsWith(".mp3"));

                foreach (var song_file in song_files)
                {
                    try
                    {
                        using (TagLib.File tfile = TagLib.File.Create(song_file))
                        {
                            string currentTitle = tfile.Tag.Title ?? Path.GetFileNameWithoutExtension(song_file);
                            string[] currentArtists = tfile.Tag.Artists ?? Array.Empty<string>();
                            string currentAlbum = tfile.Tag.Album ?? "Unknown Album";
                            uint currentYear = tfile.Tag.Year;
                            uint currentTrack = tfile.Tag.Track;
                            string[] currentGenres = tfile.Tag.Genres ?? Array.Empty<string>();
                            TimeSpan currentDuration = tfile.Properties.Duration;
                            IPicture[] currentPicture = tfile.Tag.Pictures;

                            Song currentSong = new Song(song_file, currentTitle, currentArtists, currentAlbum, currentYear, currentTrack, currentGenres, currentDuration, currentPicture);
                            Songs.Add(currentSong);
                        }
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }
            catch (Exception)
            {
            }
            ViewModel.LoadSongs(Songs);
            ViewModel.SelectedSong = selectedSongBeforeUpdate;
        }

        private void InitializeCollections()
        {
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
                    ViewModel.SongQueue.add(song);
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
            ViewModel.IsQueueVisible = !ViewModel.IsQueueVisible;

            if (ViewModel.IsQueueVisible == true)
            {
                QueueIcon.Glyph = "\uE8EC";
            }
            else if (ViewModel.IsQueueVisible == false)
            {
                QueueIcon.Glyph = "\uE71D";
            }
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

                        target.collection_cover_filename = newFileName;

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
                if (ViewModel.SongQueue.queue.Count != 0)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        LoadAndPlaySong(ViewModel.SongQueue.pop(), TimeSpan.Zero);
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
                if (ViewModel.SongQueue.queue.Count != 0)
                {
                    LoadAndPlaySong(ViewModel.SongQueue.pop(), TimeSpan.Zero);
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

        private void RemoveFromQueue(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem && menuItem.Tag is Song songToRemove)
            {
                ViewModel.SongQueue.Remove(songToRemove);
            }
        }

        private void SaveState()
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string dir = Path.Combine(localAppData, "Kith");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "save.txt");

                using (StreamWriter writetext = new StreamWriter(path))
                {
                    if(ViewModel.PlayingSong != null){
                        writetext.WriteLine($"{ViewModel.PlayingSong.FileName}");
                    }
                    else
                    {
                        writetext.WriteLine("null");
                    }
                        writetext.WriteLine($"{mediaPlayerElement.MediaPlayer.Position}");
                    writetext.WriteLine($"{Volume}");

                    foreach(Song likedSong in LikedSongsCollection.collection_songs)
                    {
                        writetext.Write($"{likedSong.FileName};");
                    }
                    writetext.Write("\n");

                    foreach (Song queueSong in ViewModel.SongQueue.queue)
                    {
                        writetext.Write($"{queueSong.FileName};");
                    }
                    writetext.Write("\n");

                    foreach (Collection col in CollectionViewModel.AllCollections)
                    {

                        writetext.Write($"{col.collection_name};");
                        writetext.Write($"{col.collection_description};");
                        writetext.Write($"{col.collection_cover_filename};");
                        foreach (Song colsong in col.collection_songs)
                        {
                            writetext.Write($"{colsong.FileName};");
                        }
                    }
                    writetext.Write("\n");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save state: {ex.Message}");
            }
            
        }

        private async void LoadState()
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string dir = Path.Combine(localAppData, "Kith");
                string path = Path.Combine(dir, "save.txt");
                //System.Console.WriteLine($"{path}");

                Song resume = null;

                using (StreamReader readtext = new StreamReader(path))
                {
                    string playing_filename = readtext.ReadLine();
                    foreach (Song s in Songs)
                    {
                        if (s.FileName == playing_filename)
                        {
                            resume = s;
                        }
                    }

                    string stringPos = readtext.ReadLine();
                    TimeSpan.TryParse(stringPos, out TimeSpan pos);
                    System.Console.WriteLine($"{pos}");

                    string stringVol = readtext.ReadLine();
                    double.TryParse(stringVol, out double vol);
                    System.Console.WriteLine($"{vol}");
                    Volume = vol;
                    mediaPlayerElement.MediaPlayer.Volume = Volume;
                    volumeSlider.Value = vol*100;

                    if (resume != null){
                        lastPlayedPosition = pos;
                        await LoadAndPlaySong(resume, lastPlayedPosition);
                        mediaPlayerElement.MediaPlayer.Pause();
                    }
                    
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load state: {ex.Message}");
            } 
        }
    }
}
