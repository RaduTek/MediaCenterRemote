using MCERemote.Core;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace MCERemote.Controller.Views
{
    /// <summary>
    /// Interaction logic for NowPlaying.xaml
    /// </summary>
    public partial class NowPlaying : Page
    {
        public NowPlaying()
        {
            InitializeComponent();
        }

        public Connection Connection { get; set; }


        private PlayerStatus _PlayerStatus = PlayerStatus.Unknown;
        public PlayerStatus PlayerStatus
        {
            get { return _PlayerStatus; }
            set
            {
                _PlayerStatus = value;
                UpdateStatus();
            }
        }

        private void UpdateStatus()
        {
            if (PlayerStatus.State == PlayerState.Unknown)
            {
                return;
            }

            albumArtist.Text = PlayerStatus.Album.ToUpper() + ": " + PlayerStatus.Artist;
            trackTitle.Text = PlayerStatus.Title;
            year.Text = "";
            timeElapsed.Text = FormatTime(PlayerStatus.Position);
            timeTotal.Text = FormatTime(PlayerStatus.Duration);

            var art = Connection.Controller?.GetThumbnail();
            if (art != null)
            {
                albumArt.Source = ImageLoader.ConvertToBitmapImage(art);
            }
        }

        private string FormatTime(TimeSpan time)
        {
            return $"{Math.Floor(time.TotalMinutes):00}:{time.Seconds:00}";
        }
    }
}
