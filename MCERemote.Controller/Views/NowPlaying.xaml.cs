using MCERemote.Core;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MCERemote.Controller.Views
{
    /// <summary>
    /// Interaction logic for NowPlaying.xaml
    /// </summary>
    public partial class NowPlaying : Controls.PageView
    {
        public NowPlaying()
        {
            InitializeComponent();

            CoverView.Margin = coverMargin;
            ShowQueue = false;
        }

        private string FormatTime(TimeSpan time)
        {
            return $"{Math.Floor(time.TotalMinutes):00}:{time.Seconds:00}";
        }

        private Thickness coverMargin = new Thickness(50, 0, 0, 0);
        private Thickness queueMargin = new Thickness(0, 0, 0, 0);

        private bool _ShowQueue;
        private bool ShowQueue
        {
            get => _ShowQueue;
            set
            {
                _ShowQueue = value;

                CoverView.Visibility = value ? Visibility.Collapsed : Visibility.Visible;
                QueueView.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                CommonView.Margin = value ? queueMargin : coverMargin;

                ViewCoverButton.IsChecked = !value;
                ViewQueueButton.IsChecked = value;
            }
        }

        #region Event Handlers

        private void PageView_Loaded(object sender, RoutedEventArgs e)
        {
            if (ShowQueue)
                ViewQueueButton.Focus();
            else
                ViewCoverButton.Focus();

            if (Connection == null)
            {
                LoadMediaInfo(new MediaInfo());
                return;
            }

            // Loaded fires every time the page is shown, so avoid subscribing twice
            Connection.PlayerStatusUpdated -= Connection_StatusUpdated;
            Connection.NowPlayingChanged -= Connection_NowPlayingChanged;
            Connection.PlayerStatusUpdated += Connection_StatusUpdated;
            Connection.NowPlayingChanged += Connection_NowPlayingChanged;

            Connection.RefreshNow();

            LoadMediaInfo(Connection.NowPlaying);
        }

        private void Connection_NowPlayingChanged(object sender, NowPlayingChangedEventArgs e)
        {
            LoadMediaInfo(e.NowPlaying);
        }

        private void Connection_StatusUpdated(object sender, EventArgs e)
        {
            if (Connection.PlayerStatus.State == PlayerState.Unknown)
                return;

            TimeElapsed.Text = FormatTime(Connection.PlayerStatus.Position);
            ShuffleButton.IsChecked = Connection.PlayerStatus.Shuffle;
            RepeatButton.IsChecked = Connection.PlayerStatus.Repeat;
        }

        #endregion

        #region Media Loaders

        private void LoadTrackIndex()
        {
            Thread.Sleep(100);
            var index = Connection.Controller.GetQueuePosition();
            App.Current.Dispatcher.Invoke((Action)(() =>
            {
                TrackIndex.Text = $"{index:00}";
            })
            );
        }

        private void LoadAlbumArt()
        {
            var albumArt = Connection.Controller?.GetNowPlayingAlbumArt(true);
            if (albumArt != null)
            {
                App.Current.Dispatcher.Invoke((Action)(() =>
                {
                    AlbumArt.Source = ImageLoader.ByteArrayToBitmapImage(albumArt);
                }));
            }
            else
            {
                App.Current.Dispatcher.Invoke((Action)(() =>
                {
                    AlbumArt.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/General/Audio.NoCover.png"));
                }));
            }
        }

        private void LoadMediaInfo(MediaInfo media)
        {
            TrackTitle.Text = media.Title;

            if (media.Album.Length > 0 && media.AlbumArtist.Length > 0)
                AlbumAndArtist.Text = media.Album.ToUpper() + ": " + media.AlbumArtist;
            else
                AlbumAndArtist.Text = media.Album.ToUpper() + media.AlbumArtist;

            Year.Text = media.Year > 999 ? media.Year.ToString() : "";
            TimeTotal.Text = FormatTime(media.Duration);

            ThreadPool.QueueUserWorkItem(_ => { LoadAlbumArt(); });
            //ThreadPool.QueueUserWorkItem(_ => { LoadTrackIndex(); });
        }

        #endregion

        #region Button Events

        private void ShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Connection.Controller?.SendPlayerAction((bool)ShuffleButton.IsChecked ? PlayerAction.ShuffleOn : PlayerAction.ShuffleOff);
            }
            catch
            { }
        }

        private void RepeatButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Connection.Controller?.SendPlayerAction((bool)RepeatButton.IsChecked ? PlayerAction.RepeatOn : PlayerAction.RepeatOff);
            }
            catch
            { }
        }

        private void VisualizeButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void EditQueueButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ViewQueueButton_Click(object sender, RoutedEventArgs e)
        {
            ShowQueue = true;
        }

        private void ViewCoverButton_Click(object sender, RoutedEventArgs e)
        {
            ShowQueue = false;
        }
    }

    #endregion

}
