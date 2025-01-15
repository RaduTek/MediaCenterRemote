using MCERemote.Core;
using System;
using System.ServiceModel;

namespace MCERemote.Receiver.Controller
{
    internal class HostSettings
    {
        public int Port { get; set; } = 55091;
    }

    internal class ServiceHost
    {
        public ServiceHost()
        {
            Service = new Service();

            Settings = new HostSettings();
        }


        #region Fields

        private HostSettings settings;

        #endregion


        #region Properties

        public System.ServiceModel.ServiceHost Host { get; private set; }

        public Service Service { get; private set; }

        public HostSettings Settings
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
            try { Host?.Close(); }
            catch { }

            var binding = new NetTcpBinding(SecurityMode.None)
            {
                MaxBufferSize = int.MaxValue,
                MaxReceivedMessageSize = int.MaxValue,
                ReaderQuotas =
                {
                    MaxArrayLength = int.MaxValue,
                    MaxStringContentLength = int.MaxValue
                }
            };

            Host = new System.ServiceModel.ServiceHost(Service);
            Host.AddServiceEndpoint(
                typeof(IControllerService),
                binding,
                $"net.tcp://localhost:{settings.Port}/"
            );
        }

        #endregion


        #region Public Methods

        public void Open()
        {
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
