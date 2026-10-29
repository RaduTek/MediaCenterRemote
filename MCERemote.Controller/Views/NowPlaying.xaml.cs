using MCERemote.Core;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

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

                // Only shows what's already loaded, the queue is loaded in the background
                if (value)
                    ShowQueueView();
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
            AttachQueue();

            Connection.RefreshNow();

            LoadMediaInfo(Connection.NowPlaying);

            if (ShowQueue)
                ShowQueueView();
        }

        private void PageView_Unloaded(object sender, RoutedEventArgs e)
        {
            DetachQueue();
        }

        private void Connection_NowPlayingChanged(object sender, NowPlayingChangedEventArgs e)
        {
            LoadMediaInfo(e.NowPlaying);
            QueueTrackChanged();
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

        // The queue is owned by the connection and loaded in the background, the view only
        // requests the regions it shows
        private PlaybackQueue Queue => Connection?.Queue;

        private int QueueLength => Queue?.Length ?? 0;

        private ListBoxItem pressedQueueItem;

        // Position the view waits on to be loaded before focusing it, 0 if not waiting
        private int awaitedQueuePosition = 0;

        // Current position as last seen by the view, to tell if focus rests on the playing track
        private int lastCurrentPosition = 0;

        // 0-based index of the queue item that last had focus, restored after the queue is resized
        private int lastFocusedQueueIndex = -1;

        // Debounces loading while scrolling
        private DispatcherTimer queueScrollTimer;

        private void AttachQueue()
        {
            var queue = Queue;
            if (queue == null)
                return;

            if (QueueList.ItemsSource != queue.Items)
                QueueList.ItemsSource = queue.Items;

            queue.Resized -= Queue_Resized;
            queue.RangeLoaded -= Queue_RangeLoaded;
            queue.Resized += Queue_Resized;
            queue.RangeLoaded += Queue_RangeLoaded;
        }

        private void DetachQueue()
        {
            var queue = Queue;
            if (queue != null)
            {
                queue.Resized -= Queue_Resized;
                queue.RangeLoaded -= Queue_RangeLoaded;
            }

            queueScrollTimer?.Stop();
            awaitedQueuePosition = 0;
            IsLoading = false;
        }

        /// <summary>
        /// Shows the queue as currently loaded and focuses the playing track. Only waits
        /// (with the loading overlay) if the region around the playing track isn't loaded yet.
        /// </summary>
        private void ShowQueueView()
        {
            var queue = Queue;
            awaitedQueuePosition = 0;
            IsLoading = false;

            if (queue == null || queue.Length == 0)
            {
                QueueStatus.Text = "The queue is empty";
                QueueStatus.Visibility = Visibility.Visible;
                return;
            }

            QueueStatus.Visibility = Visibility.Collapsed;
            UpdateQueueListHeight();

            var position = queue.CurrentPosition > 0 ? queue.CurrentPosition : 1;
            lastCurrentPosition = queue.CurrentPosition;

            if (queue.IsRangeLoaded(position))
            {
                FocusQueueItem(position - 1, true);
                return;
            }

            awaitedQueuePosition = position;
            IsLoading = true;
            queue.RequestRange(position);
        }

        /// <summary>
        /// Requests the items around the middle of the visible part of the list.
        /// </summary>
        private void RequestVisibleQueueRange()
        {
            var queue = Queue;
            var scroller = QueueList.Template?.FindName("ListScroller", QueueList) as ScrollViewer;
            if (queue == null || scroller == null || !ShowQueue)
                return;

            // Item based scrolling, so offsets are in items
            var first = (int)scroller.VerticalOffset + 1;
            var visible = (int)scroller.ViewportHeight;
            queue.RequestRange(first + visible / 2);
        }

        private void Queue_RangeLoaded(object sender, RangeLoadedEventArgs e)
        {
            var position = awaitedQueuePosition;
            if (position == 0 || position < e.Start || position >= e.Start + e.Count)
                return;

            // Stop loading before focusing an item, as input is blocked while loading
            awaitedQueuePosition = 0;
            IsLoading = false;
            FocusQueueItem(position - 1, true);
        }

        private void Queue_Resized(object sender, EventArgs e)
        {
            if (!ShowQueue)
                return;

            // Waiting on a position that may have moved or been cancelled, so start over
            if (awaitedQueuePosition > 0 || QueueLength == 0)
            {
                ShowQueueView();
                return;
            }

            QueueStatus.Visibility = Visibility.Collapsed;
            UpdateQueueListHeight();

            // Keep focus on the same position unless the user moved to the side buttons
            if (!CommonView.IsKeyboardFocusWithin && lastFocusedQueueIndex >= 0)
                FocusQueueItem(Math.Min(lastFocusedQueueIndex, QueueLength - 1), false);

            // Refresh only the region the user is looking at
            RequestVisibleQueueRange();
        }

        private void QueueTrackChanged()
        {
            var queue = Queue;
            if (queue == null || !ShowQueue)
                return;

            // The queue was replaced while waiting, so wait on the new playing track instead
            if (awaitedQueuePosition > 0)
            {
                ShowQueueView();
                return;
            }

            // Only move focus along if it was resting on the previously playing track
            var position = queue.CurrentPosition;
            var previousContainer = lastCurrentPosition < 1 ? null :
                QueueList.ItemContainerGenerator.ContainerFromIndex(lastCurrentPosition - 1) as UIElement;

            if (previousContainer != null && previousContainer.IsKeyboardFocused && position >= 1 && position <= queue.Length)
                FocusQueueItem(position - 1, false);

            lastCurrentPosition = position;
        }

        private void QueueList_GotFocus(object sender, RoutedEventArgs e)
        {
            var container = e.OriginalSource as ListBoxItem;
            if (container != null)
                lastFocusedQueueIndex = QueueList.ItemContainerGenerator.IndexFromContainer(container);
        }

        private void QueueList_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (queueScrollTimer == null)
            {
                queueScrollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
                queueScrollTimer.Tick += (s, args) =>
                {
                    queueScrollTimer.Stop();
                    RequestVisibleQueueRange();
                };
            }

            queueScrollTimer.Stop();
            queueScrollTimer.Start();
        }

        // Height of one list item including its margin, measured from a realized item
        private double queueItemHeight = 0;

        /// <summary>
        /// Sizes the queue list to fit a whole number of items in the available area,
        /// so no item is ever shown cut off.
        /// </summary>
        private void UpdateQueueListHeight()
        {
            Controls.ListFitter.FitToWholeItems(QueueList, QueueArea, ref queueItemHeight);
        }

        private void QueueArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateQueueListHeight();
        }

        private void FocusQueueItem(int index, bool center)
        {
            if (!IsLoaded || !ShowQueue || index < 0 || index >= QueueLength)
                return;

            QueueList.SelectedIndex = index;
            QueueList.UpdateLayout();

            var scroller = QueueList.Template.FindName("ListScroller", QueueList) as ScrollViewer;
            if (center && scroller != null)
                // Item based scrolling, so offsets are in items
                scroller.ScrollToVerticalOffset(Math.Max(0, index - Math.Floor(scroller.ViewportHeight / 2)));
            else
                QueueList.ScrollIntoView(Queue.Items[index]);

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
            if (e.Key != Key.Right || !ShowQueue || QueueLength == 0)
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
