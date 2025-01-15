using MCERemote.Core;
using System;
using System.Threading;
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

        public event EventHandler ButtonPressed;

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (Connection != null)
            {
                Connection.Opened += Connection_Opened;
                Connection.Closed += Connection_Closed;
                Connection.PlayerStatusUpdated += Connection_PlayerStatusUpdated;
            }

            IsEnabled = false;
        }

        private void Connection_Opened(object sender, EventArgs e)
        {
            IsEnabled = true;
        }

        private void Connection_Closed(object sender, ConnectionClosedEventArgs e)
        {
            IsEnabled = false;
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
                        ThreadPool.QueueUserWorkItem(_ =>
                        {
                            Connection.Controller.SendPlayerAction(action);

                            if (action == PlayerAction.Next || action == PlayerAction.Previous)
                            {
                                Thread.Sleep(30);
                                Connection.RefreshNow();
                            }

                            App.Current.Dispatcher.Invoke((Action)(() =>
                            {
                                ButtonPressed?.Invoke(this, EventArgs.Empty);
                            }));
                        });
                    }
                    catch { }
                }
            }
        }

    }
}
