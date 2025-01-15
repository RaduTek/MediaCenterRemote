using System;

namespace MCERemote.Core
{
    public enum PlayerAction
    {
        None,

        // Transport
        Play,
        Pause,
        PlayPause,
        Stop,
        Next,
        Previous,
        FastForward,
        FastReverse,
        ShuffleOn,
        ShuffleOff,
        RepeatOn,
        RepeatOff,

        // Audio
        VolumeUp,
        VolumeDown,
        Mute,
        Unmute,
        ToggleMute,

        // For Media Center
        Back,
        Help,
        Start,
        ChannelUp,
        ChannelDown,
        Record,

        // For Controller
        SeekThumb,
        ClientSettings,
        ClientStart,
        ClientBack,
        ClientMinimize,
        ClientClose,
    }

    public class PlayerActionEventArgs : EventArgs
    {
        public PlayerAction Action { get; private set; }

        public object Data { get; private set; }

        public PlayerActionEventArgs(PlayerAction action)
        {
            Action = action;
        }

        public PlayerActionEventArgs(PlayerAction action, object data)
        {
            Action = action;
            Data = data;
        }
    }

    public class PlayerActionConverter
    {
        public static PlayerAction Parse(string key)
        {
            try
            {
                return (PlayerAction)Enum.Parse(typeof(PlayerAction), key, true);
            } catch
            {
                return PlayerAction.None;
            }
        }
    }
}
