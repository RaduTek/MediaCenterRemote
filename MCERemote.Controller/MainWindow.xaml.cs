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
        Views.ConnectingPage connectingPage;
        Connection connection;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            connection = new Connection();

            connection.Opened += Connection_Opened;
            connection.Closed += Connection_Closed;

            controlBar.Connection = connection;

            connectingPage = new Views.ConnectingPage();
            OpenPage(connectingPage);
        }

        private void Connection_Opened(object sender, EventArgs e)
        {
            try
            {
                viewFrame.RemoveBackEntry();
            }
            catch { }
            nowPlaying = new Views.NowPlaying();
            OpenPage(nowPlaying);
        }

        private void Connection_Closed(object sender, ConnectionClosedEventArgs e)
        {
            nowPlaying = null;
            
            OpenPage(connectingPage);
        }

        private void OpenPage(Views.PageView page)
        {
            page.Connection = connection;
            viewFrame.Content = page;
        }

        private void titleBar_TitleBarActionOccurred(object sender, Layout.TitleBarActionEventArgs e)
        {
            switch (e.Action)
            {
                case Layout.TitleBarAction.Minimize:
                    WindowState = WindowState.Minimized;
                    break;
                case Layout.TitleBarAction.Close:
                    Close();
                    break;

                case Layout.TitleBarAction.Settings:
                    OpenPage(new Views.Settings());
                    break;
            }
        }
    }
}
