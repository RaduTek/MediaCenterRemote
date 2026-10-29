using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace MCERemote.Controller.Layout
{
    /// <summary>
    /// Interaction logic for LoadingOverlay.xaml
    /// </summary>
    public partial class LoadingOverlay : UserControl
    {
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(
                "IsActive",
                typeof(bool),
                typeof(LoadingOverlay),
                new PropertyMetadata(false, OnIsActiveChanged));

        /// <summary>
        /// Gets or sets whether the overlay is shown, blocking the content below it
        /// and spinning the loading animation.
        /// </summary>
        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        public LoadingOverlay()
        {
            InitializeComponent();

            spin = (Storyboard)FindResource("LoadingOverlay.Spin");
        }

        private readonly Storyboard spin;

        private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var overlay = (LoadingOverlay)d;

            if ((bool)e.NewValue)
            {
                overlay.Visibility = Visibility.Visible;
                overlay.spin.Begin(overlay, true);
            }
            else
            {
                // Stop the animation so it doesn't use CPU while hidden
                overlay.spin.Stop(overlay);
                overlay.Visibility = Visibility.Collapsed;
            }
        }
    }
}
