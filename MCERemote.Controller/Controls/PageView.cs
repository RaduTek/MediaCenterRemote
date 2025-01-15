using MCERemote.Core;
using System;
using System.Windows.Controls;

namespace MCERemote.Controller.Controls
{
    public class PageView : Page
    {
        public Connection Connection { get; set; }

        public ViewFrame ParentFrame { get; set; }
    }
}
