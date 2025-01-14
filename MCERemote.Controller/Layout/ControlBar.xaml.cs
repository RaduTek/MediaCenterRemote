using MCERemote.Core;
using System;
using System.Windows;
using System.Windows.Controls;

namespace MCERemote.Controller.Layout
{
    /// <summary>
    /// Interaction logic for ControlBar.xaml
    /// </summary>
    public partial class ControlBar : UserControl
    {
        public ControlBar()
        {
            InitializeComponent();
        }

        public event EventHandler<PlayerActionEventArgs> PlayerActionOccurred;

        private PlayerStatus _PlayerStatus = PlayerStatus.Unknown;
        public PlayerStatus PlayerStatus
        {
            get { return _PlayerStatus; }
            set {
                _PlayerStatus = value;
                UpdateStatus();
            }
        }
        private void UpdateStatus()
        {
            if (PlayerStatus.State == PlayerState.Unknown)
            {
                IsEnabled = false;
                return;
            }

            IsEnabled = true;

            var playing = PlayerStatus.State == PlayerState.Playing;

            playPause.IsChecked = playing;
            mute.IsChecked = PlayerStatus.Mute;

            // To implement for MCE
            record.IsEnabled = false;
            channelUp.IsEnabled = false;
            channelDown.IsEnabled = false;
        }

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
