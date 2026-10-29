using System;
using System.Windows;
using System.Windows.Media;

namespace MCERemote.Controller.Controls
{
    /// <summary>
    /// A large tile button shown in the Start page list.
    /// </summary>
    public class StartTile : Button
    {
        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register(
                "Image",
                typeof(ImageSource),
                typeof(StartTile),
                new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the image displayed in the top right corner of the tile.
        /// </summary>
        public ImageSource Image
        {
            get => (ImageSource)GetValue(ImageProperty);
            set => SetValue(ImageProperty, value);
        }

        public StartTile()
        {
            // The style lives in the Start page resources, so resolve it once the tile is in the tree
            SetResourceReference(StyleProperty, "Start.Tile");
        }
    }
}
