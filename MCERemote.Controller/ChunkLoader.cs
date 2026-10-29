using MCERemote.Core;
using System;
using System.Collections.Generic;
using System.Threading;

namespace MCERemote.Controller
{
    public class RangeLoadedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the 1-based position of the first item of the range.
        /// </summary>
        public int Start { get; private set; }

        public int Count { get; private set; }

        public RangeLoadedEventArgs(int start, int count)
        {
            Start = start;
            Count = count;
        }
    }

    /// <summary>
    /// Loads the items of a long list from the receiver in chunks, around the positions that
    /// are requested. One chunk loads at a time in the background, newest request first.
    /// All members must be used from the UI thread.
    /// </summary>
    /// <typeparam name="TResult">The type of a loaded chunk.</typeparam>
    public class ChunkLoader<TResult> where TResult : class
    {
        /// <summary>
        /// Number of items loaded per request, centered on the requested position.
        /// </summary>
        public const int ChunkSize = 50;

        /// <summary>
        /// Maximum number of requests waiting to be loaded, older ones are dropped.
        /// </summary>
        private const int MaxPendingRequests = 4;

        /// <param name="connection">The connection to load from.</param>
        /// <param name="getLength">Gets the current list length.</param>
        /// <param name="isLoaded">Gets whether the item at a 1-based position is loaded.</param>
        /// <param name="fetch">Loads a range (start, count) in the background; may throw.</param>
        /// <param name="apply">Applies a loaded range (start, count, result) on the UI thread.
        /// The result is null if loading failed.</param>
        public ChunkLoader(Connection connection,
            Func<int> getLength,
            Func<int, bool> isLoaded,
            Func<IControllerService, int, int, TResult> fetch,
            Action<int, int, TResult> apply)
        {
            this.connection = connection;
            this.getLength = getLength;
            this.isLoaded = isLoaded;
            this.fetch = fetch;
            this.apply = apply;
        }

        #region Fields

        private readonly Connection connection;
        private readonly Func<int> getLength;
        private readonly Func<int, bool> isLoaded;
        private readonly Func<IControllerService, int, int, TResult> fetch;
        private readonly Action<int, int, TResult> apply;

        // Incremented whenever loaded items become invalid, so results of older requests are ignored
        private int generation = 0;

        // Requests waiting to be loaded, newest last so it is loaded first
        private readonly List<Range> pending = new List<Range>();

        // Positions that are pending or being loaded, so they aren't requested twice
        private readonly HashSet<int> requested = new HashSet<int>();

        private bool isLoading = false;

        #endregion

        /// <summary>
        /// Raised when a requested range finished loading, even if loading failed.
        /// </summary>
        public event EventHandler<RangeLoadedEventArgs> RangeLoaded;

        #region Public Methods

        /// <summary>
        /// Gets whether all items in the chunk around the given position are loaded.
        /// </summary>
        public bool IsRangeLoaded(int center)
        {
            var range = GetChunk(center);
            for (int index = range.Start; index < range.Start + range.Count; index++)
            {
                if (!isLoaded(index))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Loads the chunk of items around the given 1-based position in the background,
        /// skipping items that are already loaded or requested.
        /// </summary>
        public void Request(int center)
        {
            if (getLength() == 0)
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

        /// <summary>
        /// Cancels pending requests and ignores the results of the one in progress.
        /// Call when the loaded items become invalid.
        /// </summary>
        public void Invalidate()
        {
            generation++;
            pending.Clear();
            requested.Clear();
        }

        #endregion

        #region Private Methods

        private bool IsLoadedOrRequested(int index)
        {
            return isLoaded(index) || requested.Contains(index);
        }

        private Range GetChunk(int center)
        {
            var length = getLength();
            var count = Math.Min(ChunkSize, length);
            var start = center - ChunkSize / 2;
            start = Math.Max(1, Math.Min(start, length - count + 1));
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
                TResult result = null;

                try
                {
                    if (controller != null)
                        result = fetch(controller, range.Start, range.Count);
                }
                catch
                { }

                App.Current.Dispatcher.Invoke((Action)(() =>
                {
                    isLoading = false;

                    // The list changed while loading, so the results are outdated
                    if (loadGeneration == generation)
                    {
                        for (int index = range.Start; index < range.Start + range.Count; index++)
                            requested.Remove(index);

                        apply(range.Start, range.Count, result);

                        RangeLoaded?.Invoke(this, new RangeLoadedEventArgs(range.Start, range.Count));
                    }

                    LoadNext();
                }));
            });
        }

        #endregion

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
    }
}
