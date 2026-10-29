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

        public bool Silent { get; private set; }

        public ConnectionClosedEventArgs(bool faulted, bool silent)
        {
            Faulted = faulted;
            Silent = silent;
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

    public class NowPlayingChangedEventArgs : EventArgs
    {
        public MediaInfo NowPlaying { get; private set; }

        public NowPlayingChangedEventArgs(MediaInfo nowPlaying)
        {
            NowPlaying = nowPlaying;
        }
    }

    public class QueueLengthChangedEventArgs : EventArgs
    {
        public int OldLength { get; private set; }

        public int NewLength { get; private set; }

        public QueueLengthChangedEventArgs(int oldLength, int newLength)
        {
            OldLength = oldLength;
            NewLength = newLength;
        }
    }

    #endregion

    public class Connection
    {
        public Connection()
        {
            Queue = new PlaybackQueue(this);
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

        public bool IsFirstConnection { get; set; } = true;

        public bool IsOpened => channelFactory != null &&
            channelFactory.State == CommunicationState.Opened &&
            Controller != null;

        public PlayerStatus PlayerStatus { get; private set; }

        public MediaInfo NowPlaying { get; private set; }

        /// <summary>
        /// Gets the playback queue, which is kept up to date and loaded in the background.
        /// </summary>
        public PlaybackQueue Queue { get; private set; }

        #endregion

        #region Events

        public EventHandler<PlayerStatusUpdatedEventArgs> PlayerStatusUpdated;
        public EventHandler<NowPlayingChangedEventArgs> NowPlayingChanged;
        public EventHandler<QueueLengthChangedEventArgs> QueueLengthChanged;

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
                    var wasFirstConnection = IsFirstConnection;
                    IsFaulted = false;
                    IsFirstConnection = false;

                    App.Current.Dispatcher.Invoke((Action)ResetPlayerStatus);

                    App.Current.Dispatcher.Invoke((Action)(() => Opening?.Invoke(this, EventArgs.Empty)));

                    var endpointAddress = new EndpointAddress("net.tcp://" + Host + ":55091/");
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
                    channelFactory = new ChannelFactory<IControllerService>(binding, endpointAddress);

                    Controller = channelFactory.CreateChannel();

                    if (wasFirstConnection)
                        Thread.Sleep(500);

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

        public void Close(bool faulted = false, bool silent = false)
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

            Closed?.Invoke(this, new ConnectionClosedEventArgs(faulted, silent));
        }

        public void RefreshNow()
        {
            Timer_Tick(null, null);
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
            var oldQueueLength = PlayerStatus?.QueueLength ?? 0;

            NowPlaying = new MediaInfo();
            PlayerStatus = PlayerStatus.Unknown;

            if (oldQueueLength != 0)
                QueueLengthChanged?.Invoke(this, new QueueLengthChangedEventArgs(oldQueueLength, 0));

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
            if (IsOpened)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        var status = Controller.GetPlayerStatus();

                        App.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            var oldQueueLength = PlayerStatus.QueueLength;
                            PlayerStatus = status;

                            // Raised before NowPlayingChanged, so the queue is resized before it looks up the new track
                            if (PlayerStatus.QueueLength != oldQueueLength)
                                QueueLengthChanged?.Invoke(this, new QueueLengthChangedEventArgs(oldQueueLength, PlayerStatus.QueueLength));

                            if (PlayerStatus.SourceURL != NowPlaying.SourceURL)
                            {
                                NowPlaying = Controller.GetNowPlayingMedia();
                                NowPlayingChanged?.Invoke(this, new NowPlayingChangedEventArgs(NowPlaying));
                            }

                            PlayerStatusUpdated?.Invoke(this, new PlayerStatusUpdatedEventArgs(PlayerStatus));
                        }));
                    }
                    catch (Exception ex)
                    {
                        App.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            RaiseFault(ex);
                        }));
                    }
                });
            }
        }

        #endregion
    }
}
