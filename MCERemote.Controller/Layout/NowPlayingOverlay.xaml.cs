using MCERemote.Core;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace MCERemote.Controller.Layout
{
    /// <summary>
    /// Interaction logic for NowPlayingOverlay.xaml
    /// </summary>
    public partial class NowPlayingOverlay : UserControl
    {
        public NowPlayingOverlay()
        {
            InitializeComponent();
        }

        private Connection _Connection;
        /// <summary>
        /// Gets or sets the connection to the receiver.
        /// </summary>
        public Connection Connection
        {
            get => _Connection;
            set
            {
                if (_Connection != null)
                {
                    _Connection.Closed -= Connection_Closed;
                    _Connection.PlayerStatusUpdated -= Connection_PlayerStatusUpdated;
                    _Connection.NowPlayingChanged -= Connection_NowPlayingChanged;
                }

                _Connection = value;

                if (_Connection != null)
                {
                    _Connection.Closed += Connection_Closed;
                    _Connection.PlayerStatusUpdated += Connection_PlayerStatusUpdated;
                    _Connection.NowPlayingChanged += Connection_NowPlayingChanged;
                }
            }
        }

        private bool _CanShow = true;
        /// <summary>
        /// Gets or sets whether the overlay may be shown. The overlay is only
        /// visible when this is true and media is playing or paused.
        /// </summary>
        public bool CanShow
        {
            get => _CanShow;
            set
            {
                _CanShow = value;
                UpdateVisibility();
            }
        }

        private bool hasMedia = false;

        private void UpdateVisibility()
        {
            Visibility = CanShow && hasMedia ? Visibility.Visible : Visibility.Collapsed;
        }

        #region Event Handlers

        private void Connection_Closed(object sender, ConnectionClosedEventArgs e)
        {
            hasMedia = false;
            UpdateVisibility();
        }

        private void Connection_PlayerStatusUpdated(object sender, PlayerStatusUpdatedEventArgs e)
        {
            var state = e.PlayerStatus.State;
            hasMedia = state == PlayerState.Playing || state == PlayerState.Paused;
            UpdateVisibility();

            if (hasMedia)
                TimeElapsedText.Text = FormatTime(e.PlayerStatus.Position);
        }

        private string FormatTime(TimeSpan time)
        {
            return $"{Math.Floor(time.TotalMinutes)}:{time.Seconds:00}";
        }

        private void Connection_NowPlayingChanged(object sender, NowPlayingChangedEventArgs e)
        {
            LoadMediaInfo(e.NowPlaying);
        }

        #endregion

        #region Media Loaders

        private void LoadAlbumArt()
        {
            var albumArt = Connection.Controller?.GetNowPlayingAlbumArt(true);
            App.Current.Dispatcher.Invoke((Action)(() =>
            {
                if (albumArt != null)
                    AlbumArt.Source = ImageLoader.ByteArrayToBitmapImage(albumArt);
                else
                    AlbumArt.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/General/Audio.NoCover.png"));
            }));
        }

        private void LoadMediaInfo(MediaInfo media)
        {
            TitleText.Text = media.Title;
            ArtistText.Text = string.IsNullOrEmpty(media.Artist) ? media.AlbumArtist : media.Artist;
            
            ThreadPool.QueueUserWorkItem(_ => { LoadAlbumArt(); });
        }

        #endregion
    }
}
