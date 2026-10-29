using MCERemote.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;

namespace MCERemote.Controller
{
    public class QueueRangeLoadedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the 1-based position of the first item of the range.
        /// </summary>
        public int Start { get; private set; }

        public int Count { get; private set; }

        public QueueRangeLoadedEventArgs(int start, int count)
        {
            Start = start;
            Count = count;
        }
    }

    /// <summary>
    /// Client side copy of the receiver's playback queue. It always holds one item per
    /// queue entry, but item details are loaded lazily in chunks around the positions
    /// that are requested (the current track, or what the user is scrolling through).
    /// All members must be used from the UI thread.
    /// </summary>
    public class PlaybackQueue
    {
        /// <summary>
        /// Number of items loaded per request, centered on the requested position.
        /// </summary>
        public const int ChunkSize = 50;

        /// <summary>
        /// Maximum number of requests waiting to be loaded, older ones are dropped.
        /// </summary>
        private const int MaxPendingRequests = 4;

        public PlaybackQueue(Connection connection)
        {
            this.connection = connection;

            connection.QueueLengthChanged += Connection_QueueLengthChanged;
            connection.NowPlayingChanged += Connection_NowPlayingChanged;
            connection.PlayerStatusUpdated += Connection_PlayerStatusUpdated;
        }

        #region Fields

        private readonly Connection connection;

        private readonly ItemCollection items = new ItemCollection();

        // Incremented whenever loaded details become invalid, so results of older requests are ignored
        private int generation = 0;

        // Requests waiting to be loaded, newest last so it is loaded first
        private readonly List<Range> pending = new List<Range>();

        // Positions that are pending or being loaded, so they aren't requested twice
        private readonly HashSet<int> requested = new HashSet<int>();

        private bool isLoading = false;

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
        public event EventHandler<QueueRangeLoadedEventArgs> RangeLoaded;

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets whether all items in the chunk around the given position are loaded.
        /// </summary>
        public bool IsRangeLoaded(int center)
        {
            var range = GetChunk(center);
            for (int index = range.Start; index < range.Start + range.Count; index++)
            {
                if (!items[index - 1].IsLoaded)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Loads the chunk of items around the given 1-based position in the background,
        /// skipping items that are already loaded or requested.
        /// </summary>
        public void RequestRange(int center)
        {
            if (Length == 0)
                return;

            var chunk = GetChunk(center);

            // Trim the edges that don't need loading
            var start = chunk.Start;
            var end = chunk.Start + chunk.Count - 1;
            while (start <= end && IsLoadedOrRequested(start))
                start++;
            while (end >= start && IsLoadedOrRequested(end))
                end--;

            if (start > end)
                return;

            var range = new Range(start, end - start + 1);
            for (int index = range.Start; index <= end; index++)
                requested.Add(index);

            pending.Add(range);

            if (pending.Count > MaxPendingRequests)
            {
                // Drop the oldest request, it's likely no longer on screen
                var dropped = pending[0];
                pending.RemoveAt(0);
                for (int index = dropped.Start; index < dropped.Start + dropped.Count; index++)
                    requested.Remove(index);
            }

            LoadNext();
        }

        #endregion

        #region Private Methods

        private bool IsLoadedOrRequested(int index)
        {
            return items[index - 1].IsLoaded || requested.Contains(index);
        }

        private Range GetChunk(int center)
        {
            var count = Math.Min(ChunkSize, Length);
            var start = center - ChunkSize / 2;
            start = Math.Max(1, Math.Min(start, Length - count + 1));
            return new Range(start, count);
        }

        private void LoadNext()
        {
            if (isLoading || pending.Count == 0)
                return;

            var range = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);

            var controller = connection.Controller;
            var loadGeneration = generation;
            isLoading = true;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                List<PlaylistItem> loaded = null;

                try
                {
                    loaded = controller?.GetQueueRange(range.Start, range.Count);
                }
                catch
                { }

                App.Current.Dispatcher.Invoke((Action)(() =>
                {
                    isLoading = false;

                    // The queue changed while loading, so the results are outdated
                    if (loadGeneration == generation)
                    {
                        for (int index = range.Start; index < range.Start + range.Count; index++)
                            requested.Remove(index);

                        if (loaded != null)
                        {
                            foreach (var item in loaded)
                            {
                                if (item.Index >= 1 && item.Index <= Length)
                                    items[item.Index - 1].Update(item);
                            }
                        }

                        RangeLoaded?.Invoke(this, new QueueRangeLoadedEventArgs(range.Start, range.Count));
                    }

                    LoadNext();
                }));
            });
        }

        /// <summary>
        /// Marks all loaded details as outdated and cancels requests in progress.
        /// </summary>
        private void InvalidateAll()
        {
            generation++;
            pending.Clear();
            requested.Clear();

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

        #region Nested Types

        private struct Range
        {
            public Range(int start, int count)
            {
                Start = start;
                Count = count;
            }

            public int Start;
            public int Count;
        }

        /// <summary>
        /// Collection that can be resized with a single reset notification, rather than
        /// one notification per item, which would be slow for long queues.
        /// </summary>
        private class ItemCollection : ObservableCollection<QueueItem>
        {
            public void Resize(int length)
            {
                if (length == Count)
                    return;

                while (Items.Count > length)
                    Items.RemoveAt(Items.Count - 1);

                while (Items.Count < length)
                    Items.Add(new QueueItem(Items.Count + 1));

                OnPropertyChanged(new PropertyChangedEventArgs("Count"));
                OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        #endregion
    }
}
