using System;
using System.Windows;
using System.Windows.Controls;

namespace MCERemote.Controller.Views
{
    /// <summary>
    /// Interaction logic for ConnectingPage.xaml
    /// </summary>
    public partial class ConnectingPage : Page
    {
        public ConnectingPage()
        {
            InitializeComponent();
        }

        public Connection Connection { get; set; }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Connection.Opening += Connection_Opening;
            Connection.Opened += Connection_Opened;
            Connection.Faulted += Connection_Faulted;

            Connection.Open();
        }

        private void Connection_Opened(object sender, EventArgs e)
        {
            statusLabel.Content = "Connected to " + Connection.Host;
        }

        private void Connection_Opening(object sender, EventArgs e)
        {
            statusLabel.Content = "Connecting to " + Connection.Host + "...";
        }

        private void Connection_Faulted(object sender, EventArgs e)
        {
            statusLabel.Content = "Failed to connect to " + Connection.Host;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
