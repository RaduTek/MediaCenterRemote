using System;

namespace MCERemote.Core
{
    public enum MediaType
    {
        Unknown,
        Music,
        Video,
        Movie,
        DVD,
        Image
    }

    public class MediaInfo
    {
        public string SourceURL { get; set; }

        public MediaType Type { get; set; }

        public string Title { get; set; }
        public string Artist { get; set; }
        public string Genre { get; set; }
        public string Composer { get; set; }
        public int Year { get; set; }

        public int TrackNumber { get; set; }
        public string Album { get; set; }
        public string AlbumArtist { get; set; }
        public int AlbumTotal { get; set; }

        public TimeSpan Duration { get; set; }

        public MediaInfo()
        {
            SourceURL = "";

            Type = MediaType.Unknown;

            Title = "";
            Artist = "";
            Genre = "";
            Composer = "";
            Year = 0;

            TrackNumber = 0;
            Album = "";
            AlbumArtist = "";
            AlbumTotal = 0;

            Duration = TimeSpan.Zero;
        }
    }
}
