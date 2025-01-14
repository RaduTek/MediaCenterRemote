using MCERemote.Core;
using System;
using System.Windows;
using System.Windows.Controls;

namespace MCERemote.Controller.Layout
{
    /// <summary>
    /// Interaction logic for TitleBar.xaml
    /// </summary>
    public partial class TitleBar : UserControl
    {
        public TitleBar()
        {
            InitializeComponent();
        }

        public event EventHandler<TitleBarActionEventArgs> TitleBarActionOccurred;

        private void Button_Clicked(object sender, RoutedEventArgs e)
        {
            if (sender is Control control)
            {
                string tag = control.Tag as string;
                TitleBarAction action = TitleBarActionConverter.Parse(tag);

                if (action != TitleBarAction.None)
                {
                    TitleBarActionEventArgs args = new TitleBarActionEventArgs(action);
                    TitleBarActionOccurred?.Invoke(this, args);
                }
            }
        }
    }
}
