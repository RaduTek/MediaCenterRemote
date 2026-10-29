using MCERemote.Core;
using System;
using System.Threading;
using System.Windows;

namespace MCERemote.Controller.Views
{
    /// <summary>
    /// Shows the songs of an album or a playlist, with information about it.
    /// </summary>
    public partial class MusicCollection : Controls.PageView
    {
        /// <param name="kind">Either <see cref="LibraryItemKind.Album"/> or <see cref="LibraryItemKind.Playlist"/>.</param>
        /// <param name="name">The album or playlist name.</param>
        public MusicCollection(LibraryItemKind kind, string name)
        {
            InitializeComponent();

            query = kind == LibraryItemKind.Playlist
                ? new LibraryQuery(LibraryItemKind.Song, LibraryQuery.PlaylistFilter, name)
                : new LibraryQuery(LibraryItemKind.Song, "WM/AlbumTitle", name);

            Title = name;
            KindTitle.Text = kind == LibraryItemKind.Playlist ? "PLAYLIST" : "ALBUM";
            NameTitle.Text = name;
            NameTitle.Visibility = kind == LibraryItemKind.Playlist ? Visibility.Visible : Visibility.Collapsed;
            InfoTitle.Text = name;
        }

        #region Fields

        private readonly LibraryQuery query;

        private LibraryList songs;

        #endregion

        #region Private Methods

        private void LoadInfo()
        {
            var controller = Connection?.Controller;
            if (controller == null)
                return;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                LibraryInfo info = null;

                try
                {
                    info = controller.GetLibraryInfo(query);
                }
                catch
                { }

                App.Current.Dispatcher.Invoke((Action)(() =>
                {
                    if (info != null)
                        ShowInfo(info);
                }));
            });
        }

        private void ShowInfo(LibraryInfo info)
        {
            var artist = info.Artist ?? "";
            if (info.Year > 999)
                artist = artist.Length > 0 ? $"{artist}, {info.Year}" : info.Year.ToString();
            InfoArtist.Text = artist;

            var duration = info.TotalDuration;
            var length = duration.TotalHours >= 1
                ? $"{Math.Floor(duration.TotalHours)}:{duration.Minutes:00}:{duration.Seconds:00}"
                : $"{duration.Minutes}:{duration.Seconds:00}";
            InfoSummary.Text = $"{info.TrackCount} {(info.TrackCount == 1 ? "song" : "songs")}, {length}";
        }

        private void RestoreFocus()
        {
            LibraryContent.FocusLast();
        }

        #endregion

        #region Event Handlers

        private void PageView_Loaded(object sender, RoutedEventArgs e)
        {
            // Loaded fires again when returning to the page, which keeps its state
            if (songs == null && Connection != null)
            {
                songs = new LibraryList(Connection, query);
                LibraryContent.Show(songs);
                LoadInfo();
            }

            RestoreFocus();
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
