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
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Policy;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;

using TagLib;




namespace Kith
{
    public sealed partial class MainWindow : Window
    {
        private List<Song> Songs { get; set; } = new List<Song>();
        public MainWindow()
        {
            this.InitializeComponent();
            //setting topsection of grid to be titlebar (in order to be draggable)
            CustomizeWindow();
            RefreshSongs();

            //setting min window size
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            _ = new WindowsSubclass(hwnd, 69, new Size(930, 550));

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
            SetTitleBar(LayoutRoot);
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

                Song currentSong = new Song(song_file, currentTitle, currentArtists, currentAlbum, currentYear, currentTrack, currentGenres, currentDuration, currentPicture);
                Songs.Add(currentSong);

            }
        }




    }
}
