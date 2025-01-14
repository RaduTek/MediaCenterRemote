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

        public event EventHandler<PlayerActionEventArgs> PlayerActionOccurred;

        private void Button_Clicked(object sender, RoutedEventArgs e)
        {
            if (sender is Control control)
            {
                string tag = control.Tag as string;
                PlayerAction action = PlayerActionConverter.Parse(tag);

                if (action != PlayerAction.None)
                {
                    PlayerActionEventArgs args = new PlayerActionEventArgs(action);
                    PlayerActionOccurred?.Invoke(this, args);
                }
            }
        }
    }
}
