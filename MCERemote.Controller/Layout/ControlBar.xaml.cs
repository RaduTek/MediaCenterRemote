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

        public Connection Connection { get; set; }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (Connection != null)
                Connection.PlayerStatusUpdated += Connection_PlayerStatusUpdated;
        }

        private void Connection_PlayerStatusUpdated(object sender, PlayerStatusUpdatedEventArgs e)
        {
            if (e.PlayerStatus.State == PlayerState.Unknown)
            {
                IsEnabled = false;
                return;
            }

            IsEnabled = true;

            var playing = e.PlayerStatus.State == PlayerState.Playing;

            playPause.IsChecked = playing;
            mute.IsChecked = e.PlayerStatus.Mute;

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

                if (action != PlayerAction.None && Connection.IsOpened)
                {
                    try
                    {
                        Connection.Controller.SendPlayerAction(action);
                    }
                    catch { }
                }
            }
        }

    }
}
