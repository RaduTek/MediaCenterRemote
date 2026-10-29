using MCERemote.Core;
using System;
using System.ComponentModel;

namespace MCERemote.Controller
{
    /// <summary>
    /// A playback queue entry. Starts as a placeholder and is filled in once its
    /// details are loaded from the receiver.
    /// </summary>
    public class QueueItem : INotifyPropertyChanged
    {
        /// <summary>
        /// Creates a placeholder for the item at the given 1-based queue position.
        /// </summary>
        public QueueItem(int index)
        {
            Index = index;
            SourceURL = "";
            Title = "";
            Artist = "";
            DurationText = "";
        }

        /// <summary>
        /// Gets the 1-based position of the item in the queue.
        /// </summary>
        public int Index { get; private set; }

        public string SourceURL { get; private set; }

        public string Title { get; private set; }

        public string Artist { get; private set; }

        public string DurationText { get; private set; }

        /// <summary>
        /// Gets whether the details shown are up to date with the receiver.
        /// </summary>
        public bool IsLoaded { get; private set; }

        private bool _IsCurrent;
        /// <summary>
        /// Gets or sets whether this item is the one currently playing.
        /// </summary>
        public bool IsCurrent
        {
            get => _IsCurrent;
            set
            {
                if (_IsCurrent == value)
                    return;

                _IsCurrent = value;
                OnPropertyChanged("IsCurrent");
            }
        }

        /// <summary>
        /// Fills in the item details loaded from the receiver.
        /// </summary>
        public void Update(PlaylistItem item)
        {
            SourceURL = item.SourceURL ?? "";
            Title = item.Title ?? "";
            Artist = item.Artist ?? "";
            DurationText = $"{Math.Floor(item.Duration.TotalMinutes)}:{item.Duration.Seconds:00}";
            IsLoaded = true;

            OnPropertyChanged("SourceURL");
            OnPropertyChanged("Title");
            OnPropertyChanged("Artist");
            OnPropertyChanged("DurationText");
            OnPropertyChanged("IsLoaded");
        }

        /// <summary>
        /// Marks the details as outdated so they get loaded again. The old details stay
        /// visible until then.
        /// </summary>
        public void Invalidate()
        {
            if (!IsLoaded)
                return;

            IsLoaded = false;
            OnPropertyChanged("IsLoaded");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
