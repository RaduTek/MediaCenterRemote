using MCERemote.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MCERemote.Controller
{
    /// <summary>
    /// Client side copy of the receiver's playback queue. It always holds one item per
    /// queue entry, but item details are loaded lazily in chunks around the positions
    /// that are requested (the current track, or what the user is scrolling through).
    /// All members must be used from the UI thread.
    /// </summary>
    public class PlaybackQueue
    {
        public PlaybackQueue(Connection connection)
        {
            this.connection = connection;

            items = new ResizableCollection<QueueItem>(index => new QueueItem(index));
            loader = new ChunkLoader<List<PlaylistItem>>(connection,
                () => Length,
                index => items[index - 1].IsLoaded,
                (controller, start, count) => controller.GetQueueRange(start, count),
                ApplyRange);
            loader.RangeLoaded += (s, e) => RangeLoaded?.Invoke(this, e);

            connection.QueueLengthChanged += Connection_QueueLengthChanged;
            connection.NowPlayingChanged += Connection_NowPlayingChanged;
            connection.PlayerStatusUpdated += Connection_PlayerStatusUpdated;
        }

        #region Fields

        private readonly Connection connection;

        private readonly ResizableCollection<QueueItem> items;

        private readonly ChunkLoader<List<PlaylistItem>> loader;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the queue items, one per queue entry. Bind to this collection directly.
        /// </summary>
        public ObservableCollection<QueueItem> Items => items;

        public int Length => items.Count;

        /// <summary>
        /// Gets the 1-based position of the currently playing item, or 0 if unknown.
        /// </summary>
        public int CurrentPosition { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// Raised after the queue length changed and the items were replaced with
        /// placeholders or outdated details.
        /// </summary>
        public event EventHandler Resized;

        /// <summary>
        /// Raised when a requested range finished loading, even if loading failed.
        /// </summary>
        public event EventHandler<RangeLoadedEventArgs> RangeLoaded;

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets whether all items in the chunk around the given position are loaded.
        /// </summary>
        public bool IsRangeLoaded(int center) => loader.IsRangeLoaded(center);

        /// <summary>
        /// Loads the chunk of items around the given 1-based position in the background,
        /// skipping items that are already loaded or requested.
        /// </summary>
        public void RequestRange(int center) => loader.Request(center);

        #endregion

        #region Private Methods

        private void ApplyRange(int start, int count, List<PlaylistItem> loaded)
        {
            if (loaded == null)
                return;

            foreach (var item in loaded)
            {
                if (item.Index >= 1 && item.Index <= Length)
                    items[item.Index - 1].Update(item);
            }
        }

        /// <summary>
        /// Marks all loaded details as outdated and cancels requests in progress.
        /// </summary>
        private void InvalidateAll()
        {
            loader.Invalidate();

            foreach (var item in items)
                item.Invalidate();
        }

        private void Resize(int length)
        {
            InvalidateAll();

            // Items can be inserted or removed anywhere, so positions of loaded details are
            // no longer reliable; they stay as outdated placeholders until loaded again
            items.Resize(length);

            var position = CurrentPosition;
            CurrentPosition = 0;
            SetCurrentPosition(position);

            Resized?.Invoke(this, EventArgs.Empty);
        }

        private void SetCurrentPosition(int position)
        {
            if (position == CurrentPosition)
                return;

            if (CurrentPosition >= 1 && CurrentPosition <= Length)
                items[CurrentPosition - 1].IsCurrent = false;

            CurrentPosition = position;

            if (CurrentPosition >= 1 && CurrentPosition <= Length)
                items[CurrentPosition - 1].IsCurrent = true;
        }

        #endregion

        #region Event Handlers

        private void Connection_QueueLengthChanged(object sender, QueueLengthChangedEventArgs e)
        {
            Resize(e.NewLength);
            RequestRange(connection.PlayerStatus.QueuePosition);
        }

        private void Connection_NowPlayingChanged(object sender, NowPlayingChangedEventArgs e)
        {
            var position = connection.PlayerStatus.QueuePosition;
            SetCurrentPosition(position);

            if (position < 1 || position > Length)
                return;

            // A different queue of the same length was loaded, so everything is outdated
            var item = items[position - 1];
            if (item.IsLoaded && item.SourceURL != (e.NowPlaying.SourceURL ?? ""))
                InvalidateAll();

            // Preload around the new track, so the queue is ready when the user opens it
            RequestRange(position);
        }

        private void Connection_PlayerStatusUpdated(object sender, PlayerStatusUpdatedEventArgs e)
        {
            SetCurrentPosition(e.PlayerStatus.QueuePosition);
        }

        #endregion
    }
}
