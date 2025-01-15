using System;

namespace MCERemote.Core
{
    public enum RemoteControlActions
    {
        None,

        Play,
        Pause,
        Stop,
        Record,
        FastReverse,
        FastForward,
        SkipBack,
        SkipForward,

        Up,
        Down,
        Left,
        Right,
        Select,
        Back,
        Info,
        Start,

        VolumeUp,
        VolumeDown,
        Mute,

        ChannelUp,
        ChannelDown,

        LiveTV,
        Guide,
        Recordings,

        DVDMenu,
        Teletext,
        TtxRed,
        TtxGreen,
        TtxYellow,
        TtxBlue,
    }
}
