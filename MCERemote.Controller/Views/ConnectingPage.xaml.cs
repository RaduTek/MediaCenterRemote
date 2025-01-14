using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace MCERemote.Controller.Views
{
    /// <summary>
    /// Interaction logic for ConnectingPage.xaml
    /// </summary>
    public partial class ConnectingPage : PageView
    {
        public ConnectingPage()
        {
            InitializeComponent();

            retryButton.Visibility = Visibility.Collapsed;
        }

        private string connectingWord = "Connecting";

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (Connection == null)
                return;

            if (!Connection.IsFirstConnection)
            {
                connectingWord = "Reconnecting";
                if (!Connection.IsOpened)
                {
                    statusLabel.Content = "Disconnected from " + Connection.Host + ".";
                    return;
                }
            }

            Connection.Opening += Connection_Opening;
            Connection.Opened += Connection_Opened;
            Connection.Faulted += Connection_Faulted;
            Connection.Closed += Connection_Closed;

            if (Connection.IsFirstConnection)
                Connection.Open();
        }

        private void Connection_Opened(object sender, EventArgs e)
        {
            statusLabel.Content = "Connected to " + Connection.Host;
        }

        private void Connection_Opening(object sender, EventArgs e)
        {
            statusLabel.Content = connectingWord + " to " + Connection.Host + "...";
        }

        private void Connection_Faulted(object sender, ConnectionFaultedEventArgs e)
        {
            statusLabel.Content = "Failed to connect to " + Connection.Host + ".";
            retryButton.Visibility = Visibility.Visible;
        }

        private void Connection_Closed(object sender, ConnectionClosedEventArgs e)
        {
            statusLabel.Content = "Disconnected from " + Connection.Host + (e.Faulted ? " due to failure." : ".");
            retryButton.Visibility = Visibility.Visible;
        }

        private void retryButton_Click(object sender, RoutedEventArgs e)
        {
            retryButton.Visibility = Visibility.Collapsed;
            Connection.Open();
        }
    }
}
