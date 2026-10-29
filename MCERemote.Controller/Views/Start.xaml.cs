using System;
using System.Windows;
using System.Windows.Input;

namespace MCERemote.Controller.Views
{
    /// <summary>
    /// Represents an item of the Start page.
    /// </summary>
    public enum StartItem
    {
        NowPlaying,
        Music,
        Pictures,
        Videos,
        Extras,
        Settings
    }

    /// <summary>
    /// Provides data for the ItemInvoked event.
    /// </summary>
    public class StartItemEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the item that was invoked.
        /// </summary>
        public StartItem Item { get; private set; }

        public StartItemEventArgs(StartItem item)
        {
            Item = item;
        }
    }

    /// <summary>
    /// Interaction logic for Start.xaml
    /// </summary>
    public partial class Start : Controls.PageView
    {
        public Start()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Event raised when a Start page item is invoked.
        /// </summary>
        public event EventHandler<StartItemEventArgs> ItemInvoked;

        private UIElement lastFocusedTile;

        #region Event Handlers

        private void FocusTile()
        {
            (lastFocusedTile ?? Tiles.Children[0]).Focus();
        }

        private void PageView_Loaded(object sender, RoutedEventArgs e)
        {
            FocusTile();
        }

        private void PageView_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // Tiles can't take focus while a dialog disables the content, so restore it afterwards
            if (IsEnabled && IsLoaded && !IsKeyboardFocusWithin)
                FocusTile();
        }

        private void Tiles_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            int step;
            if (e.Key == Key.Left)
                step = -1;
            else if (e.Key == Key.Right)
                step = 1;
            else
                return;

            e.Handled = true;

            var index = Tiles.Children.IndexOf(Keyboard.FocusedElement as UIElement);
            if (index < 0)
                return;

            // Skip over tiles that can't take focus (e.g. disabled)
            for (index += step; index >= 0 && index < Tiles.Children.Count; index += step)
            {
                if (Tiles.Children[index].Focus())
                    break;
            }
        }

        private void Tiles_GotFocus(object sender, RoutedEventArgs e)
        {
            var tile = e.OriginalSource as Controls.StartTile;
            if (tile == null)
                return;

            lastFocusedTile = tile;

            // Include the list's side margins so the first and last tiles scroll fully into view
            tile.BringIntoView(new Rect(
                -Tiles.Margin.Left, 0,
                tile.ActualWidth + Tiles.Margin.Left + Tiles.Margin.Right, tile.ActualHeight));
        }

        private void Tile_Click(object sender, RoutedEventArgs e)
        {
            var item = (StartItem)Enum.Parse(typeof(StartItem), (string)((FrameworkElement)sender).Tag);
            ItemInvoked?.Invoke(this, new StartItemEventArgs(item));
        }

        #endregion
    }
}
