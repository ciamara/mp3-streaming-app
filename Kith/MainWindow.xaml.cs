using GroupDocs.Metadata;
using GroupDocs.Metadata.Formats.Audio;
using Kith.Sources;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TagLib;
using Windows.Foundation;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;


namespace Kith
{
    public sealed partial class MainWindow : Window
    {
        private object _windowSubclassingReference;
        private List<Song> Songs { get; set; } = new List<Song>();

        private SongsView ViewModel { get; set; }

        private Song selectedSongBeforeUpdate;
        private TimeSpan lastPlayedPosition;

        public MainWindow()
        {
            this.InitializeComponent();

            ViewModel = new SongsView();
            LayoutRoot.DataContext = ViewModel;
       
            //setting topsection of grid to be titlebar (in order to be draggable)
            CustomizeWindow();
            RefreshSongs();

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
            MediaPlayer player = MediaPlayerUI.MediaPlayer;
            lastPlayedPosition = player.Position;
            selectedSongBeforeUpdate = ViewModel.SelectedSong;

            string[] arrayArtists = artistsInput.Text.Split(',');
            string[] arrayGenres = genresInput.Text.Split(',');
            uint iyear = Convert.ToUInt32(yearInput.Text, 16);
            uint itrack = Convert.ToUInt32(trackInput.Text, 16);

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

        private async void UpdateFile(Song songToUpdate)
        {
            MediaPlayer player = MediaPlayerUI.MediaPlayer;
            if (player.Source != null)
            {
                player.Pause();
                player.Source = null;
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
        public async Task LoadAndPlaySong(Song song, TimeSpan playFrom)
        {
            if (song == null) return;

            Windows.Media.Playback.MediaPlayer player = MediaPlayerUI.MediaPlayer;

            try
            {
                Windows.Storage.StorageFile file = await Windows.Storage.StorageFile.GetFileFromPathAsync(song.FileName);

                Windows.Media.Core.MediaSource mediaSource = Windows.Media.Core.MediaSource.CreateFromStorageFile(file);

                player.Source = mediaSource;

                player.Position = lastPlayedPosition;
                player.Play();
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
                System.Console.WriteLine("No song selected.");
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
 
            MediaPlayer playerEngine = MediaPlayerUI.MediaPlayer;

            bool isCurrentlySelected = (ViewModel.SelectedSong?.FileName == song.FileName);

            if (isCurrentlySelected && playerEngine.Source != null)
            {
                lastPlayedPosition = playerEngine.Position;

                playerEngine.Pause();
                playerEngine.Source = null;
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
    }
}
