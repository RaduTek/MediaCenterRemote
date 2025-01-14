using MCERemote.Core;
using System;
using System.Windows;
using System.Windows.Threading;

namespace MCERemote.Controller
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Top = 0;
            Left = 0;
        }

        DispatcherTimer timer;

        Connection connection;
        PlayerStatus _PlayerStatus = PlayerStatus.Unknown;
        PlayerStatus PlayerStatus
        {
            get { return _PlayerStatus; }
            set
            {
                _PlayerStatus = value;
                controlBar.PlayerStatus = value;
                nowPlaying.PlayerStatus = value;
            }
        }

        Views.NowPlaying nowPlaying;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            nowPlaying = new Views.NowPlaying();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;

            titleBar.PlayerActionOccurred += PlayerActionOccurred;
            controlBar.PlayerActionOccurred += PlayerActionOccurred;

            connection = new Connection();
            connection.Opened += Connection_Opened;
            connection.Closed += Connection_Closed;
            connection.Faulted += Connection_Faulted;

            nowPlaying.Connection = connection;
            Views.ConnectingPage connectingPage = new Views.ConnectingPage
            {
                Connection = connection
            };
            viewFrame.Content = connectingPage;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            PlayerStatus = connection.Controller.GetPlayerStatus();
        }

        private void Connection_Opened(object sender, EventArgs e)
        {
            PlayerStatus = connection.Controller.GetPlayerStatus();
            timer.Start();

            viewFrame.Content = nowPlaying;
        }

        private void Connection_Closed(object sender, EventArgs e)
        {
            PlayerStatus = PlayerStatus.Unknown;
            timer.Stop();
        }

        private void Connection_Faulted(object sender, EventArgs e)
        {
            PlayerStatus = PlayerStatus.Unknown;
            timer.Stop();
        }

        private void PlayerActionOccurred(object sender, PlayerActionEventArgs e)
        {
            switch (e.Action)
            {
                case PlayerAction.ClientMinimize:
                    WindowState = WindowState.Minimized;
                    break;
                case PlayerAction.ClientClose:
                    Close();
                    break;

                case PlayerAction.SeekThumb:
                    if (e.Data is double seconds)
                        connection.Controller.Seek(seconds);
                    break;

                default:
                    if (connection.Connected)
                        connection.Controller.SendPlayerAction(e.Action);
                    break;
            }
        }
    }
}
