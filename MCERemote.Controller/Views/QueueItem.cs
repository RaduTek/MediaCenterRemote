using MCERemote.Core;
using System;
using System.ComponentModel;

namespace MCERemote.Controller.Views
{
    /// <summary>
    /// A playback queue entry shown in the Now Playing queue list.
    /// </summary>
    public class QueueItem : INotifyPropertyChanged
    {
        public QueueItem(PlaylistItem item)
        {
            Index = item.Index;
            SourceURL = item.SourceURL ?? "";
            Title = item.Title ?? "";
            Artist = item.Artist ?? "";
            DurationText = $"{Math.Floor(item.Duration.TotalMinutes)}:{item.Duration.Seconds:00}";
        }

        /// <summary>
        /// Gets the 1-based position of the item in the queue.
        /// </summary>
        public int Index { get; private set; }

        public string SourceURL { get; private set; }

        public string Title { get; private set; }

        public string Artist { get; private set; }

        public string DurationText { get; private set; }

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
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsCurrent"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
