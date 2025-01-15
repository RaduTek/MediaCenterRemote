using System;
using System.Windows;
using System.Windows.Controls;

namespace MCERemote.Controller.Controls
{
    public enum LayoutTitleVariant
    {
        Primary,
        Secondary
    }

    public class LayoutTitle : TextBlock
    {
        public LayoutTitle()
        {
            Variant = LayoutTitleVariant.Primary;
        }

        private LayoutTitleVariant _Variant;
        public LayoutTitleVariant Variant
        {
            get => _Variant;
            set
            {
                _Variant = value;
                switch (value)
                {
                    case LayoutTitleVariant.Primary:
                        Style = FindResource("Page.Layout.Title.Primary") as Style;
                        break;
                    case LayoutTitleVariant.Secondary:
                        Style = FindResource("Page.Layout.Title.Secondary") as Style;
                        break;
                }
            }
        }
    }
}
