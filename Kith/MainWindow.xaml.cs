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




namespace Kith
{
    public sealed partial class MainWindow : Window
    {
        public List<Song> Songs;
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
        private async void RefreshSongs()
        {
            var song_files = Directory.EnumerateFiles("C:\\Users\\kotel\\Music", "*.*", SearchOption.TopDirectoryOnly)
            .Where(s => s.EndsWith(".mp3"));

            foreach (var song_file in song_files)
            {
                using (Metadata metadata = new Metadata(song_file))
                {
                    var root = metadata.GetRootPackage<MP3RootPackage>();
                    if (root.ID3V2 != null && root. ID3V1 != null)
                    {
                        string title = (root.ID3V2.Title);
                        Console.WriteLine(title);
                        string artist = (root.ID3V2.Artist);
                        Console.WriteLine(artist);
                        string album = (root.ID3V2.Album);
                        Console.WriteLine(album);
                        string year = (root.ID3V2.Year);
                        Console.WriteLine(year);
                        string track = (root.ID3V2.TrackNumber);
                        Console.WriteLine(track);
                        ID3V1Genre genre = (root.ID3V1.GenreValue);
                        Console.WriteLine(genre);
                        string composer = (root.ID3V2.Composers);
                        Console.WriteLine(composer);

                        //TimeSpan length = await GetAudioFileDurationAsync(song_file);
                        Console.WriteLine(song_file);
                        //Console.WriteLine(length);

                        var song = new Song(song_file, title, artist, album, year, track, genre, composer);
                        this.Songs.Add(song);
                    }
                } 
            }
        }




    }
}
