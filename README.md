
# Kith MP3 Streaming & Tag Editor (WinUI 3 / C#)  

Kith is a desktop music player built using WinUI 3 and the Windows App SDK, utilizing the Model-View-ViewModel (MVVM) pattern. It features real-time tag (idv3) editing (Title, Artist, Album, etc.) directly on MP3 files using the TagLib# library.



## Features

- Real-time List View -> Displays local MP3 files from a directory.
- Instant Detail Update -> Edits to album art and tags update instantly in the detail view without a full page refresh due to INotifyPropertyChanged implementation on the Song data model.
- Playback Resume -> Resumes playback from the exact position when tags of the currently playing song are updated.

