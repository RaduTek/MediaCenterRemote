using MCERemote.Core;
using System;
using System.Collections.Generic;
using WMPLib;

namespace MCERemote.Receiver.Controller
{
    /// <summary>
    /// Lists media library contents in ranges. The result of each query is built once and
    /// cached, so loading further ranges of the same list only reads the items in the range.
    /// </summary>
    internal class Library
    {
        public Library(WindowsMediaPlayer player)
        {
            this.player = player;
        }

        #region Fields

        private const int MaxCachedSources = 8;

        private readonly WindowsMediaPlayer player;

        // Most recently used last
        private readonly List<KeyValuePair<string, Source>> sources = new List<KeyValuePair<string, Source>>();

        #endregion

        #region Public Methods

        public LibraryRange GetRange(LibraryQuery query, int start, int count, bool refresh)
        {
            var range = new LibraryRange();
            var source = GetSource(query, refresh);

            range.TotalCount = source.Count;

            var first = Math.Max(1, start);
            var last = Math.Min(source.Count, start + count - 1);

            for (int index = first; index <= last; index++)
                range.Items.Add(source.GetItem(index));

            return range;
        }

        public LibraryInfo GetInfo(LibraryQuery query)
        {
            var info = new LibraryInfo { Title = query.FilterValue ?? "" };

            var songs = GetSongs(query);
            if (songs == null)
                return info;

            var count = songs.count;
            var duration = 0.0;

            for (int i = 0; i < count; i++)
            {
                var media = songs.Item[i];
                duration += media.duration;

                if (i == 0)
                {
                    info.Artist = GetArtist(media);
                    int.TryParse(media.getItemInfo("WM/Year"), out int year);
                    info.Year = year;
                }
            }

            info.TrackCount = count;
            info.TotalDuration = TimeSpan.FromSeconds(Math.Floor(duration));

            return info;
        }

        /// <summary>
        /// Finds a song of the media library by its file location.
        /// </summary>
        public IWMPMedia FindSong(string sourceURL)
        {
            var results = player.mediaCollection.getByAttribute("SourceURL", sourceURL);
            return results != null && results.count > 0 ? results.Item[0] : null;
        }

        public static LibraryItem ToSongItem(IWMPMedia media, int index)
        {
            return new LibraryItem
            {
                Index = index,
                Kind = LibraryItemKind.Song,
                Title = media.name,
                Subtitle = GetArtist(media),
                Album = media.getItemInfo("WM/AlbumTitle"),
                Duration = TimeSpan.FromSeconds(media.duration),
                SourceURL = media.sourceURL
            };
        }

        #endregion

        #region Private Methods

        private static string GetArtist(IWMPMedia media)
        {
            var artist = media.getItemInfo("WM/AlbumArtist");
            return string.IsNullOrEmpty(artist) ? media.getItemInfo("Author") : artist;
        }

        private Source GetSource(LibraryQuery query, bool refresh)
        {
            var key = query.Key;
            var cached = sources.FindIndex(pair => pair.Key == key);

            if (cached >= 0)
            {
                var entry = sources[cached];
                sources.RemoveAt(cached);

                if (!refresh)
                {
                    sources.Add(entry);
                    return entry.Value;
                }
            }

            var source = CreateSource(query);

            sources.Add(new KeyValuePair<string, Source>(key, source));
            if (sources.Count > MaxCachedSources)
                sources.RemoveAt(0);

            return source;
        }

        private Source CreateSource(LibraryQuery query)
        {
            switch (query.Kind)
            {
                case LibraryItemKind.Album:
                    return new StringSource(LibraryItemKind.Album, GetStrings("WM/AlbumTitle", query));
                case LibraryItemKind.Artist:
                    return new StringSource(LibraryItemKind.Artist, GetStrings("WM/AlbumArtist", query));
                case LibraryItemKind.Genre:
                    return new StringSource(LibraryItemKind.Genre, GetStrings("WM/Genre", query));
                case LibraryItemKind.Playlist:
                    return new StringSource(LibraryItemKind.Playlist, GetPlaylistNames());
                case LibraryItemKind.Song:
                    return new SongSource(GetSongs(query));
                default:
                    return new StringSource(query.Kind, new List<string>());
            }
        }

        private IWMPMediaCollection2 Collection => (IWMPMediaCollection2)player.mediaCollection;

        private IWMPQuery CreateQuery(LibraryQuery query)
        {
            var wmpQuery = Collection.createQuery();
            if (!string.IsNullOrEmpty(query.FilterAttribute) && query.FilterAttribute != LibraryQuery.PlaylistFilter)
                wmpQuery.addCondition(query.FilterAttribute, "Equals", query.FilterValue);
            return wmpQuery;
        }

        /// <summary>
        /// Gets the distinct, sorted, non-empty values of an attribute among the audio items
        /// matching the query filter.
        /// </summary>
        private List<string> GetStrings(string attribute, LibraryQuery query)
        {
            var strings = Collection.getStringCollectionByQuery(attribute, CreateQuery(query), "audio", attribute, true);
            var values = new List<string>();

            var count = strings.count;
            for (int i = 0; i < count; i++)
            {
                var value = strings.Item(i);
                if (!string.IsNullOrEmpty(value))
                    values.Add(value);
            }

            return values;
        }

        private List<string> GetPlaylistNames()
        {
            var playlists = player.playlistCollection.getAll();
            var names = new List<string>();

            var count = playlists.count;
            for (int i = 0; i < count; i++)
            {
                var name = playlists.Item(i).name;
                if (!string.IsNullOrEmpty(name))
                    names.Add(name);
            }

            names.Sort(StringComparer.CurrentCultureIgnoreCase);
            return names;
        }

        /// <summary>
        /// Gets the songs matching the query filter: a playlist's songs in playlist order,
        /// an album's songs in track order, or other songs by title.
        /// </summary>
        private IWMPPlaylist GetSongs(LibraryQuery query)
        {
            if (query.FilterAttribute == LibraryQuery.PlaylistFilter)
            {
                var playlists = player.playlistCollection.getByName(query.FilterValue);
                return playlists.count > 0 ? playlists.Item(0) : null;
            }

            var sortAttribute = query.FilterAttribute == "WM/AlbumTitle" ? "WM/TrackNumber" : "Title";
            return Collection.getPlaylistByQuery(CreateQuery(query), "audio", sortAttribute, true);
        }

        #endregion

        #region Sources

        private abstract class Source
        {
            public abstract int Count { get; }

            public abstract LibraryItem GetItem(int index);
        }

        private class StringSource : Source
        {
            public StringSource(LibraryItemKind kind, List<string> values)
            {
                this.kind = kind;
                this.values = values;
            }

            private readonly LibraryItemKind kind;
            private readonly List<string> values;

            public override int Count => values.Count;

            public override LibraryItem GetItem(int index)
            {
                return new LibraryItem { Index = index, Kind = kind, Title = values[index - 1] };
            }
        }

        private class SongSource : Source
        {
            public SongSource(IWMPPlaylist songs)
            {
                this.songs = songs;
                count = songs?.count ?? 0;
            }

            private readonly IWMPPlaylist songs;
            private readonly int count;

            public override int Count => count;

            public override LibraryItem GetItem(int index)
            {
                return ToSongItem(songs.Item[index - 1], index);
            }
        }

        #endregion
    }
}
