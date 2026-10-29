using System;
using System.Windows;
using System.Windows.Media;

namespace MCERemote.Controller.Controls
{
    /// <summary>
    /// A square tile button showing an image (e.g. an album cover), with its caption below.
    /// </summary>
    public class TileButton : Button
    {
        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register(
                "Image",
                typeof(ImageSource),
                typeof(TileButton),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the image shown inside the tile.
        /// </summary>
        public ImageSource Image
        {
            get => (ImageSource)GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }

        public TileButton()
        {
            Style = FindResource("Page.Tile") as Style;
        }
    }
}
