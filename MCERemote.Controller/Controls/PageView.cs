using MCERemote.Core;
using System;
using System.Windows.Controls;

namespace MCERemote.Controller.Controls
{
    public class PageView : Page
    {
        public Connection Connection { get; set; }

        public ViewFrame ParentFrame { get; set; }

        /// <summary>
        /// Gets or sets whether the now playing overlay is shown over this page.
        /// </summary>
        public bool ShowNowPlayingOverlay { get; set; } = true;
    }
}
