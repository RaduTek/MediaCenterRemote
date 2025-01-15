using MCERemote.Receiver.Controller;
using System;
using System.Windows.Forms;

namespace MCERemote.Receiver
{
    internal class MainContext : ApplicationContext
    {
        private NotifyIcon trayIcon;
        private ServiceHost host;

        public MainContext()
        {
            InitializeComponents();

            host = new ServiceHost();

            Application.ApplicationExit += OnApplicationExit;
            Application.Idle += Application_Idle;
        }

        private void Application_Idle(object sender, EventArgs e)
        {
            if (host.State != System.ServiceModel.CommunicationState.Opened)
            {
                host.Open();
            }
        }

        private void InitializeComponents()
        {
            trayIcon = new NotifyIcon
            {
                Icon = Properties.Resources.Receiver,
                Text = Application.ProductName,
                Visible = true
            };

            var trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add(new MenuItem("Settings", OnShowSettings));
            trayMenu.MenuItems.Add(new MenuItem("-"));
            trayMenu.MenuItems.Add(new MenuItem("Exit", OnExit));

            trayIcon.ContextMenu = trayMenu;

            trayIcon.DoubleClick += OnShowSettings;
        }

        private void OnShowSettings(object sender, EventArgs e)
        {
            //if (statusForm == null || statusForm.IsDisposed)
            //{
            //    statusForm = new StatusForm();
            //    statusForm.Show();
            //}
            //else
            //{
            //    statusForm.BringToFront();
            //}
        }

        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void OnApplicationExit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            trayIcon.Dispose();

            if (host != null)
            {
                host.Close();
                host = null;
            }
        }
    }
}
