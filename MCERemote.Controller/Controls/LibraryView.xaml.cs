using MCERemote.Core;
using System;
using System.Windows;
using System.Windows.Controls;

namespace MCERemote.Controller.Controls
{
    /// <summary>
    /// Shows a <see cref="LibraryList"/>: albums as a grid of tiles, other items as a list.
    /// Loads the list the first time it is shown.
    /// </summary>
    public partial class LibraryView : UserControl
    {
        public LibraryView()
        {
            InitializeComponent();
        }

        private LibraryList list;

        #region Properties

        /// <summary>
        /// Gets whether the shown list is loading for the first time.
        /// </summary>
        public bool IsBusy => list != null && list.IsListLoading;

        #endregion

        #region Events

        /// <summary>
        /// Raised when an entry is chosen with Enter or a tap.
        /// </summary>
        public event EventHandler<LibraryEntryEventArgs> ItemInvoked;

        /// <summary>
        /// Raised when Left is pressed at the left edge, to move focus out of the view.
        /// </summary>
        public event EventHandler ExitLeft;

        /// <summary>
        /// Raised when <see cref="IsBusy"/> changes.
        /// </summary>
        public event EventHandler BusyChanged;

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the given list, loading it if it wasn't loaded yet.
        /// </summary>
        public void Show(LibraryList list)
        {
            if (this.list != null)
                this.list.ListLoaded -= List_ListLoaded;

            this.list = list;

            var isGrid = list.Query.Kind == LibraryItemKind.Album;

            if (isGrid)
            {
                if (AlbumGrid.Source != list)
                    AlbumGrid.Source = list;
            }
            else
            {
                EntryList.EntryTemplate = (DataTemplate)FindResource(
                    list.Query.Kind == LibraryItemKind.Song ? "Library.SongTemplate" : "Library.NameTemplate");
                if (EntryList.Source != list)
                    EntryList.Source = list;
            }

            AlbumGrid.Visibility = isGrid ? Visibility.Visible : Visibility.Collapsed;
            EntryList.Visibility = isGrid ? Visibility.Collapsed : Visibility.Visible;

            list.ListLoaded += List_ListLoaded;
            if (!list.IsListLoaded)
                list.Load();

            UpdateStatus();
            BusyChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Focuses the entry that last had focus, or the first one.
        /// </summary>
        public void FocusLast()
        {
            if (list == null)
                return;

            if (list.Query.Kind == LibraryItemKind.Album)
                AlbumGrid.FocusLast();
            else
                EntryList.FocusLast();
        }

        #endregion

        #region Private Methods

        private void UpdateStatus()
        {
            var isEmpty = list != null && !list.IsListLoading && list.Length == 0;

            EmptyStatus.Text = isEmpty ? GetEmptyText(list.Query.Kind) : "";
            EmptyStatus.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
        }

        private static string GetEmptyText(LibraryItemKind kind)
        {
            switch (kind)
            {
                case LibraryItemKind.Album: return "No albums found";
                case LibraryItemKind.Artist: return "No artists found";
                case LibraryItemKind.Genre: return "No genres found";
                case LibraryItemKind.Playlist: return "No playlists found";
                default: return "No songs found";
            }
        }

        #endregion

        #region Event Handlers

        private void List_ListLoaded(object sender, EventArgs e)
        {
            UpdateStatus();
            BusyChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Content_ItemInvoked(object sender, LibraryEntryEventArgs e)
        {
            ItemInvoked?.Invoke(this, e);
        }

        private void Content_ExitLeft(object sender, EventArgs e)
        {
            ExitLeft?.Invoke(this, e);
        }

        #endregion
    }
}
