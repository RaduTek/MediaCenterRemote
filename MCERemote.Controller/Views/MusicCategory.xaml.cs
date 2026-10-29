using MCERemote.Core;
using System;
using System.Windows;
using System.Windows.Input;

namespace MCERemote.Controller.Views
{
    /// <summary>
    /// Shows the albums and songs of an artist or a genre.
    /// </summary>
    public partial class MusicCategory : Controls.PageView
    {
        /// <param name="kind">Either <see cref="LibraryItemKind.Artist"/> or <see cref="LibraryItemKind.Genre"/>.</param>
        /// <param name="name">The artist or genre name.</param>
        public MusicCategory(LibraryItemKind kind, string name)
        {
            InitializeComponent();

            this.name = name;
            filterAttribute = kind == LibraryItemKind.Genre ? "WM/Genre" : "WM/AlbumArtist";

            Title = name;
            KindTitle.Text = kind == LibraryItemKind.Genre ? "GENRE" : "ARTIST";
            NameTitle.Text = name;
        }

        #region Fields

        private readonly string name;
        private readonly string filterAttribute;

        private LibraryList albums;
        private LibraryList songs;

        private bool showSongs = false;

        // Whether focus was last in the content rather than the sidebar, to restore it
        private bool focusInContent = false;

        #endregion

        #region Private Methods

        private void ShowView(bool songsView)
        {
            showSongs = songsView;

            AlbumsButton.IsChecked = !songsView;
            SongsButton.IsChecked = songsView;

            // Created on first use, the connection is only known once the page is opened
            if (songsView)
                LibraryContent.Show(songs ?? (songs = new LibraryList(Connection, new LibraryQuery(LibraryItemKind.Song, filterAttribute, name))));
            else
                LibraryContent.Show(albums ?? (albums = new LibraryList(Connection, new LibraryQuery(LibraryItemKind.Album, filterAttribute, name))));
        }

        private Controls.ButtonRadio CurrentViewButton => showSongs ? SongsButton : AlbumsButton;

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
            if (albums == null && Connection != null)
                ShowView(false);

            RestoreFocus();
        }

        private void AlbumsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowView(false);
        }

        private void SongsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowView(true);
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
