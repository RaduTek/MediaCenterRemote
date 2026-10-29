using MCERemote.Core;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MCERemote.Controller.Controls
{
    public class PageView : Page
    {
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(
                "IsLoading",
                typeof(bool),
                typeof(PageView),
                new PropertyMetadata(false));

        public Connection Connection { get; set; }

        public ViewFrame ParentFrame { get; set; }

        /// <summary>
        /// Gets or sets whether the now playing overlay is shown over this page.
        /// </summary>
        public bool ShowNowPlayingOverlay { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the page is loading. While loading, the main window shows
        /// the loading overlay over the page and the page ignores keyboard and mouse input.
        /// </summary>
        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        #region Input Blocking

        // The overlay blocks clicks, but keyboard input still goes to the focused element,
        // so swallow it here before it tunnels down into the page content

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (IsLoading)
                e.Handled = true;

            base.OnPreviewKeyDown(e);
        }

        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            if (IsLoading)
                e.Handled = true;

            base.OnPreviewKeyUp(e);
        }

        protected override void OnPreviewTextInput(TextCompositionEventArgs e)
        {
            if (IsLoading)
                e.Handled = true;

            base.OnPreviewTextInput(e);
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            if (IsLoading)
                e.Handled = true;

            base.OnPreviewMouseDown(e);
        }

        #endregion
    }
}
