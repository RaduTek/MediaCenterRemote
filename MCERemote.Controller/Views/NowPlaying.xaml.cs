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
    public partial class NowPlaying : PageView
    {
        public NowPlaying()
        {
            InitializeComponent();
        }

        private string FormatTime(TimeSpan time)
        {
            return $"{Math.Floor(time.TotalMinutes):00}:{time.Seconds:00}";
        }

        private void Connection_StatusUpdated(object sender, EventArgs e)
        {
            if (Connection.PlayerStatus.State == PlayerState.Unknown)
            {
                return;
            }

            albumArtist.Text = Connection.PlayerStatus.Album.ToUpper() + ": " + Connection.PlayerStatus.Artist;
            trackTitle.Text = Connection.PlayerStatus.Title;
            year.Text = "";
            timeElapsed.Text = FormatTime(Connection.PlayerStatus.Position);
            timeTotal.Text = FormatTime(Connection.PlayerStatus.Duration);

            //var art = Connection.Controller?.GetThumbnail();
            //if (art != null)
            //{
            //    albumArt.Source = ImageLoader.ConvertToBitmapImage(art);
            //}
        }

        private void PageView_Loaded(object sender, RoutedEventArgs e)
        {
            Connection.PlayerStatusUpdated += Connection_StatusUpdated;
        }
    }
}
