using System;
using System.Collections.Generic;

namespace MCERemote.Core
{
    public enum LibraryItemKind
    {
        Album,
        Artist,
        Genre,
        Playlist,
        Song
    }

    /// <summary>
    /// Describes a list of media library items, optionally filtered by an attribute.
    /// </summary>
    public class LibraryQuery
    {
        /// <summary>
        /// Gets or sets the kind of items to list.
        /// </summary>
        public LibraryItemKind Kind { get; set; }

        /// <summary>
        /// Gets or sets the attribute to filter by, e.g. "WM/AlbumArtist", "WM/Genre",
        /// "WM/AlbumTitle", or <see cref="PlaylistFilter"/> to list the songs of a playlist.
        /// Empty for no filter.
        /// </summary>
        public string FilterAttribute { get; set; }

        /// <summary>
        /// Gets or sets the value the filter attribute must equal.
        /// </summary>
        public string FilterValue { get; set; }

        /// <summary>
        /// Filter attribute that selects the songs of the playlist named by the filter value.
        /// </summary>
        public const string PlaylistFilter = "Playlist";

        public LibraryQuery()
        {
            FilterAttribute = "";
            FilterValue = "";
        }

        public LibraryQuery(LibraryItemKind kind, string filterAttribute = "", string filterValue = "")
        {
            Kind = kind;
            FilterAttribute = filterAttribute ?? "";
            FilterValue = filterValue ?? "";
        }

        /// <summary>
        /// Gets a key that identifies this query, for caching.
        /// </summary>
        public string Key => $"{Kind}|{FilterAttribute}|{FilterValue}";
    }

    /// <summary>
    /// An item of a media library list.
    /// </summary>
    public class LibraryItem
    {
        /// <summary>
        /// Gets or sets the 1-based position of the item in its list.
        /// </summary>
        public int Index { get; set; }

        public LibraryItemKind Kind { get; set; }

        /// <summary>
        /// Gets or sets the item name: song, album, artist, genre or playlist name.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the song artist. Songs only.
        /// </summary>
        public string Subtitle { get; set; }

        /// <summary>
        /// Gets or sets the song album. Songs only.
        /// </summary>
        public string Album { get; set; }

        /// <summary>
        /// Gets or sets the song duration. Songs only.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Gets or sets the song file location, which identifies it. Songs only.
        /// </summary>
        public string SourceURL { get; set; }
    }

    /// <summary>
    /// A range of items of a media library list.
    /// </summary>
    public class LibraryRange
    {
        /// <summary>
        /// Gets or sets the total number of items in the list.
        /// </summary>
        public int TotalCount { get; set; }

        public List<LibraryItem> Items { get; set; }

        public LibraryRange()
        {
            Items = new List<LibraryItem>();
        }
    }

    /// <summary>
    /// Summary of an album or playlist.
    /// </summary>
    public class LibraryInfo
    {
        public string Title { get; set; }

        public string Artist { get; set; }

        public int Year { get; set; }

        public int TrackCount { get; set; }

        public TimeSpan TotalDuration { get; set; }

        public LibraryInfo()
        {
            Title = "";
            Artist = "";
        }
    }
}
