using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace MCERemote.Controller.Controls
{
    /// <summary>
    /// A scrolling grid of album tiles for a <see cref="LibraryList"/>. WPF 3.5 has no
    /// virtualizing wrap panel, so the grid is a virtualized list of rows of tiles.
    /// Shows whole rows only and loads the entries around the visible ones as the user scrolls.
    /// </summary>
    public partial class AlbumGridView : UserControl
    {
        public AlbumGridView()
        {
            InitializeComponent();

            Rows.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(Tile_Click));
        }

        #region Fields

        private LibraryList source;

        // 0-based index of the entry that last had focus
        private int lastFocusedIndex = -1;

        // Height of one row including its margin, measured from a realized row
        private double rowHeight = 0;

        // Debounces loading while scrolling
        private DispatcherTimer scrollTimer;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the number of tiles per row.
        /// </summary>
        public int Columns { get; set; } = 4;

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

                if (source != null)
                    source.ListLoaded += Source_ListLoaded;

                BuildRows();
            }
        }

        public int Count => source?.Length ?? 0;

        #endregion

        #region Events

        /// <summary>
        /// Raised when a tile is chosen with Enter or a tap.
        /// </summary>
        public event EventHandler<LibraryEntryEventArgs> ItemInvoked;

        /// <summary>
        /// Raised when Left is pressed on the first tile of a row, to move focus out of the grid.
        /// </summary>
        public event EventHandler ExitLeft;

        #endregion

        #region Public Methods

        /// <summary>
        /// Focuses the tile of the entry at the given 0-based index, scrolling its row into view.
        /// </summary>
        public void FocusIndex(int index)
        {
            if (index < 0 || index >= Count || !IsLoaded)
                return;

            var rows = Rows.ItemsSource as List<AlbumRow>;
            var row = index / Columns;
            if (rows == null || row >= rows.Count)
                return;

            Rows.ScrollIntoView(rows[row]);
            Rows.UpdateLayout();

            var container = Rows.ItemContainerGenerator.ContainerFromIndex(row) as DependencyObject;
            var tile = FindTile(container, source.Items[index]);
            tile?.Focus();
        }

        /// <summary>
        /// Focuses the tile that last had focus, or the first one.
        /// </summary>
        public void FocusLast()
        {
            FocusIndex(Math.Max(0, Math.Min(lastFocusedIndex, Count - 1)));
        }

        /// <summary>
        /// Sizes the grid to fit a whole number of rows in the available area.
        /// </summary>
        public void FitToWholeRows()
        {
            ListFitter.FitToWholeItems(Rows, GridArea, ref rowHeight);
        }

        #endregion

        #region Private Methods

        private void BuildRows()
        {
            lastFocusedIndex = -1;

            var rows = new List<AlbumRow>();
            if (source != null)
            {
                for (int start = 0; start < source.Length; start += Columns)
                    rows.Add(new AlbumRow(source.Items, start, Math.Min(Columns, source.Length - start)));
            }

            Rows.ItemsSource = rows;
            FitToWholeRows();
        }

        private void RequestVisibleRange()
        {
            var scroller = Rows.Template?.FindName("ListScroller", Rows) as ScrollViewer;
            if (source == null || scroller == null)
                return;

            // Item based scrolling, so offsets are in rows
            var middleRow = (int)scroller.VerticalOffset + (int)scroller.ViewportHeight / 2;
            source.RequestRange(middleRow * Columns + Columns / 2 + 1);
        }

        private int IndexOf(DependencyObject element)
        {
            var entry = (element as FrameworkElement)?.DataContext as LibraryEntry;
            return entry != null ? entry.Index - 1 : -1;
        }

        private static TileButton FindTile(DependencyObject parent, LibraryEntry entry)
        {
            if (parent == null)
                return null;

            var tile = parent as TileButton;
            if (tile != null && tile.DataContext == entry)
                return tile;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var found = FindTile(VisualTreeHelper.GetChild(parent, i), entry);
                if (found != null)
                    return found;
            }

            return null;
        }

        #endregion

        #region Event Handlers

        private void Source_ListLoaded(object sender, EventArgs e)
        {
            BuildRows();
        }

        private void GridArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            FitToWholeRows();
        }

        private void Rows_ScrollChanged(object sender, ScrollChangedEventArgs e)
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

        private void Rows_GotFocus(object sender, RoutedEventArgs e)
        {
            var index = IndexOf(e.OriginalSource as DependencyObject);
            if (index >= 0)
                lastFocusedIndex = index;
        }

        private void Rows_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var index = IndexOf(Keyboard.FocusedElement as DependencyObject);
            if (index < 0)
                return;

            var column = index % Columns;
            var target = index;

            switch (e.Key)
            {
                case Key.Left:
                    if (column == 0)
                    {
                        ExitLeft?.Invoke(this, EventArgs.Empty);
                        e.Handled = true;
                        return;
                    }
                    target = index - 1;
                    break;

                case Key.Right:
                    target = index + 1;
                    break;

                case Key.Up:
                    target = index - Columns;
                    break;

                case Key.Down:
                    // Move to the last tile if the next row is shorter
                    if (index / Columns < (Count - 1) / Columns)
                        target = Math.Min(index + Columns, Count - 1);
                    break;

                default:
                    return;
            }

            e.Handled = true;

            if (target >= 0 && target < Count && target != index)
                FocusIndex(target);
        }

        private void Tile_Click(object sender, RoutedEventArgs e)
        {
            var entry = (e.OriginalSource as FrameworkElement)?.DataContext as LibraryEntry;
            if (entry != null && entry.IsLoaded)
                ItemInvoked?.Invoke(this, new LibraryEntryEventArgs(entry));
        }

        #endregion

        /// <summary>
        /// A row of the grid, holding the entries at a range of positions of the list.
        /// </summary>
        public class AlbumRow
        {
            public AlbumRow(IList<LibraryEntry> items, int start, int count)
            {
                var entries = new List<LibraryEntry>(count);
                for (int i = start; i < start + count; i++)
                    entries.Add(items[i]);
                Entries = entries;
            }

            public List<LibraryEntry> Entries { get; private set; }
        }
    }
}
