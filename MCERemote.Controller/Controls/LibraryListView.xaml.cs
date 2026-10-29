using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace MCERemote.Controller.Controls
{
    public class LibraryEntryEventArgs : EventArgs
    {
        public LibraryEntry Entry { get; private set; }

        public LibraryEntryEventArgs(LibraryEntry entry)
        {
            Entry = entry;
        }
    }

    /// <summary>
    /// A scrolling list of a <see cref="LibraryList"/>, showing whole items only and loading
    /// the entries around the visible ones as the user scrolls.
    /// </summary>
    public partial class LibraryListView : UserControl
    {
        public LibraryListView()
        {
            InitializeComponent();
        }

        #region Fields

        private LibraryList source;

        private ListBoxItem pressedItem;

        // 0-based index of the entry that last had focus
        private int lastFocusedIndex = -1;

        // Height of one item including its margin, measured from a realized item
        private double itemHeight = 0;

        // Debounces loading while scrolling
        private DispatcherTimer scrollTimer;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the list shown.
        /// </summary>
        public LibraryList Source
        {
            get => source;
            set
            {
                if (source != null)
                    source.ListLoaded -= Source_ListLoaded;

                source = value;
                lastFocusedIndex = -1;
                List.ItemsSource = source?.Items;

                if (source != null)
                    source.ListLoaded += Source_ListLoaded;

                FitToWholeItems();
            }
        }

        /// <summary>
        /// Gets or sets the template of a list row.
        /// </summary>
        public DataTemplate EntryTemplate
        {
            get => List.ItemTemplate;
            set => List.ItemTemplate = value;
        }

        public int Count => source?.Length ?? 0;

        #endregion

        #region Events

        /// <summary>
        /// Raised when an entry is chosen with Enter or a tap.
        /// </summary>
        public event EventHandler<LibraryEntryEventArgs> ItemInvoked;

        /// <summary>
        /// Raised when Left is pressed in the list, to move focus out of it.
        /// </summary>
        public event EventHandler ExitLeft;

        #endregion

        #region Public Methods

        /// <summary>
        /// Focuses the entry at the given 0-based index, scrolling it into view.
        /// </summary>
        /// <param name="center">Scrolls the entry to the middle of the list, rather than
        /// just into view.</param>
        public void FocusIndex(int index, bool center = false)
        {
            if (index < 0 || index >= Count || !IsLoaded)
                return;

            List.SelectedIndex = index;
            List.UpdateLayout();

            var scroller = List.Template.FindName("ListScroller", List) as ScrollViewer;
            if (center && scroller != null)
                // Item based scrolling, so offsets are in items
                scroller.ScrollToVerticalOffset(Math.Max(0, index - Math.Floor(scroller.ViewportHeight / 2)));
            else
                List.ScrollIntoView(source.Items[index]);

            List.UpdateLayout();

            (List.ItemContainerGenerator.ContainerFromIndex(index) as UIElement)?.Focus();
        }

        /// <summary>
        /// Focuses the entry that last had focus, or the first one.
        /// </summary>
        public void FocusLast()
        {
            FocusIndex(Math.Max(0, Math.Min(lastFocusedIndex, Count - 1)));
        }

        /// <summary>
        /// Sizes the list to fit a whole number of items in the available area.
        /// </summary>
        public void FitToWholeItems()
        {
            ListFitter.FitToWholeItems(List, ListArea, ref itemHeight);
        }

        #endregion

        #region Private Methods

        private void RequestVisibleRange()
        {
            var scroller = List.Template?.FindName("ListScroller", List) as ScrollViewer;
            if (source == null || scroller == null)
                return;

            // Item based scrolling, so offsets are in items
            var first = (int)scroller.VerticalOffset + 1;
            var visible = (int)scroller.ViewportHeight;
            source.RequestRange(first + visible / 2);
        }

        private void Invoke(ListBoxItem container)
        {
            var entry = container?.DataContext as LibraryEntry;
            if (entry != null && entry.IsLoaded)
                ItemInvoked?.Invoke(this, new LibraryEntryEventArgs(entry));
        }

        #endregion

        #region Event Handlers

        private void Source_ListLoaded(object sender, EventArgs e)
        {
            lastFocusedIndex = -1;
            FitToWholeItems();
        }

        private void ListArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FitToWholeItems();
        }

        private void List_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (scrollTimer == null)
            {
                scrollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
                scrollTimer.Tick += (s, args) =>
                {
                    scrollTimer.Stop();
                    RequestVisibleRange();
                };
            }

            scrollTimer.Stop();
            scrollTimer.Start();
        }

        private void List_GotFocus(object sender, RoutedEventArgs e)
        {
            var container = e.OriginalSource as ListBoxItem;
            if (container != null)
                lastFocusedIndex = List.ItemContainerGenerator.IndexFromContainer(container);
        }

        private void List_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left)
            {
                ExitLeft?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                var container = Keyboard.FocusedElement as ListBoxItem;
                if (container != null && !e.IsRepeat)
                {
                    ButtonHelper.SetIsEnterPressed(container, true);
                    Invoke(container);

                    // Invoking may move focus away (e.g. to a menu), so the key release
                    // won't reach this item to end the press
                    if (!container.IsKeyboardFocused)
                        ButtonHelper.SetIsEnterPressed(container, false);
                }
                e.Handled = true;
            }
        }

        private void List_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            var container = Keyboard.FocusedElement as ListBoxItem;
            if (container != null)
                ButtonHelper.SetIsEnterPressed(container, false);
            e.Handled = true;
        }

        private void List_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            pressedItem = ItemsControl.ContainerFromElement(List, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (pressedItem != null)
                ButtonHelper.SetIsEnterPressed(pressedItem, true);
        }

        private void List_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (pressedItem == null)
                return;

            ButtonHelper.SetIsEnterPressed(pressedItem, false);

            // Only invoke when released over the same item it was pressed on
            var container = ItemsControl.ContainerFromElement(List, e.OriginalSource as DependencyObject) as ListBoxItem;
            if (container == pressedItem)
                Invoke(container);

            pressedItem = null;
        }

        private void List_MouseLeave(object sender, MouseEventArgs e)
        {
            if (pressedItem == null)
                return;

            ButtonHelper.SetIsEnterPressed(pressedItem, false);
            pressedItem = null;
        }

        #endregion
    }
}
