using MCERemote.Core;
using System;
using System.Drawing;
using System.ServiceModel;
using System.Threading;
using WMPLib;
using WMPUtil;

namespace MCERemote.Receiver
{
    #region Event Args

    internal class PlayerActionInvokedEventArgs : EventArgs
    {
        public PlayerAction Action { get; set; }
    }

    internal class MessageReceivedEventArgs : EventArgs
    {
        public string Message { get; set; }
    }

    #endregion

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    internal class ControllerService : IControllerService
    {
        public ControllerService()
        {
            player = null;
        }

        #region Fields

        private static readonly int volumeStep = 5;
        private static WindowsMediaPlayer player;
        private static string playerError = "";

        #endregion

        #region Events

        public event EventHandler<PlayerActionInvokedEventArgs> PlayerActionInvoked;

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        #endregion

        #region Implementation Methods

        public static bool InitPlayer()
        {
            if (player != null)
                return true;

            try
            {
                if (!WMPRemote.IsWindowMediaPlayerLoaded)
                {
                    playerError = "WMP is not loaded.";
                    return false;
                }

                player = WMPRemote.ActivePlayer;
                return true;
            }
            catch (Exception ex)
            {
                playerError = ex.Message;
                return false;
            }
        }

        public static string GetPlayerError()
        {
            return playerError;
        }

        #endregion

        #region Interface Methods

        public void Announce(String message)
        {
            MessageReceived?.Invoke(this, new MessageReceivedEventArgs { Message = message });
        }

        public PlayerStatus GetPlayerStatus()
        {
            if (!InitPlayer())
            {
                return PlayerStatus.Unknown;
            }

            var status = new PlayerStatus
            {
                State = GetPlayerState(),
                Volume = player.settings.volume,
                Mute = player.settings.mute
            };

            for (int i = 0; i < 10 && GetPlayerState() == PlayerState.Loading; i++)
            {
                // Wait for media to be loaded
                Thread.Sleep(10);
            }

            if (player.currentMedia != null)
            {
                try
                {
                    status.Title = player.currentMedia.name;
                    status.Artist = player.currentMedia.getItemInfo("Artist");
                    status.Album = player.currentMedia.getItemInfo("Album");
                    status.Duration = TimeSpan.FromSeconds(player.currentMedia.duration);
                    status.Position = TimeSpan.FromSeconds(Math.Floor(player.controls.currentPosition));
                }
                catch
                { }
            }

            return status;
        }

        public PlayerState GetPlayerState()
        {
            if (!InitPlayer())
                return PlayerState.Unknown;

            try
            {
                switch (player.playState)
                {
                    case WMPPlayState.wmppsPlaying:
                    case WMPPlayState.wmppsScanForward:
                    case WMPPlayState.wmppsScanReverse:
                        return PlayerState.Playing;

                    case WMPPlayState.wmppsPaused:
                        return PlayerState.Paused;

                    case WMPPlayState.wmppsStopped:
                    case WMPPlayState.wmppsMediaEnded:
                        return PlayerState.Stopped;

                    case WMPPlayState.wmppsReady:
                    case WMPPlayState.wmppsUndefined:
                        return PlayerState.Ready;

                    case WMPPlayState.wmppsBuffering:
                    case WMPPlayState.wmppsTransitioning:
                    case WMPPlayState.wmppsWaiting:
                    case WMPPlayState.wmppsReconnecting:
                        return PlayerState.Loading;

                    case WMPPlayState.wmppsLast:
                    default:
                        return PlayerState.Unknown;
                }
            } 
            catch
            {
                return PlayerState.Unknown;
            }
        }

        public void SetPlayerState(PlayerState state)
        {
            if (player == null)
                return;

            switch (state)
            {
                case PlayerState.Playing:
                    player.controls.play();
                    break;
                case PlayerState.Paused:
                    player.controls.pause();
                    break;
                case PlayerState.Stopped:
                    player.controls.stop();
                    break;
            }
        }

        public void SendPlayerAction(PlayerAction action)
        {
            if (!InitPlayer())
                return;

            switch (action)
            {
                case PlayerAction.Play:
                    SetPlayerState(PlayerState.Playing);
                    break;

                case PlayerAction.Pause:
                    SetPlayerState(PlayerState.Paused);
                    break;

                case PlayerAction.PlayPause:
                    if (GetPlayerState() == PlayerState.Playing)
                        SetPlayerState(PlayerState.Paused);
                    else
                        SetPlayerState(PlayerState.Playing);
                    break;

                case PlayerAction.Stop:
                    SetPlayerState(PlayerState.Stopped);
                    break;

                case PlayerAction.Next:
                    player.controls.next();
                    break;

                case PlayerAction.Previous:
                    player.controls.previous();
                    break;

                case PlayerAction.FastForward:
                    player.controls.fastForward();
                    break;

                case PlayerAction.FastReverse:
                    player.controls.fastReverse();
                    break;

                case PlayerAction.VolumeUp:
                    player.settings.volume = Math.Min(player.settings.volume + volumeStep, 100);
                    break;

                case PlayerAction.VolumeDown:
                    player.settings.volume = Math.Max(0, player.settings.volume - volumeStep);
                    break;

                case PlayerAction.Mute:
                    player.settings.mute = true;
                    break;

                case PlayerAction.Unmute:
                    player.settings.mute = false;
                    break;

                case PlayerAction.ToggleMute:
                    player.settings.mute = !player.settings.mute;
                    break;
            }

            PlayerActionInvoked?.Invoke(this, new PlayerActionInvokedEventArgs { Action = action });
        }

        public PlayerStatus SendPlayerActionWithStatus(PlayerAction action)
        {
            if (!InitPlayer())
                return PlayerStatus.Unknown;

            SendPlayerAction(action);

            return GetPlayerStatus();
        }

        public int GetVolume()
        {
            if (player == null)
                return 0;

            return player.settings.volume;
        }

        public void SetVolume(int volume)
        {
            if (player == null)
                return;

            player.settings.volume = volume;
        }

        public bool GetMute()
        {
            if (player == null)
                return false;

            return player.settings.mute;
        }

        public void SetMute(bool mute)
        {
            if (player == null)
                return;

            player.settings.mute = mute;
        }

        public void Seek(double seconds)
        {
            if (player == null)
                return;

            player.controls.currentPosition = seconds;
        }

        public Image GetThumbnail()
        {
            return null;
        }


        #endregion
    }
}
