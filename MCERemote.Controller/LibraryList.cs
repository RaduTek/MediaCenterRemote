using MCERemote.Core;
using System;
using System.Collections.ObjectModel;
using System.Threading;

namespace MCERemote.Controller
{
    /// <summary>
    /// A media library list for one query. It holds one entry per item once the length is
    /// known, but entry details are loaded lazily in chunks around the requested positions.
    /// All members must be used from the UI thread.
    /// </summary>
    public class LibraryList
    {
        public LibraryList(Connection connection, LibraryQuery query)
        {
            this.connection = connection;
            Query = query;

            items = new ResizableCollection<LibraryEntry>(index => new LibraryEntry(index, query.Kind));
            loader = new ChunkLoader<LibraryRange>(connection,
                () => Length,
                index => items[index - 1].IsLoaded,
                (controller, start, count) => controller.GetLibraryRange(Query, start, count, false),
                ApplyRange);
            loader.RangeLoaded += (s, e) => RangeLoaded?.Invoke(this, e);
        }

        #region Fields

        private readonly Connection connection;

        private readonly ResizableCollection<LibraryEntry> items;

        private readonly ChunkLoader<LibraryRange> loader;

        #endregion

        #region Properties

        public LibraryQuery Query { get; private set; }

        /// <summary>
        /// Gets the list entries. Bind to this collection directly.
        /// </summary>
        public ObservableCollection<LibraryEntry> Items => items;

        public int Length => items.Count;

        /// <summary>
        /// Gets whether the first chunk was loaded and the length is known. Stays false if
        /// loading failed (e.g. while disconnected), so the next <see cref="Load"/> retries.
        /// </summary>
        public bool IsListLoaded { get; private set; }

        /// <summary>
        /// Gets whether the first chunk is being loaded.
        /// </summary>
        public bool IsListLoading { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// Raised when the first chunk finished loading and the length is known, even if
        /// loading failed (the list is then empty).
        /// </summary>
        public event EventHandler ListLoaded;

        /// <summary>
        /// Raised when a requested range finished loading, even if loading failed.
        /// </summary>
        public event EventHandler<RangeLoadedEventArgs> RangeLoaded;

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads the list length and its first chunk, refreshing the receiver's copy of the list.
        /// </summary>
        public void Load()
        {
            if (IsListLoading)
                return;

            IsListLoading = true;
            loader.Invalidate();

            var controller = connection.Controller;
            var query = Query;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                LibraryRange range = null;

                try
                {
                    range = controller?.GetLibraryRange(query, 1, ChunkLoader<LibraryRange>.ChunkSize, true);
                }
                catch
                { }

                App.Current.Dispatcher.Invoke((Action)(() =>
                {
                    IsListLoading = false;
                    IsListLoaded = range != null;

                    items.Resize(0);
                    items.Resize(range?.TotalCount ?? 0);
                    ApplyRange(1, ChunkLoader<LibraryRange>.ChunkSize, range);

                    ListLoaded?.Invoke(this, EventArgs.Empty);
                }));
            });
        }

        /// <summary>
        /// Loads the chunk of entries around the given 1-based position in the background,
        /// skipping entries that are already loaded or requested.
        /// </summary>
        public void RequestRange(int center) => loader.Request(center);

        #endregion

        #region Private Methods

        private void ApplyRange(int start, int count, LibraryRange range)
        {
            if (range == null)
                return;

            foreach (var item in range.Items)
            {
                if (item.Index >= 1 && item.Index <= Length)
                    items[item.Index - 1].Update(item);
            }
        }

        #endregion
    }
}
