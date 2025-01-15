using System;
using System.Windows;
using System.Windows.Controls;

namespace MCERemote.Controller.Controls
{
    public class LayoutPrimary : ItemsControl
    {
        public LayoutPrimary()
        {
            Style = FindResource("Page.Layout.Primary") as Style;
        }
    }
}
