using MCERemote.Core;
using System;
using System.ServiceModel;
using System.Threading;
using System.Windows.Threading;

namespace MCERemote.Controller
{

    #region Custom Event Args

    public class ConnectionFaultedEventArgs : EventArgs
    {
        public Exception Exception { get; private set; }
        public ConnectionFaultedEventArgs(Exception exception)
        {
            Exception = exception;
        }
    }

    public class ConnectionClosedEventArgs : EventArgs
    {
        public bool Faulted { get; private set; }

        public ConnectionClosedEventArgs(bool faulted)
        {
            Faulted = faulted;
        }
    }

    public class PlayerStatusUpdatedEventArgs : EventArgs
    {
        public PlayerStatus PlayerStatus { get; private set; }
        public PlayerStatusUpdatedEventArgs(PlayerStatus playerStatus)
        {
            PlayerStatus = playerStatus;
        }
    }

    #endregion

    public class Connection
    {
        public Connection()
        {
        }

        #region Fields

        private IClientChannel channel;
        private ChannelFactory<IControllerService> channelFactory;
        private DispatcherTimer timer;

        #endregion

        #region Properties

        public string Host
        {
            get
            {
                return Properties.Settings.Default.ReceiverHost;
            }
            set
            {
                Properties.Settings.Default.ReceiverHost = value;
                Properties.Settings.Default.Save();

                Close();
                Open();
            }
        }

        public IControllerService Controller { get; private set; }

        public bool IsFaulted { get; private set; } = false;

        public bool IsFirstConnection { get; private set; } = true;

        public bool IsOpened => channelFactory != null &&
            channelFactory.State == CommunicationState.Opened &&
            Controller != null;

        public PlayerStatus PlayerStatus { get; private set; }

        #endregion

        #region Events

        public EventHandler<PlayerStatusUpdatedEventArgs> PlayerStatusUpdated;
        public EventHandler<ConnectionFaultedEventArgs> Faulted;
        public EventHandler<ConnectionClosedEventArgs> Closed;
        public EventHandler Opened;
        public EventHandler Opening;

        #endregion

        #region Public Methods

        public void Open()
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    IsFaulted = false;
                    IsFirstConnection = false;

                    App.Current.Dispatcher.Invoke((Action)ResetPlayerStatus);

                    App.Current.Dispatcher.Invoke((Action)(() => Opening?.Invoke(this, EventArgs.Empty)));

                    var endpointAddress = new EndpointAddress("net.tcp://" + Host + ":55091/");
                    var binding = new NetTcpBinding(SecurityMode.None);
                    channelFactory = new ChannelFactory<IControllerService>(binding, endpointAddress);

                    Controller = channelFactory.CreateChannel();
                    Controller?.Announce("Controller is connected");

                    App.Current.Dispatcher.Invoke((Action)(() => InitTimer()));

                    App.Current.Dispatcher.Invoke((Action)(() => Opened?.Invoke(this, EventArgs.Empty)));

                    channel = (IClientChannel)Controller;
                    channel.Faulted += (s, e) =>
                    {
                        RaiseFault(new Exception("Channel faulted."));
                    };
                }
                catch (Exception ex)
                {
                    RaiseFault(ex);
                }
            });
        }

        public void Close(bool faulted = false)
        {
            try
            {
                timer?.Stop();
                timer = null;

                channel?.Close();
                channelFactory?.Close();

                Controller = null;
                channel = null;
                channelFactory = null;
            }
            catch { }

            ResetPlayerStatus();

            Closed?.Invoke(this, new ConnectionClosedEventArgs(faulted));
        }

        #endregion

        #region Private Methods

        private void RaiseFault(Exception ex)
        {
            App.Current.Dispatcher.Invoke((Action)(() =>
            {
                IsFaulted = true;
                Faulted?.Invoke(this, new ConnectionFaultedEventArgs(ex));
                Close(true);
            }));
        }

        private void ResetPlayerStatus()
        {
            PlayerStatus = PlayerStatus.Unknown;
            PlayerStatusUpdated?.Invoke(this, new PlayerStatusUpdatedEventArgs(PlayerStatus));
        }

        private void InitTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (IsOpened)
                {
                    PlayerStatus = Controller.GetPlayerStatus();
                    PlayerStatusUpdated?.Invoke(this, new PlayerStatusUpdatedEventArgs(PlayerStatus));
                }
            }
            catch (Exception ex)
            {
                RaiseFault(ex);
            }
        }

        #endregion
    }
}
