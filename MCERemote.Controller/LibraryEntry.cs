using MCERemote.Core;
using System;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MCERemote.Controller
{
    /// <summary>
    /// A media library list entry. Starts as a placeholder and is filled in once its details
    /// are loaded from the receiver.
    /// </summary>
    public class LibraryEntry : INotifyPropertyChanged
    {
        private static readonly ImageSource NoCover =
            new BitmapImage(new Uri("pack://application:,,,/Resources/General/Audio.NoCover.png"));

        /// <summary>
        /// Creates a placeholder for the entry at the given 1-based position.
        /// </summary>
        public LibraryEntry(int index, LibraryItemKind kind)
        {
            Index = index;
            Kind = kind;
            Title = "";
            Subtitle = "";
            Album = "";
            DurationText = "";
            SourceURL = "";
        }

        /// <summary>
        /// Gets the 1-based position of the entry in its list.
        /// </summary>
        public int Index { get; private set; }

        public LibraryItemKind Kind { get; private set; }

        public string Title { get; private set; }

        /// <summary>
        /// Gets the song artist. Songs only.
        /// </summary>
        public string Subtitle { get; private set; }

        /// <summary>
        /// Gets the song album. Songs only.
        /// </summary>
        public string Album { get; private set; }

        public string DurationText { get; private set; }

        public string SourceURL { get; private set; }

        /// <summary>
        /// Gets the cover image. Album art isn't loaded yet, so this is always the placeholder.
        /// </summary>
        public ImageSource CoverImage => NoCover;

        public bool IsLoaded { get; private set; }

        /// <summary>
        /// Fills in the entry details loaded from the receiver.
        /// </summary>
        public void Update(LibraryItem item)
        {
            Title = item.Title ?? "";
            Subtitle = item.Subtitle ?? "";
            Album = item.Album ?? "";
            SourceURL = item.SourceURL ?? "";
            DurationText = Kind == LibraryItemKind.Song
                ? $"{Math.Floor(item.Duration.TotalMinutes)}:{item.Duration.Seconds:00}"
                : "";
            IsLoaded = true;

            OnPropertyChanged("Title");
            OnPropertyChanged("Subtitle");
            OnPropertyChanged("Album");
            OnPropertyChanged("SourceURL");
            OnPropertyChanged("DurationText");
            OnPropertyChanged("IsLoaded");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
