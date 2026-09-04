using AudioHelpers;
using Kith.Sources;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using TagLib;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media.Devices;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.AxHost;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Window = Microsoft.UI.Xaml.Window;


namespace Kith
{
    /// <summary>
    /// Main Window of mp3 player.
    /// </summary>
    public sealed partial class MainWindow : Window
    {

        [System.Runtime.InteropServices.DllImport("User32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);

        private object _windowSubclassingReference;
        private List<Song> Songs { get; set; } = new List<Song>();

        private Collection CurrentCollection { get; set; } = new();

        private Collection AllSongsCollection { get; set; } = new();

        private Collection LikedSongsCollection { get; set; } = new();

        private Stack<Song> pastSongs { get; set; } = new Stack<Song>();

        private double Volume { get; set; }

        private bool Muted { get; set; }

        private bool repeatEnabled { get; set; }

        private bool dynamicColorMode { get; set; }

        private bool shuffleEnabled { get; set; }

        private SongsView ViewModel { get; set; }

        private CollectionsView CollectionViewModel { get; set; }

        private Song selectedSongBeforeUpdate = new Song();

        private TimeSpan lastPlayedPosition;

        private const int BarCount = 32;
        private float[] lastFrameBars = new float[BarCount];

        public ObservableCollection<AudioBar> AudioBars { get; set; }

        private AudioHelper _audioHelper;
        private DispatcherTimer _visualizerTimer;

        public MainWindow()
        {
            this.InitializeComponent();

            // initializing audio bars
            AudioBars = new ObservableCollection<AudioBar>();
            for (int i = 0; i < BarCount; i++)
            {
                AudioBars.Add(new AudioBar { Height = 10 }); // min height
            }

            _audioHelper = new AudioHelper();

            _visualizerTimer = new DispatcherTimer();
            _visualizerTimer.Interval = TimeSpan.FromMilliseconds(30);
            _visualizerTimer.Tick += VisualizerTimer_Tick;
            _visualizerTimer.Start();

            mediaPlayerElement.MediaPlayer.MediaEnded += MediaPlayer_MediaEnded;

            CollectionViewModel = new CollectionsView();
            ViewModel = new SongsView();
            LayoutRoot.DataContext = ViewModel;
            AudioVisualizerMode.DataContext = ViewModel;
            leftSection.DataContext = CollectionViewModel;
            collectionInfo.DataContext = CollectionViewModel;
            FlyoutCollectionsList.DataContext = CollectionViewModel;

            //setting topsection of grid to be titlebar (in order to be draggable)
            CustomizeWindow();
            RefreshSongs();
            InitializeCollections();
            ViewModel.SwapCurrentCollectionSelection(Songs);
            if (CurrentCollection != null)
            {
                CollectionViewModel.SelectedCollection = CurrentCollection;
            }
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

            uint dpi = GetDpiForWindow(hwnd);
            double scalingFactor = dpi / 96.0;

            int physicalWidth = (int)(1230 * scalingFactor);
            int physicalHeight = (int)(660 * scalingFactor);

            AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(physicalWidth, physicalHeight));

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
            //OverlappedPresenter presenter = (OverlappedPresenter)AppWindow.Presenter;
            //MaximizeRestoreBtn.Icon = presenter.State == OverlappedPresenterState.Maximized ? new SymbolIcon { Symbol = Symbol.BackToWindow } : new SymbolIcon { Symbol = Symbol.FullScreen };

            if (AppWindow.Presenter is OverlappedPresenter overlappedPresenter)
            {
                if (overlappedPresenter.State == OverlappedPresenterState.Maximized)
                {
                    MaximizeRestoreBtn.Icon = overlappedPresenter.State == OverlappedPresenterState.Maximized ? new SymbolIcon { Symbol = Symbol.BackToWindow } : new SymbolIcon { Symbol = Symbol.FullScreen };
                }
                else
                {
                    MaximizeRestoreBtn.Icon = overlappedPresenter.State == OverlappedPresenterState.Maximized ? new SymbolIcon { Symbol = Symbol.BackToWindow } : new SymbolIcon { Symbol = Symbol.FullScreen };
                }
            }
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

            if(ViewModel.SelectedSong != null)
            {
                selectedSongBeforeUpdate = ViewModel.SelectedSong;
            }
         
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

            //RefreshSongs();
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
                            //string[] currentArtists = tfile.Tag.Performers ?? Array.Empty<string>();

                            string[] currentArtists = (tfile.Tag.Performers != null && tfile.Tag.Performers.Length > 0)
                                    ? tfile.Tag.Performers
                                    : new string[] { "Unknown Artist" };

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

        private void UpdateFile(Song songToUpdate)
        {
            if (mediaPlayerElement.MediaPlayer.Source != null)
            {
                mediaPlayerElement.MediaPlayer.Pause();
                mediaPlayerElement.MediaPlayer.Source = null;
                _audioHelper?.Dispose();
            }

            TagLib.File tfile = TagLib.File.Create(songToUpdate.FileName);

            tfile.Tag.Title = songToUpdate.Title;
            tfile.Tag.Performers = songToUpdate.Artists;
            tfile.Tag.Album = songToUpdate.Album;
            tfile.Tag.Year = songToUpdate.Year;
            tfile.Tag.Track = songToUpdate.Track;
            tfile.Tag.Genres = songToUpdate.Genres;
            tfile.Tag.Pictures = songToUpdate.Pictures;

            tfile.Save();

            selectedSongBeforeUpdate = songToUpdate;

            if (selectedSongBeforeUpdate != null)
            {
                _ = LoadAndPlaySong(selectedSongBeforeUpdate, lastPlayedPosition);
            }
        }

        private void SongsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel.SelectedSong != null && e.AddedItems.Count > 0)
            {
                lastPlayedPosition = TimeSpan.Zero;
                _ = LoadAndPlaySong(ViewModel.SelectedSong, TimeSpan.Zero);
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
        public async Task LoadAndPlaySong(Song song, TimeSpan playFrom, bool autoPlay = true)
        {
            if (song == null) return;

            _ = UpdateVisualizerThemeAsync(song);

            try
            {
                ViewModel.PlayingSong = song;

                if (_audioHelper == null)
                {
                    _audioHelper = new AudioHelper();
                }
                _audioHelper.Load(song.FileName);

                Windows.Storage.StorageFile file = await Windows.Storage.StorageFile.GetFileFromPathAsync(song.FileName);

                Windows.Media.Core.MediaSource mediaSource = Windows.Media.Core.MediaSource.CreateFromStorageFile(file);

                void OnMediaOpened(Windows.Media.Playback.MediaPlayer sender, object args)
                {
                    sender.MediaOpened -= OnMediaOpened;

                    if (playFrom != TimeSpan.Zero)
                    {
                        sender.PlaybackSession.Position = playFrom;
                    }
                    if (autoPlay)
                    {
                        sender.Play();
                    }
                    else
                    {
                        sender.Pause();
                    }
                }

                mediaPlayerElement.MediaPlayer.MediaOpened += OnMediaOpened;
                mediaPlayerElement.MediaPlayer.Source = mediaSource;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Playback Error: {ex.Message}");
            }
        }

        private async void AlbumCoverImage_Tapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;

            if (ViewModel.PlayingSong == null)
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
                await UpdateAlbumArt(ViewModel.PlayingSong, file);
            }
        }
        private async Task UpdateAlbumArt(Song song, StorageFile imageFile)
        {

            bool isCurrentlyPlaying = (ViewModel.PlayingSong?.FileName == song.FileName);

            if (isCurrentlyPlaying && mediaPlayerElement.MediaPlayer.Source != null)
            {
                lastPlayedPosition = mediaPlayerElement.MediaPlayer.Position;

                mediaPlayerElement.MediaPlayer.Pause();
                mediaPlayerElement.MediaPlayer.Source = null;
                _audioHelper?.Dispose();
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

            var allInstancesOfSong = Songs.Where(s => s.FileName == song.FileName).ToList();
            if (CollectionViewModel?.AllCollections != null)
            {
                foreach (var collection in CollectionViewModel.AllCollections)
                {
                    allInstancesOfSong.AddRange(collection.collection_songs.Where(s => s.FileName == song.FileName));
                }
            }
            allInstancesOfSong.Add(song);

            bool fileSaved = false;
            foreach (var songInstanceToUpdate in allInstancesOfSong.Distinct())
            {
                songInstanceToUpdate.Pictures = new TagLib.IPicture[] { newPicture };

                if (!fileSaved)
                {
                    using (TagLib.File tfile = TagLib.File.Create(songInstanceToUpdate.FileName))
                    {
                        tfile.Tag.Pictures = songInstanceToUpdate.Pictures;
                        tfile.Save();
                    }
                    fileSaved = true;
                }
            }

            if (isCurrentlyPlaying)
            {
                _ = LoadAndPlaySong(song, lastPlayedPosition);
            }

            ViewModel.SwapCurrentCollectionSelection(CurrentCollection.collection_songs);
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
                mediaPlayerElement.MediaPlayer.Volume = Volume;

                string targetGlyph;

                if (Volume > 0)
                {
                    Muted = false;
                    targetGlyph = "\uE767";
                }
                else
                {
                    Muted = true;
                    targetGlyph = "\uE74F";
                }

                if (VolumeIcon != null)
                {
                    VolumeIcon.Glyph = targetGlyph;
                }

                if (visualizerVolumeIcon != null)
                {
                    visualizerVolumeIcon.Glyph = targetGlyph;
                }
            }
        }

        private void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (mediaPlayerElement != null && mediaPlayerElement.MediaPlayer != null)
            {
                string targetGlyph;

                if (Muted)
                {
                    // unmute
                    Muted = false;
                    mediaPlayerElement.MediaPlayer.Volume = Volume;
                    targetGlyph = "\uE767"; // The 'Volume' icon
                }
                else
                {
                    // mute
                    Muted = true;
                    Volume = mediaPlayerElement.MediaPlayer.Volume;
                    mediaPlayerElement.MediaPlayer.Volume = 0;
                    targetGlyph = "\uE74F";
                }

                if (VolumeIcon != null)
                {
                    VolumeIcon.Glyph = targetGlyph;
                }

                if (visualizerVolumeIcon != null)
                {
                    visualizerVolumeIcon.Glyph = targetGlyph;
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
            LayoutRoot.Visibility = Visibility.Collapsed;
            AudioVisualizerMode.Visibility = Visibility.Visible;

            ExtendsContentIntoTitleBar = false;

            // link media player
            if (mediaPlayerElement.MediaPlayer != null)
            {
                visualizerMediaPlayerElement.SetMediaPlayer(mediaPlayerElement.MediaPlayer);
            }

            if ((OverlappedPresenter)AppWindow.Presenter != null)
            {
                ((OverlappedPresenter)AppWindow.Presenter).IsResizable = false;
                ((OverlappedPresenter)AppWindow.Presenter).IsMaximizable = false;
                ((OverlappedPresenter)AppWindow.Presenter).IsMinimizable = false;
            }

            if (AppWindow != null)
            {
                AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
            }
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
            CollectionViewModel.playlists.Add(n);

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
            //Console.WriteLine($"media ended: {ViewModel.PlayingSong.Title}");
            if (ViewModel.PlayingSong != null)
            {
                pastSongs.Push(ViewModel.PlayingSong);
            }
            
            if (repeatEnabled)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    if(ViewModel.PlayingSong != null)
                    {
                        _ = LoadAndPlaySong(ViewModel.PlayingSong, TimeSpan.Zero);
                        if (dynamicColorMode)
                        {
                            UpdateAppTheme(ViewModel.PlayingSong);
                        }
                    } 
                });
            }
            else if (shuffleEnabled)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (ViewModel.PlayingSong != null)
                    {
                        var rand = CurrentCollection.RandomNext(ViewModel.PlayingSong);
                        _ = LoadAndPlaySong(rand, TimeSpan.Zero);
                        if (dynamicColorMode)
                        {
                            UpdateAppTheme(rand);
                        }
                    }
                });
            }
            else
            {
                if (ViewModel.SongQueue.queue.Count != 0)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        var popped = ViewModel.SongQueue.pop();
                        _ =  LoadAndPlaySong(popped, TimeSpan.Zero);
                        if (dynamicColorMode)
                        {
                            UpdateAppTheme(popped);
                        }
                    });
                }
                else
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        var nextSong = CurrentCollection?.Next(ViewModel.PlayingSong!);
                        if (nextSong != null)
                        {
                            _ = LoadAndPlaySong(nextSong, TimeSpan.Zero);
                            if (dynamicColorMode)
                            {
                                UpdateAppTheme(nextSong);
                            }
                        }
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

            if (sender is Microsoft.UI.Xaml.Controls.Button clickedButton)
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
                    if (collection.GetType() == typeof(Album))
                    {
                        CollectionViewModel.albums.Remove(collection);
                    }
                    else
                    {
                        CollectionViewModel.playlists.Remove(collection);
                    }
                    if (CollectionFilter.Text != "")
                    {
                        CollectionViewModel.filtered.Remove(collection);
                    }

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
                if (sender is Microsoft.UI.Xaml.Controls.ListView listView && listView.DataContext is Song targetSong)
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
                    if (pastSongs.Count() != 0)
                    {
                        _ = LoadAndPlaySong(pastSongs.Pop(), TimeSpan.Zero);
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
                    if (ViewModel.PlayingSong != null)
                    {
                        //Console.WriteLine("next from queue");
                        pastSongs.Push(ViewModel.PlayingSong);
                        _ = LoadAndPlaySong(ViewModel.SongQueue.pop(), TimeSpan.Zero);
                    }
                }
                else
                {
                    if (ViewModel.PlayingSong != null)
                    {
                        mediaPlayerElement.MediaPlayer.Position = ViewModel.PlayingSong.Duration;
                    }    
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
            if (sender is Microsoft.UI.Xaml.Controls.ListView listView)
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
                string dir = Path.Combine(ApplicationData.Current.LocalFolder.Path, "Kith");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "save.txt");

                using (StreamWriter writetext = new StreamWriter(path))
                {
                    // info
                    writetext.WriteLine(ViewModel.PlayingSong?.FileName ?? "null");
                    writetext.WriteLine(mediaPlayerElement.MediaPlayer.Position.ToString());
                    writetext.WriteLine(Volume.ToString());

                    // liked
                    foreach (Song likedSong in LikedSongsCollection.collection_songs)
                    {
                        writetext.Write($"{likedSong.FileName};");
                    }
                    writetext.WriteLine();

                    // queue
                    foreach (Song queueSong in ViewModel.SongQueue.queue)
                    {
                        writetext.Write($"{queueSong.FileName};");
                    }
                    writetext.WriteLine();

                    // playlists
                    foreach (Collection col in CollectionViewModel.AllCollections)
                    {
                        if (col == AllSongsCollection || col == LikedSongsCollection) continue;

                        string type = col is Album ? "ALBUM" : "COLLECTION";
                        string cover = string.IsNullOrEmpty(col.collection_cover_filename) ? "none" : col.collection_cover_filename;

                        writetext.Write($"{type};{col.collection_name};{col.collection_description};{cover};");

                        foreach (Song colsong in col.collection_songs)
                        {
                            writetext.Write($"{colsong.FileName};");
                        }
                        writetext.WriteLine();
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine($"Save error: {ex.Message}"); }
        }

        private void LoadState()
        {
            try
            {
                string path = Path.Combine(ApplicationData.Current.LocalFolder.Path, "Kith", "save.txt");
                if (!System.IO.File.Exists(path)) return;

                using (StreamReader readtext = new StreamReader(path))
                {
                    string? playing_filename = readtext.ReadLine();
                    TimeSpan.TryParse(readtext.ReadLine(), out TimeSpan pos);
                    double.TryParse(readtext.ReadLine(), out double vol);
                    Volume = vol;
                    mediaPlayerElement.MediaPlayer.Volume = Volume;
                    volumeSlider.Value = vol * 100;
        
                    // liked songs
                    string? likedData = readtext.ReadLine();
                    if (!string.IsNullOrWhiteSpace(likedData))
                    {
                        foreach (string file in likedData.Split(';', StringSplitOptions.RemoveEmptyEntries))
                        {
                            var s = Songs.FirstOrDefault(x => x.FileName == file);
                            if (s != null) { s.liked = true; LikedSongsCollection.Add(s); }
                        }
                    }

                    // queue
                    string? queueData = readtext.ReadLine();
                    if (!string.IsNullOrWhiteSpace(queueData))
                    {
                        foreach (string file in queueData.Split(';', StringSplitOptions.RemoveEmptyEntries))
                        {
                            var s = Songs.FirstOrDefault(x => x.FileName == file);
                            if (s != null) ViewModel.SongQueue.queue.Add(s);
                        }
                    }

                    // playlists
                    string? playlistLine;
                    while ((playlistLine = readtext.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(playlistLine)) continue;

                        string[] parts = playlistLine.Split(';');
                        if (parts.Length < 4) continue;

                        string type = parts[0];
                        string name = parts[1];
                        string desc = parts[2];
                        string cover = parts[3];

                        string? fullCoverPath = null;
                        if (cover != "none" && !string.IsNullOrEmpty(cover))
                        {
                            fullCoverPath = cover.Contains("://") ? cover : "ms-appdata:///local/" + cover;
                        }

                        List<Song> loadedSongs = new List<Song>();
                        for (int i = 4; i < parts.Length; i++)
                        {
                            if (string.IsNullOrWhiteSpace(parts[i])) continue;
                            var s = Songs.FirstOrDefault(x => x.FileName == parts[i]);
                            if (s != null) loadedSongs.Add(s);
                        }

                        if (type == "ALBUM" && loadedSongs.Count > 0)
                        {
                            Album newAlbum = new Album(loadedSongs);
                            CollectionViewModel.AllCollections.Add(newAlbum);
                            CollectionViewModel.albums.Add(newAlbum);
                        }
                        else
                        {
                            Collection newCol;
                            if (fullCoverPath != null)
                            {
                                newCol = new Collection(name, desc, fullCoverPath, true);
                            }
                            else
                            {
                                newCol = new Collection(name, desc, true);
                            }
                            foreach (var s in loadedSongs) newCol.Add(s);
                            CollectionViewModel.AllCollections.Add(newCol);
                            CollectionViewModel.playlists.Add(newCol);
                        }
                    }

                    var resume = Songs.FirstOrDefault(s => s.FileName == playing_filename);
                    if (resume != null) _ = LoadAndPlaySong(resume, pos, false);
                }
            }
            catch (Exception ex) { Debug.WriteLine($"Load error: {ex.Message}"); }
        }

        // triggers fft calculation every tick (30ms)
        private void VisualizerTimer_Tick(object? sender, object e)
        {
            //Console.WriteLine("timer tick");

            if (AudioVisualizerMode.Visibility != Visibility.Visible || mediaPlayerElement.MediaPlayer == null) return;

            if (mediaPlayerElement.MediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
            {
                TimeSpan currentPos = mediaPlayerElement.MediaPlayer.PlaybackSession.Position;
                float[] magnitudes = _audioHelper.GetFft(currentPos);

                if (magnitudes != null)
                {
                    UpdateVisualizer(magnitudes);
                }
            }
            else
            {
                UpdateVisualizer(new float[512]);
            }
        }

        // calculates heights and updates ui, groups magnitudes into barCount bins
        private void UpdateVisualizer(float[] magnitudes)
        {
            float[] displayBars = new float[BarCount];
            int maxBin = magnitudes.Length / 2;

            for (int i = 0; i < BarCount; i++)
            {
                double lowerBound = Math.Pow(maxBin, (double)i / BarCount);
                double upperBound = Math.Pow(maxBin, (double)(i + 1) / BarCount);

                int startBin = (int)Math.Floor(lowerBound);
                int endBin = (int)Math.Ceiling(upperBound);

                if (endBin <= startBin) endBin = startBin + 1;
                if (endBin > magnitudes.Length) endBin = magnitudes.Length;

                float maxMagnitude = 0;
                for (int j = startBin; j < endBin; j++)
                {
                    if (magnitudes[j] > maxMagnitude)
                        maxMagnitude = magnitudes[j];
                }
                
                // scaling
                float intensity = (float)(Math.Sqrt(maxMagnitude) * 150);

                intensity = Math.Max(5, intensity); // min height
                intensity = Math.Min(250, intensity); // max height

                float smoothingAmount = intensity > lastFrameBars[i] ? 0.7f : 0.15f;

                displayBars[i] = LinInterpolation(lastFrameBars[i], intensity, smoothingAmount);
                lastFrameBars[i] = displayBars[i];

                AudioBars[i].Height = displayBars[i];
            }
        }

        // smooth gliding of the bars instead of snapping to new pos
        private float LinInterpolation(float start, float end, float amount)
        {
            return start + (end - start) * amount;
        }

        private void ExitVisualizer_Tapped(object sender, RoutedEventArgs e)
        {
            AudioVisualizerMode.Visibility = Visibility.Collapsed;
            LayoutRoot.Visibility = Visibility.Visible;

            AppWindow.SetPresenter(AppWindowPresenterKind.Default);

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(TitleBarContainer);
        }

        private async Task UpdateVisualizerThemeAsync(Song song)
        {
            if (song?.Pictures == null || song.Pictures.Length == 0) return;

            try
            {
                using (SoftwareBitmap bitmap = await VisualHelper.GetBitmapFromIPicture(song.Pictures[0]))
                {
                    if (bitmap == null) return;

                    string hexColor = await VisualHelper.ExtractFeatureColor(bitmap);
                    var rawColor = VisualHelper.GetColorFromHex(hexColor);
                    bool isLight = VisualHelper.IsColorLight(rawColor);
                    SolidColorBrush brush;
                    SolidColorBrush emptyTrackBrush;

                    if (isLight)
                    {
                        brush = new SolidColorBrush(rawColor);
                        emptyTrackBrush = new SolidColorBrush(Color.FromArgb(70, rawColor.R, rawColor.G, rawColor.B));
                    }
                    else
                    {
                        var lightenedColor = VisualHelper.LightenColor(rawColor, 0.2);
                        brush = new SolidColorBrush(lightenedColor);
                        lightenedColor = VisualHelper.LightenColor(rawColor, 0.4);
                        emptyTrackBrush = new SolidColorBrush(Color.FromArgb(70, lightenedColor.R, lightenedColor.G, lightenedColor.B));
                    }
                    

                    DispatcherQueue.TryEnqueue(() =>
                    {
                        // volume slider
                        visualizerVolumeSlider.Foreground = brush;
                        visualizerVolumeSlider.BorderBrush = brush;

                        var vrs = visualizerVolumeSlider.Resources;
                        vrs["SliderThumbBackground"] = brush;
                        vrs["SliderThumbBackgroundPointerOver"] = brush;
                        vrs["SliderThumbBackgroundPressed"] = brush;
                        vrs["SliderTrackValueFill"] = brush;
                        vrs["SliderTrackValueFillPointerOver"] = brush;
                        vrs["SliderTrackValueFillPressed"] = brush;
                        vrs["SliderTickBarFill"] = brush;

                        vrs["SliderTrackFill"] = emptyTrackBrush;
                        vrs["SliderTrackFillPointerOver"] = emptyTrackBrush;
                        vrs["SliderTrackFillPressed"] = emptyTrackBrush;

                        vrs["ProgressBarBackgroundThemeBrush"] = emptyTrackBrush;
                        vrs["ProgressBarBackground"] = emptyTrackBrush;

                        visualizerVolumeSlider.FocusVisualPrimaryBrush = brush;
                        visualizerVolumeSlider.FocusVisualSecondaryBrush = new SolidColorBrush(Microsoft.UI.Colors.Black);

                        // transport controls slider
                        var tc = visualizerTransportControls;
                        tc.Resources["SliderTrackValueFill"] = brush;
                        tc.Resources["SliderTrackValueFillPointerOver"] = brush;
                        tc.Resources["SliderThumbBackground"] = brush;
                        tc.Resources["SliderThumbBackgroundPointerOver"] = brush;
                        tc.Resources["SliderThumbBackgroundPressed"] = brush;
                        tc.Resources["SliderTrackValueFillPressed"] = brush;
                        tc.Resources["ProgressBarForegroundThemeBrush"] = brush;
                        tc.Resources["ProgressBarIndeterminateForegroundThemeBrush"] = brush;
                        tc.Resources["ProgressBarForeground"] = brush;

                        tc.Resources["SliderTrackFill"] = emptyTrackBrush;
                        tc.Resources["SliderTrackFillPointerOver"] = emptyTrackBrush;
                        tc.Resources["SliderTrackFillPressed"] = emptyTrackBrush;

                        tc.Resources["ProgressBarBackgroundThemeBrush"] = emptyTrackBrush;
                        tc.Resources["ProgressBarBackground"] = emptyTrackBrush;


                        // background gradient
                        LinearGradientBrush dynamicGradient = new LinearGradientBrush();
                        dynamicGradient.StartPoint = new Windows.Foundation.Point(0, 0);
                        dynamicGradient.EndPoint = new Windows.Foundation.Point(0, 1);

                        // audio bars
                        foreach (var bar in AudioBars)
                        {
                            bar.BarBrush = brush;
                        }
  
                        Color topColor = Color.FromArgb(180, rawColor.R, rawColor.G, rawColor.B);
                        dynamicGradient.GradientStops.Add(new GradientStop() { Color = topColor, Offset = 0.1 });

                        dynamicGradient.GradientStops.Add(new GradientStop() { Color = VisualHelper.GetColorFromHex("#1B1B1B"), Offset = 0.8 });

                        VisualizerGradient.Fill = dynamicGradient;

                        // refresh
                        RefreshElementTheme(visualizerVolumeSlider);
                        RefreshElementTheme(tc);
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Theme Update Error: {ex.Message}");
            }
        }

        private async Task UpdateAppTheme(Song song)
        {
            if (song?.Pictures == null || song.Pictures.Length == 0) return;

            try
            {
                using (SoftwareBitmap bitmap = await VisualHelper.GetBitmapFromIPicture(song.Pictures[0]))
                {
                    if (bitmap == null) return;

                    string hexColor = await VisualHelper.ExtractFeatureColor(bitmap);
                    var rawColor = VisualHelper.GetColorFromHex(hexColor);
                    bool isLight = VisualHelper.IsColorLight(rawColor);
                    SolidColorBrush baseBrush;
                    SolidColorBrush hoverBrush;
                    SolidColorBrush focusBrush;
                    SolidColorBrush lightTextBrush;
                    SolidColorBrush darkTextBrush;
                    SolidColorBrush textBrush;

                    lightTextBrush = new SolidColorBrush(VisualHelper.GetColorFromHex("#D7CFCD"));
                    darkTextBrush = new SolidColorBrush(VisualHelper.GetColorFromHex("#251914"));

                    if (isLight)
                    {
                        rawColor = VisualHelper.DarkenColor(rawColor, 0.3);
                        baseBrush = new SolidColorBrush(rawColor);

                        var lighterColor = VisualHelper.LightenColor(rawColor, 0.15);
                        hoverBrush = new SolidColorBrush(lighterColor);

                        var lightestColor = VisualHelper.LightenColor(lighterColor, 0.15);
                        focusBrush = new SolidColorBrush(lightestColor);

                        textBrush = darkTextBrush;
                    }
                    else
                    {
                        baseBrush = new SolidColorBrush(rawColor);

                        var lighterColor = VisualHelper.LightenColor(rawColor, 0.1);
                        hoverBrush = new SolidColorBrush(lighterColor);

                        var lightestColor = VisualHelper.LightenColor(lighterColor, 0.1);
                        focusBrush = new SolidColorBrush(lightestColor);

                        textBrush = lightTextBrush;
                    }

                    DispatcherQueue.TryEnqueue(() =>
                    {
                        // downloader -------------------------------------------------------

                        var dwnl = downloader.Resources;

                        downloader.Background = baseBrush;
                        downloader.BorderBrush = baseBrush;
                        downloader.Foreground = textBrush;

                        dwnl["TextControlForeground"] = textBrush;
                        dwnl["TextControlForegroundPointerOver"] = textBrush;
                        dwnl["TextControlForegroundFocused"] = textBrush;

                        dwnl["TextControlButtonForeground"] = textBrush;
                        dwnl["TextControlButtonForegroundPointerOver"] = textBrush;
                        dwnl["TextControlButtonForegroundPressed"] = textBrush;

                        dwnl["TextControlBackgroundPointerOver"] = hoverBrush;
                        dwnl["TextControlBorderBrushPointerOver"] = hoverBrush;

                        dwnl["TextControlBackgroundFocused"] = focusBrush;
                        dwnl["TextControlBorderBrushFocused"] = focusBrush;

                        // gradients ----------------------------------------------------

                        LinearGradientBrush sideGradient = new LinearGradientBrush();
                        sideGradient.StartPoint = new Windows.Foundation.Point(0, 0);
                        sideGradient.EndPoint = new Windows.Foundation.Point(0, 1);

                        sideGradient.GradientStops.Add(new GradientStop
                        {
                            Color = VisualHelper.GetColorFromHex("#111111"),
                            Offset = 0.1
                        });

                        sideGradient.GradientStops.Add(new GradientStop
                        {
                            Color = Windows.UI.Color.FromArgb(255, (byte)(rawColor.R * 0.25), (byte)(rawColor.G * 0.25), (byte)(rawColor.B * 0.25)),
                            Offset = 0.40
                        });

                        sideGradient.GradientStops.Add(new GradientStop
                        {
                            Color = Windows.UI.Color.FromArgb(255, (byte)(rawColor.R * 0.42), (byte)(rawColor.G * 0.42), (byte)(rawColor.B * 0.42)),
                            Offset = 0.60
                        });

                        sideGradient.GradientStops.Add(new GradientStop
                        {
                            Color = rawColor,
                            Offset = 1.0
                        });

                        LinearGradientBrush bottomGradient = new LinearGradientBrush();
                        bottomGradient.StartPoint = new Windows.Foundation.Point(0, 0);
                        bottomGradient.EndPoint = new Windows.Foundation.Point(0, 1);

                        bottomGradient.GradientStops.Add(new GradientStop
                        {
                            Color = rawColor,
                            Offset = 0.1
                        });

                        bottomGradient.GradientStops.Add(new GradientStop
                        {
                            Color = Windows.UI.Color.FromArgb(255, (byte)(rawColor.R * 0.42), (byte)(rawColor.G * 0.42), (byte)(rawColor.B * 0.42)),
                            Offset = 0.50
                        });

                        bottomGradient.GradientStops.Add(new GradientStop
                        {
                            Color = Windows.UI.Color.FromArgb(255, (byte)(rawColor.R * 0.25), (byte)(rawColor.G * 0.25), (byte)(rawColor.B * 0.25)),
                            Offset = 0.70
                        });

                        bottomGradient.GradientStops.Add(new GradientStop
                        {
                            Color = VisualHelper.GetColorFromHex("#111111"),
                            Offset = 1.0
                        });

                        LeftSection.Fill = sideGradient;
                        RightSection.Fill = sideGradient;
                        MiddleSection.Fill = sideGradient;
                        BottomSection.Fill = bottomGradient;

                        // collection filter ----------------------------------------------------------

                        var fil = CollectionFilter.Resources;

                        CollectionFilter.Background = baseBrush;
                        CollectionFilter.BorderBrush = baseBrush;
                        CollectionFilter.Foreground = textBrush;

                        fil["TextControlForeground"] = textBrush;
                        fil["TextControlForegroundPointerOver"] = textBrush;
                        fil["TextControlForegroundFocused"] = textBrush;

                        fil["TextControlButtonForeground"] = textBrush;
                        fil["TextControlButtonForegroundPointerOver"] = textBrush;
                        fil["TextControlButtonForegroundPressed"] = textBrush;

                        fil["TextControlBackgroundPointerOver"] = hoverBrush;
                        fil["TextControlBorderBrushPointerOver"] = hoverBrush;

                        fil["TextControlBackgroundFocused"] = focusBrush;
                        fil["TextControlBorderBrushFocused"] = focusBrush;

                        // filter buttons ---------------------------------------------------------

                        var pfil = playlistFilterButton.Resources;
                        var afil = albumFilterButton.Resources;

                        playlistFilterButton.Foreground = lightTextBrush;
                        albumFilterButton.Foreground = lightTextBrush;

                        pfil["ToggleButtonBackgroundChecked"] = baseBrush;
                        afil["ToggleButtonBackgroundChecked"] = baseBrush;

                        pfil["ToggleButtonBackgroundCheckedPointerOver"] = baseBrush;
                        afil["ToggleButtonBackgroundCheckedPointerOver"] = baseBrush;

                        pfil["ToggleButtonBackgroundCheckedPressed"] = baseBrush;
                        afil["ToggleButtonBackgroundCheckedPressed"] = baseBrush;

                        pfil["ToggleButtonBorderBrushChecked"] = baseBrush;
                        afil["ToggleButtonBorderBrushChecked"] = baseBrush;

                        pfil["ToggleButtonForegroundChecked"] = textBrush;
                        afil["ToggleButtonForegroundChecked"] = textBrush;

                        pfil["ToggleButtonForegroundUnchecked"] = lightTextBrush;
                        afil["ToggleButtonForegroundUnchecked"] = lightTextBrush;

                        pfil["ToggleButtonBackgroundPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, (byte)(rawColor.R * 0.25), (byte)(rawColor.G * 0.25), (byte)(rawColor.B * 0.25)));
                        afil["ToggleButtonBackgroundPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, (byte)(rawColor.R * 0.25), (byte)(rawColor.G * 0.25), (byte)(rawColor.B * 0.25)));

                        pfil["ToggleButtonBorderBrushPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, (byte)(rawColor.R * 0.25), (byte)(rawColor.G * 0.25), (byte)(rawColor.B * 0.25)));
                        afil["ToggleButtonBorderBrushPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, (byte)(rawColor.R * 0.25), (byte)(rawColor.G * 0.25), (byte)(rawColor.B * 0.25)));

                        if (isLight)
                        {
                            pfil["ToggleButtonForegroundCheckedPointerOver"] = VisualHelper.GetColorFromHex("#111111");
                            afil["ToggleButtonForegroundCheckedPointerOver"] = VisualHelper.GetColorFromHex("#111111");
                            pfil["ToggleButtonForegroundCheckedPressed"] = VisualHelper.GetColorFromHex("#111111");
                            afil["ToggleButtonForegroundCheckedPressed"] = VisualHelper.GetColorFromHex("#111111");
                        }
                        else
                        {
                            pfil["ToggleButtonForegroundCheckedPointerOver"] = VisualHelper.GetColorFromHex("#FFFFFF");
                            afil["ToggleButtonForegroundCheckedPointerOver"] = VisualHelper.GetColorFromHex("#FFFFFF");
                            pfil["ToggleButtonForegroundCheckedPressed"] = VisualHelper.GetColorFromHex("#FFFFFF");
                            afil["ToggleButtonForegroundCheckedPressed"] = VisualHelper.GetColorFromHex("#FFFFFF");
                        }

                        // collections view -------------------------------------------------------------------------------
                        var cv = CollectionsView.Resources;

                        cv["ListViewItemSelectionIndicatorBrush"] = rawColor;
                        cv["ListViewItemSelectionIndicatorPointerOverBrush"] = rawColor;
                        cv["ListViewItemSelectionIndicatorPressedBrush"] = rawColor;

                        // collection view

                        coverBackground.Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, (byte)(rawColor.R * 0.6), (byte)(rawColor.G * 0.6), (byte)(rawColor.B * 0.6)));

                        // song filter

                        var sfil = SongFilter.Resources;

                        SongFilter.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, (byte)(rawColor.R * 0.6), (byte)(rawColor.G * 0.6), (byte)(rawColor.B * 0.6)));
                        SongFilter.Foreground = textBrush;
                        SongFilter.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, (byte)(rawColor.R * 0.6), (byte)(rawColor.G * 0.6), (byte)(rawColor.B * 0.6)));

                        sfil["TextControlBackgroundPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, (byte)(rawColor.R * 0.7), (byte)(rawColor.G * 0.7), (byte)(rawColor.B * 0.7)));
                        sfil["TextControlBorderBrushPointerOver"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, (byte)(rawColor.R * 0.7), (byte)(rawColor.G * 0.7), (byte)(rawColor.B * 0.7)));

                        sfil["TextControlBackgroundGotFocus"] = new SolidColorBrush(rawColor);

                        sfil["TextControlBackgroundFocused"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, (byte)(rawColor.R * 0.8), (byte)(rawColor.G * 0.8), (byte)(rawColor.B * 0.8)));
                        sfil["TextControlBorderBrushFocused"] = new SolidColorBrush(Windows.UI.Color.FromArgb(255, (byte)(rawColor.R * 0.8), (byte)(rawColor.G * 0.8), (byte)(rawColor.B * 0.8)));

                        sfil["TextControlForegroundPointerOver"] = textBrush;
                        sfil["TextControlForegroundFocused"] = textBrush;

                        sfil["TextControlButtonForeground"] = textBrush;
                        sfil["TextControlButtonForegroundPointerOver"] = textBrush;
                        sfil["TextControlButtonForegroundPressed"] = textBrush;

                        // media transport controls

                        //< SolidColorBrush x: Key = "SystemControlForegroundBaseHighBrush" Color = "White" />
                        //< SolidColorBrush x: Key = "SystemControlForegroundBaseMediumBrush" Color = "#D7CFCD" />

                        //< !--Transport Control Buttons(Play, Pause, Next, Prev) States-- >
                        //< SolidColorBrush x: Key = "AppBarButtonForegroundPointerOver" Color = "White" />
                        //< SolidColorBrush x: Key = "AppBarButtonForegroundPressed" Color = "Gray" />
                        //< SolidColorBrush x: Key = "AppBarButtonBackgroundPointerOver" Color = "#40251b" />
                        //< SolidColorBrush x: Key = "AppBarButtonBackgroundPressed" Color = "#4d2a1d" />

                        //< !--Seek Bar(Slider) Empty Track Colors-- >
                        //< SolidColorBrush x: Key = "SliderTrackFill" Color = "#341e16" />
                        //< SolidColorBrush x: Key = "SliderTrackFillPointerOver" Color = "#40251b" />
                        //< SolidColorBrush x: Key = "SliderTrackFillPressed" Color = "#4d2a1d" />


                        //< !--Seek Bar(Slider) Played Value Colors-- >
                        //< SolidColorBrush x: Key = "SliderTrackValueFill" Color = "#D7CFCD" />
                        //< SolidColorBrush x: Key = "SliderTrackValueFillPointerOver" Color = "White" />
                        //< SolidColorBrush x: Key = "SliderTrackValueFillPressed" Color = "White" />

                        //< !--Seek Bar(Slider) Thumb(The Dragger) Colors-- >
                        //< SolidColorBrush x: Key = "SliderThumbBackground" Color = "#D7CFCD" />
                        //< SolidColorBrush x: Key = "SliderThumbBackgroundPointerOver" Color = "White" />
                        //< SolidColorBrush x: Key = "SliderThumbBackgroundPressed" Color = "Gray" />

                        //var mtc = mediaPlayerElement.TransportControls.Resources;

                        //mtc[""]


                        // refresh
                        RefreshElementTheme(downloader);
                        RefreshElementTheme(LeftSection);
                        RefreshElementTheme(RightSection);
                        RefreshElementTheme(MiddleSection);
                        RefreshElementTheme(BottomSection);
                        RefreshElementTheme(CollectionFilter);
                        RefreshElementTheme(playlistFilterButton);
                        RefreshElementTheme(albumFilterButton);
                        RefreshElementTheme(CollectionsView);
                        RefreshElementTheme(coverBackground);
                        RefreshElementTheme(SongFilter);
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Theme Update Error: {ex.Message}");
            }
        }

        private void RefreshElementTheme(FrameworkElement element)
        {
            var current = element.RequestedTheme;
            element.RequestedTheme = current == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
            element.RequestedTheme = ElementTheme.Default;
        }

        private async void downloadButton_Click(object sender, RoutedEventArgs e)
        {
            string url = downloader.Text;
            downloader.Text = "";

            downloadProgressRing.IsActive = true;
            downloadProgressRing.IsIndeterminate = true;

            var progress = new Progress<double>(p =>
            {
                if (downloadProgressRing.IsIndeterminate)
                {
                    downloadProgressRing.IsIndeterminate = false;
                }

                downloadProgressRing.Value = p * 100;
            });

            await Downloader.Download(url, progress);

            downloadProgressRing.Value = 0;
            downloadProgressRing.IsActive = false;

            RefreshSongs();

            CollectionViewModel.ChangeSelectedCollection(CurrentCollection);
            ViewModel.SwapCurrentCollectionSelection(CurrentCollection.collection_songs);
        }

        private void groupByAlbumButton_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<string, string> albumMap = new Dictionary<string, string>();
            string? albumSongs;

            foreach (Song s in Songs)
            {
                if (albumMap.ContainsKey(s.Album))
                {
                    albumMap.TryGetValue(s.Album, out albumSongs);
                    albumSongs = albumSongs + ";" + s.FileName;
                    albumMap[$"{s.Album}"] = albumSongs;
                }
                else
                {
                    albumMap.Add(s.Album, s.FileName);
                }
            }

            foreach (KeyValuePair<string, string> entry in albumMap)
            {
                //Console.WriteLine($"Album: {entry.Key}, songs: {entry.Value}");

                if (entry.Value.Contains(";"))
                {
                    string[] songFileNames = entry.Value.Split(";");
                    
                    List<Song> songsForAlbum = Songs.FindAll(song => songFileNames.Contains(song.FileName));

                    if (songsForAlbum.Count > 0)
                    {
                        string targetAlbumName = songsForAlbum[0].Album;

                        var existingCollection = CollectionViewModel.AllCollections.FirstOrDefault(c => c.collection_name == targetAlbumName);

                        if (existingCollection != null)
                        {
                            existingCollection.collection_songs = songsForAlbum;

                            // Console.WriteLine($"Updated existing album: {targetAlbumName}");
                        }
                        else
                        {
                            Album album = new Album(songsForAlbum);
                            CollectionViewModel.AllCollections.Add(album);
                            CollectionViewModel.albums.Add(album);

                            // Console.WriteLine($"Created new album: {targetAlbumName}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Warning: No matching Song objects found for Album '{entry.Key}'.");
                    }
                }
            }


        }

        private void albumFilterButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void playlistFilterButton_Click(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            bool showAlbums = albumFilterButton.IsChecked == true;
            bool showPlaylists = playlistFilterButton.IsChecked == true;

            Collection sel_col = CurrentCollection;

            if (showAlbums && showPlaylists)
            {
                // show both
                CollectionsView.ItemsSource = CollectionViewModel.AllCollections;

                CollectionViewModel.SelectedCollection = sel_col;
                CurrentCollection = sel_col;

                ViewModel.SwapCurrentCollectionSelection(sel_col.collection_songs);
            }
            else if (showAlbums && !showPlaylists)
            {
                // show albums
                CollectionsView.ItemsSource = CollectionViewModel.albums;

                if (sel_col.GetType() == typeof(Album))
                {
                    CollectionViewModel.SelectedCollection = sel_col;
                    CurrentCollection = sel_col;

                    ViewModel.SwapCurrentCollectionSelection(sel_col.collection_songs);
                }
                else
                {
                    CollectionViewModel.SelectedCollection = AllSongsCollection;
                    CurrentCollection = AllSongsCollection;

                    ViewModel.SwapCurrentCollectionSelection(AllSongsCollection.collection_songs);
                }
            }
            else if (!showAlbums && showPlaylists)
            {
                // show playlists
                CollectionsView.ItemsSource = CollectionViewModel.playlists;

                if (sel_col.GetType() != typeof(Album))
                {
                    CollectionViewModel.SelectedCollection = sel_col;
                    CurrentCollection = sel_col;

                    ViewModel.SwapCurrentCollectionSelection(sel_col.collection_songs);
                }
                else
                {
                    CollectionViewModel.SelectedCollection = AllSongsCollection;
                    CurrentCollection = AllSongsCollection;

                    ViewModel.SwapCurrentCollectionSelection(AllSongsCollection.collection_songs);
                }
            }
            else
            {
                // show nothing.
                CollectionsView.ItemsSource = null;

                CollectionViewModel.SelectedCollection = AllSongsCollection;
                CurrentCollection = AllSongsCollection;

                ViewModel.SwapCurrentCollectionSelection(AllSongsCollection.collection_songs);
            }
        }

        private void filterCollections(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            string filter = CollectionFilter.Text;

            var filtered = CollectionViewModel.AllCollections.Where(c => c.collection_name.Contains(filter, StringComparison.OrdinalIgnoreCase) || c.collection_description.Contains(filter, StringComparison.OrdinalIgnoreCase));

            var currentSelection = CollectionViewModel.SelectedCollection;

            CollectionViewModel.filtered = new ObservableCollection<Collection>(filtered);

            CollectionsView.ItemsSource = CollectionViewModel.filtered;

            if (currentSelection != null && CollectionViewModel.filtered.Contains(currentSelection))
            {
                CollectionsView.SelectedItem = currentSelection;
                CollectionViewModel.SelectedCollection = currentSelection;
            }
            else if (CollectionViewModel.filtered.Count > 0)
            {
                CollectionsView.SelectedItem = CollectionViewModel.filtered[0];
            }
        }

        private void filterSongs(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            string filter = SongFilter.Text;
            //Console.WriteLine($"filter: {filter}");

            var filtered = ViewModel.CurrentCollectionSongs.Where(s => s.Title.Contains(filter, StringComparison.OrdinalIgnoreCase) || s.Artists[0].Contains(filter, StringComparison.OrdinalIgnoreCase) || s.Album.Contains(filter, StringComparison.OrdinalIgnoreCase));

            ViewModel.filtered = new ObservableCollection<Song>(filtered);

            SongsView.ItemsSource = ViewModel.filtered;
        }

        private void CollectionShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SongQueue.Clear();

            List<Song> col_songs = ViewModel.CurrentCollectionSongs.ToList<Song>();

            while(col_songs.Count() != 0)
            {
                Random rnd = new Random();
                int rand = rnd.Next(0, col_songs.Count);
                ViewModel.SongQueue.add(col_songs[rand]);
                col_songs.RemoveAt(rand);
            }
            _ = LoadAndPlaySong(ViewModel.SongQueue.pop(), TimeSpan.Zero);
        }

        private void CollectionPlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SongQueue.Clear();

            List<Song> col_songs = ViewModel.CurrentCollectionSongs.ToList<Song>();

            for(int i=0;  i<col_songs.Count(); i++)
            {
                ViewModel.SongQueue.add(col_songs[i]);
            }
            _ = LoadAndPlaySong(ViewModel.SongQueue.pop(), TimeSpan.Zero);
        }

        private void QueueClearButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SongQueue.Clear();
        }

        private async void CdBurnButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentCollection.collection_duration < 80)
            {
                string imgBurnPath = AudioHelpers.BurnerImgBurn.GetImgBurnPath();
                if (string.IsNullOrEmpty(imgBurnPath))
                {
                    ContentDialog imgBurnErrorDialog = new ContentDialog
                    {
                        Title = "ImgBurn Not Found",
                        Content = "ImgBurn is required to burn CDs but was not found on your system. Please install ImgBurn and try again.",
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot,
                        Background = new SolidColorBrush(VisualHelper.GetColorFromHex("#6c2d19")),
                        BorderBrush = new SolidColorBrush(VisualHelper.GetColorFromHex("#6c2d19"))
                    };
                    _ = imgBurnErrorDialog.ShowAsync();
                    return;
                }

                string driveLetter;
                try
                {
                    driveLetter = GetCdDriveLetter();
                }
                catch (Exception ex)
                {
                    ContentDialog driveErrorDialog = new ContentDialog
                    {
                        Title = "Optical Drive Not Found",
                        Content = ex.Message,
                        CloseButtonText = "OK",
                        XamlRoot = this.Content.XamlRoot,
                        Background = new SolidColorBrush(VisualHelper.GetColorFromHex("#6c2d19")),
                        BorderBrush = new SolidColorBrush(VisualHelper.GetColorFromHex("#6c2d19"))
                    };
                    _ = driveErrorDialog.ShowAsync();
                    return;
                }

                CdBurnButton.IsEnabled = false;

                bool isAlbum = CurrentCollection is Album;
                string discAuthor = "Various Artists";

                if (isAlbum)
                {
                    if (CurrentCollection.collection_songs.Count > 0)
                    {
                        discAuthor = CurrentCollection.collection_songs[0].Artists[0];
                    }
                }
                else
                {
                    Microsoft.UI.Xaml.Controls.TextBox authorInput = new Microsoft.UI.Xaml.Controls.TextBox
                    {
                        Text = Environment.UserName,
                        SelectionStart = 0,
                        SelectionLength = Environment.UserName.Length
                    };

                    ContentDialog authorDialog = new ContentDialog
                    {
                        Title = "Playlist Author",
                        Content = authorInput,
                        PrimaryButtonText = "Burn CD",
                        SecondaryButtonText = "Cancel",
                        DefaultButton = ContentDialogButton.Primary,
                        XamlRoot = this.Content.XamlRoot,
                        Background = new SolidColorBrush(VisualHelper.GetColorFromHex("#6c2d19")),
                        BorderBrush = new SolidColorBrush(VisualHelper.GetColorFromHex("#6c2d19"))
                    };

                    var authorResult = await authorDialog.ShowAsync();

                    if (authorResult != ContentDialogResult.Primary)
                    {
                        CdBurnButton.IsEnabled = true;
                        return; // cancelled burn
                    }

                    if (!string.IsNullOrWhiteSpace(authorInput.Text))
                    {
                        discAuthor = authorInput.Text.Trim();
                    }
                }

                string musicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
                string dir = Path.Combine(musicPath, "burn");

                if (Directory.Exists(dir)) Directory.Delete(dir, true);
                Directory.CreateDirectory(dir);

                var statusTextBlock = new TextBlock
                {
                    Text = "Preparing to burn...",
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var progressBar = new Microsoft.UI.Xaml.Controls.ProgressBar
                {
                    IsIndeterminate = true,
                    HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch
                };

                var dialogPanel = new StackPanel();
                dialogPanel.Children.Add(statusTextBlock);
                dialogPanel.Children.Add(progressBar);

                ContentDialog burnDialog = new ContentDialog
                {
                    Title = "Burning Audio CD",
                    Content = dialogPanel,
                    XamlRoot = this.Content.XamlRoot,
                    Background = new SolidColorBrush(VisualHelper.GetColorFromHex("#6c2d19")),
                    BorderBrush = new SolidColorBrush(VisualHelper.GetColorFromHex("#6c2d19"))
                };

                _ = burnDialog.ShowAsync();

                try
                {
                    uint index = 0;
                    foreach (Song s in ViewModel.CurrentCollectionSongs)
                    {
                        string sanitized_name = AudioHelper.Sanitize(s.FileName, index);
                        System.IO.File.Copy(s.FileName, Path.Combine(dir, sanitized_name), true);
                        index++;
                    }

                    EventHandler<string> statusHandler = (s, msg) => 
                    {
                        DispatcherQueue.TryEnqueue(() => statusTextBlock.Text = msg);
                    };

                    EventHandler<Exception> errorHandler = (s, ex) => 
                    {
                        DispatcherQueue.TryEnqueue(() => 
                        {
                            statusTextBlock.Text = $"error: {ex.Message}";
                            progressBar.IsIndeterminate = false;
                            burnDialog.CloseButtonText = "Close";
                        });
                    };

                    EventHandler burnCompletedHandler = (s, args) => 
                    {
                        DispatcherQueue.TryEnqueue(() => 
                        {
                            statusTextBlock.Text = "burn completed successfully";
                            progressBar.IsIndeterminate = false;
                            progressBar.Value = 100;
                        });
                        
                        Task.Delay(1500).ContinueWith(_ => 
                        {
                            DispatcherQueue.TryEnqueue(() => burnDialog.Hide());
                        });
                    };

                    BurnerImgBurn.StatusUpdated += statusHandler;
                    BurnerImgBurn.BurnError += errorHandler;
                    BurnerImgBurn.BurnCompleted += burnCompletedHandler;

                    try
                    {
                        await Task.Run(() => AudioHelpers.BurnerImgBurn.BurnCDWithText(dir, CurrentCollection.collection_name, discAuthor, isAlbum, driveLetter, imgBurnPath));
                    }
                    finally
                    {
                        BurnerImgBurn.StatusUpdated -= statusHandler;
                        BurnerImgBurn.BurnError -= errorHandler;
                        BurnerImgBurn.BurnCompleted -= burnCompletedHandler;
                    }
                }
                catch (Exception ex)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        statusTextBlock.Text = $"error: {ex.Message}";
                        progressBar.IsIndeterminate = false;
                        burnDialog.CloseButtonText = "Close";
                    });
                }
                finally
                {
                    if (Directory.Exists(dir))
                    {
                        try { Directory.Delete(dir, true); } catch { }
                    }

                    CdBurnButton.IsEnabled = true;
                }
            }
            else
            {
                ContentDialog durationErrorDialog = new ContentDialog
                {
                    Title = "Playlist Too Long",
                    Content = "The selected playlist exceeds the 80-minute limit for a standard Audio CD. Please remove some tracks and try again.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot,
                    Background = new SolidColorBrush(VisualHelper.GetColorFromHex("#6c2d19")),
                    BorderBrush = new SolidColorBrush(VisualHelper.GetColorFromHex("#6c2d19"))
                };

                _ = durationErrorDialog.ShowAsync();
                Console.WriteLine("[BURN ERROR]: playlist time longer than 80 minutes.");
            }
        }

        /// <summary>
        /// Retrieves the first available CD-ROM drive letter on the system.
        /// </summary>
        private string GetCdDriveLetter()
        {
            var cdDrive = DriveInfo.GetDrives().FirstOrDefault(d => d.DriveType == DriveType.CDRom);

            if (cdDrive != null)
            {
                return cdDrive.Name.TrimEnd('\\');
            }

            throw new Exception("No optical drive detected on this system.");
        }

        private void folderExportButton_Click(object sender, RoutedEventArgs e)
        {
            //creating dir with playlist name
            string musicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            string dir = Path.Combine(musicPath, CurrentCollection.collection_name);

            if (CurrentCollection.GetType() == typeof(Album))
            {
                dir = Path.Combine(musicPath, CurrentCollection.collection_name + " - " + CurrentCollection.collection_songs[0].Artists[0]);
            }

            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);

            //copying songs into new dir
            foreach (Song s in ViewModel.CurrentCollectionSongs)
            {
                System.IO.File.Copy(s.FileName, Path.Combine(dir, Path.GetFileName(s.FileName)), true);
            }
        }

        private void colorModeButton_Click(object sender, RoutedEventArgs e)
        {
            dynamicColorMode = !dynamicColorMode;

            UpdateAppTheme(ViewModel.PlayingSong);
        }
    }
}
