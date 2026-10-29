using MCERemote.Core;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace MCERemote.Controller.Views
{
    /// <summary>
    /// Interaction logic for MusicLibrary.xaml
    /// </summary>
    public partial class MusicLibrary : Controls.PageView
    {
        public MusicLibrary()
        {
            InitializeComponent();
        }

        #region Fields

        // Lists of each view, kept so switching back to a view doesn't load it again
        private readonly Dictionary<LibraryItemKind, LibraryList> lists = new Dictionary<LibraryItemKind, LibraryList>();

        private LibraryItemKind currentKind = LibraryItemKind.Album;

        private bool initialized = false;

        // Whether focus was last in the content rather than the sidebar, to restore it
        private bool focusInContent = false;

        #endregion

        #region Private Methods

        private void ShowView(LibraryItemKind kind)
        {
            currentKind = kind;

            AlbumsButton.IsChecked = kind == LibraryItemKind.Album;
            ArtistsButton.IsChecked = kind == LibraryItemKind.Artist;
            PlaylistsButton.IsChecked = kind == LibraryItemKind.Playlist;
            SongsButton.IsChecked = kind == LibraryItemKind.Song;
            GenresButton.IsChecked = kind == LibraryItemKind.Genre;

            ViewTitle.Text = GetViewName(kind);

            if (!lists.TryGetValue(kind, out LibraryList list))
            {
                list = new LibraryList(Connection, new LibraryQuery(kind));
                lists[kind] = list;
            }

            LibraryContent.Show(list);
        }

        private static string GetViewName(LibraryItemKind kind)
        {
            switch (kind)
            {
                case LibraryItemKind.Album: return "Albums";
                case LibraryItemKind.Artist: return "Artists";
                case LibraryItemKind.Playlist: return "Playlists";
                case LibraryItemKind.Genre: return "Genres";
                default: return "Songs";
            }
        }

        private Controls.ButtonRadio CurrentViewButton
        {
            get
            {
                switch (currentKind)
                {
                    case LibraryItemKind.Album: return AlbumsButton;
                    case LibraryItemKind.Artist: return ArtistsButton;
                    case LibraryItemKind.Playlist: return PlaylistsButton;
                    case LibraryItemKind.Genre: return GenresButton;
                    default: return SongsButton;
                }
            }
        }

        private void RestoreFocus()
        {
            if (focusInContent)
                LibraryContent.FocusLast();
            else
                CurrentViewButton.Focus();
        }

        #endregion

        #region Event Handlers

        private void PageView_Loaded(object sender, RoutedEventArgs e)
        {
            // Loaded fires again when returning to the page, which keeps its state
            if (!initialized && Connection != null)
            {
                initialized = true;
                ShowView(LibraryItemKind.Album);
            }

            RestoreFocus();
        }

        private void ViewButton_Click(object sender, RoutedEventArgs e)
        {
            var kind = (LibraryItemKind)Enum.Parse(typeof(LibraryItemKind), (string)((FrameworkElement)sender).Tag);
            ShowView(kind);
        }

        private void Sidebar_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Right)
                return;

            LibraryContent.FocusLast();
            e.Handled = true;
        }

        private void Content_GotFocus(object sender, RoutedEventArgs e)
        {
            focusInContent = true;
        }

        private void Content_ExitLeft(object sender, EventArgs e)
        {
            focusInContent = false;
            CurrentViewButton.Focus();
        }

        private void Content_BusyChanged(object sender, EventArgs e)
        {
            IsLoading = LibraryContent.IsBusy;

            // Focus is blocked while loading, so put it back once the list is shown
            if (!IsLoading && IsLoaded)
                RestoreFocus();
        }

        private void Content_ItemInvoked(object sender, Controls.LibraryEntryEventArgs e)
        {
            MusicActions.Open(this, e.Entry, RestoreFocus);
        }

        #endregion
    }
}
