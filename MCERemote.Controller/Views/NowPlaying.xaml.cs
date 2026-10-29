using MCERemote.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

                if (value)
                    LoadQueue();
            }
        }

        #region Event Handlers

        private void PageView_Loaded(object sender, RoutedEventArgs e)
        {
            if (ShowQueue)
            {
                ViewQueueButton.Focus();
                LoadQueue();
            }
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

            if (ShowQueue)
                RefreshQueuePosition(e.NowPlaying.SourceURL);
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

        #region Queue

        private List<QueueItem> queue = new List<QueueItem>();

        // Incremented on every queue load so results of outdated requests are ignored
        private int queueLoadId = 0;

        private ListBoxItem pressedQueueItem;

        private void LoadQueue()
        {
            var loadId = ++queueLoadId;
            var controller = Connection?.Controller;

            queue = new List<QueueItem>();
            QueueList.ItemsSource = null;
            QueueStatus.Visibility = Visibility.Collapsed;
            IsLoading = true;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                List<PlaylistItem> playlist = null;
                var position = 0;

                try
                {
                    if (controller != null)
                    {
                        playlist = controller.GetNowPlayingPlaylist();
                        position = controller.GetQueuePosition();
                    }
                }
                catch
                { }

                App.Current.Dispatcher.Invoke((Action)(() =>
                {
                    // A newer load is in progress and will clear the loading state
                    if (loadId != queueLoadId)
                        return;

                    // Stop loading before focusing an item, as input is blocked while loading
                    IsLoading = false;

                    queue = (playlist ?? new List<PlaylistItem>()).Select(item => new QueueItem(item)).ToList();
                    QueueList.ItemsSource = queue;

                    if (queue.Count == 0)
                    {
                        QueueStatus.Text = "The queue is empty";
                        QueueStatus.Visibility = Visibility.Visible;
                        return;
                    }

                    UpdateQueueListHeight();
                    SetCurrentQueueItem(position, true);
                }));
            });
        }

        private void RefreshQueuePosition(string sourceURL)
        {
            var loadId = queueLoadId;
            var controller = Connection?.Controller;
            if (controller == null)
                return;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                var position = 0;

                try
                {
                    position = controller.GetQueuePosition();
                }
                catch
                { }

                App.Current.Dispatcher.Invoke((Action)(() =>
                {
                    if (loadId != queueLoadId || !ShowQueue)
                        return;

                    // The queue itself changed, so load it again
                    if (position < 1 || position > queue.Count || queue[position - 1].SourceURL != sourceURL)
                    {
                        LoadQueue();
                        return;
                    }

                    // Only move focus along if it was resting on the previously playing track
                    var previous = queue.FindIndex(item => item.IsCurrent);
                    var previousContainer = previous < 0 ? null : QueueList.ItemContainerGenerator.ContainerFromIndex(previous) as UIElement;
                    var followFocus = previousContainer != null && previousContainer.IsKeyboardFocused;

                    SetCurrentQueueItem(position, followFocus);
                }));
            });
        }

        // Height of one list item including its margin, measured from a realized item
        private double queueItemHeight = 0;

        /// <summary>
        /// Sizes the queue list to fit a whole number of items in the available area,
        /// so no item is ever shown cut off.
        /// </summary>
        private void UpdateQueueListHeight()
        {
            if (queueItemHeight <= 0 && queue.Count > 0)
            {
                QueueList.UpdateLayout();
                var container = QueueList.ItemContainerGenerator.ContainerFromIndex(0) as FrameworkElement;
                if (container != null && container.ActualHeight > 0)
                    queueItemHeight = container.ActualHeight + container.Margin.Top + container.Margin.Bottom;
            }

            if (queueItemHeight <= 0)
                return;

            var available = QueueArea.ActualHeight;
            var count = Math.Max(1, Math.Floor(available / queueItemHeight));
            QueueList.Height = count * queueItemHeight;
        }

        private void QueueArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateQueueListHeight();
        }

        private void SetCurrentQueueItem(int position, bool focus)
        {
            foreach (var item in queue)
                item.IsCurrent = item.Index == position;

            if (focus && position >= 1 && position <= queue.Count)
                FocusQueueItem(position - 1, true);
        }

        private void FocusQueueItem(int index, bool center)
        {
            if (!IsLoaded || !ShowQueue)
                return;

            QueueList.SelectedIndex = index;
            QueueList.UpdateLayout();

            var scroller = QueueList.Template.FindName("ListScroller", QueueList) as ScrollViewer;
            if (center && scroller != null)
                // Item based scrolling, so offsets are in items
                scroller.ScrollToVerticalOffset(Math.Max(0, index - Math.Floor(scroller.ViewportHeight / 2)));
            else
                QueueList.ScrollIntoView(queue[index]);

            QueueList.UpdateLayout();

            (QueueList.ItemContainerGenerator.ContainerFromIndex(index) as UIElement)?.Focus();
        }

        private void PlayQueueItem(QueueItem item)
        {
            var controller = Connection?.Controller;
            if (controller == null || item == null)
                return;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    controller.PlayQueueItem(item.Index);
                }
                catch
                { }

                App.Current.Dispatcher.Invoke((Action)(() => Connection?.RefreshNow()));
            });
        }

        private void QueueList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left)
            {
                ViewQueueButton.Focus();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                var container = Keyboard.FocusedElement as ListBoxItem;
                if (container != null && !e.IsRepeat)
                {
                    Controls.ButtonHelper.SetIsEnterPressed(container, true);
                    PlayQueueItem(container.DataContext as QueueItem);
                }
                e.Handled = true;
            }
        }

        private void QueueList_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            var container = Keyboard.FocusedElement as ListBoxItem;
            if (container != null)
                Controls.ButtonHelper.SetIsEnterPressed(container, false);
            e.Handled = true;
        }

        private void QueueList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            pressedQueueItem = ItemsControl.ContainerFromElement(QueueList, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (pressedQueueItem != null)
                Controls.ButtonHelper.SetIsEnterPressed(pressedQueueItem, true);
        }

        private void QueueList_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (pressedQueueItem == null)
                return;

            Controls.ButtonHelper.SetIsEnterPressed(pressedQueueItem, false);

            // Only play when released over the same item it was pressed on
            var container = ItemsControl.ContainerFromElement(QueueList, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (container == pressedQueueItem)
                PlayQueueItem(container.DataContext as QueueItem);

            pressedQueueItem = null;
        }

        private void QueueList_MouseLeave(object sender, MouseEventArgs e)
        {
            if (pressedQueueItem == null)
                return;

            Controls.ButtonHelper.SetIsEnterPressed(pressedQueueItem, false);
            pressedQueueItem = null;
        }

        private void SideButtons_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Right || !ShowQueue || queue.Count == 0)
                return;

            FocusQueueItem(Math.Max(0, QueueList.SelectedIndex), false);
            e.Handled = true;
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
