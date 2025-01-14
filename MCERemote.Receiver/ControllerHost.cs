using MCERemote.Core;
using System;
using System.ServiceModel;

namespace MCERemote.Receiver
{
    internal class ControllerHostSettings
    {
        public int Port { get; set; } = 55091;
    }

    internal class ControllerHost
    {
        public ControllerHost()
        {
            Service = new ControllerService();

            Settings = new ControllerHostSettings();
        }


        #region Fields

        private ControllerHostSettings settings;

        #endregion


        #region Properties

        public ServiceHost Host { get; private set; }

        public ControllerService Service { get; private set; }

        public ControllerHostSettings Settings
        {
            get { return settings; }
            set
            {
                settings = value;
                Initialize();
            }
        }

        public CommunicationState State { get { return Host.State; } }

        #endregion


        #region Private Methods

        private void Initialize()
        {
            Host?.Close();

            Host = new ServiceHost(Service);
            Host.AddServiceEndpoint(
                typeof(IControllerService),
                new NetTcpBinding(SecurityMode.None),
                $"net.tcp://localhost:{settings.Port}/"
            );
        }

        #endregion


        #region Public Methods

        public void Open()
        {
            if (Host == null)
                Initialize();

            Host?.Open();
        }

        public void Close()
        {
            Host?.Close();
        }

        #endregion
    }
}
