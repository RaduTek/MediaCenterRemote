using MCERemote.Core;
using System;
using System.ServiceModel;
using System.Windows.Threading;

namespace MCERemote.Controller
{
    public class Connection
    {
        private string host;
        private DispatcherTimer timer;
        private ChannelFactory<IControllerService> channelFactory;
        private IClientChannel channel;

        public Connection()
        {
            host = Properties.Settings.Default.ReceiverHost;
        }

        public IControllerService Controller { get; private set; }
        public PlayerStatus PlayerStatus { get; private set; }

        public EventHandler Closed;
        public EventHandler Faulted;
        public EventHandler Opened;
        public EventHandler Opening;

        public bool Connected => channelFactory != null && 
            channelFactory.State == CommunicationState.Opened && 
            Controller != null;

        public string Host
        {
            get
            {
                return host;
            }
            set
            {
                Properties.Settings.Default.ReceiverHost = host = value;
                Properties.Settings.Default.Save();

                Close();
                Open();
            }
        }

        public void Open()
        {
            Opening?.Invoke(this, EventArgs.Empty);

            var endpointAddress = new EndpointAddress("net.tcp://" + host + ":55091/");
            var binding = new NetTcpBinding(SecurityMode.None);
            channelFactory = new ChannelFactory<IControllerService>(binding, endpointAddress);

            Controller = channelFactory.CreateChannel();
            Controller?.Announce("Controller is connected");

            Opened?.Invoke(this, EventArgs.Empty);

            channel = (IClientChannel)Controller;
            channel.Faulted += (sender, e) =>
            {
                Faulted?.Invoke(this, EventArgs.Empty);
                Close();
            };
        }

        public void Close()
        {
            channel?.Close();
            channelFactory?.Close();

            Controller = null;
            channel = null;
            channelFactory = null;

            Closed?.Invoke(this, EventArgs.Empty);
        }

        private void InitTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick; ;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (Connected)
            {
                PlayerStatus = Controller.GetPlayerStatus();
            }
        }
    }
}
