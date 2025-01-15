using System;
using System.Windows;

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

        Views.NowPlaying nowPlaying;
        Connection connection;
        Controls.Dialog connectingDialog;

        #region Events

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            connectingDialog = new Controls.Dialog()
            {
                Title = "CONNECTING",
                Text = "Connecting to the receiver...",
                VerticalAlignment = VerticalAlignment.Top,
            };

            Connection_Init();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            connection.Close(silent: true);
        }

        private void TitleBar_TitleBarActionOccurred(object sender, Layout.TitleBarActionEventArgs e)
        {
            switch (e.Action)
            {
                case Layout.TitleBarAction.Minimize:
                    WindowState = WindowState.Minimized;
                    break;
                case Layout.TitleBarAction.Close:
                    Close();
                    break;

                case Layout.TitleBarAction.Back:
                    ViewFrame.GoBack();
                    break;

                case Layout.TitleBarAction.Settings:
                    ViewFrame.OpenPage(new Views.Settings());
                    break;
            }
        }

        private void ViewFrame_HistoryChanged(object sender, EventArgs e)
        {
            TitleBar.CanGoBack = ViewFrame.CanGoBack;
        }

        private void DialogHost_Opened(object sender, Controls.DialogHostEventArgs e)
        {
            ContentGrid.IsEnabled = false;
        }

        private void DialogHost_Closed(object sender, Controls.DialogHostEventArgs e)
        {
            ContentGrid.IsEnabled = true;
        }

        #endregion

        #region Connection

        private void Connection_Init()
        {
            connection = new Connection();

            connection.Opening += Connection_Opening;
            connection.Opened += Connection_Opened;
            connection.Closed += Connection_Closed;

            ControlBar.Connection = connection;
            ViewFrame.Connection = connection;

            connection.Open();
        }

        private void Connection_Opening(object sender, EventArgs e)
        {
            DialogHost.ShowDialog(connectingDialog);
        }

        private void Connection_Opened(object sender, EventArgs e)
        {
            connectingDialog.Close();
            nowPlaying = new Views.NowPlaying();
            ViewFrame.OpenPage(nowPlaying, true);
        }

        private void Connection_Closed(object sender, ConnectionClosedEventArgs e)
        {
            if (e.Silent)
                return;

            connectingDialog.Close();

            var dialog = new Controls.Dialog()
            {
                Title = "CONNECTION LOST",
                Text = "The connection to the receiver has been lost. Would you like to reconnect?",
                Button1Text = "Reconnect",
                Button2Text = "Settings",
            };

            dialog.Closed += ConnectionLostDialog_Closed;

            DialogHost.ShowDialog(dialog);
        }

        private void ConnectionLostDialog_Closed(object sender, Controls.DialogClosedEventArgs e)
        {
            if (e.Button == Controls.DialogButtonType.Button1)
                connection.Open();
            else if (e.Button == Controls.DialogButtonType.Button2)
                ViewFrame.OpenPage(new Views.Settings());
        }

        #endregion

    }
}
