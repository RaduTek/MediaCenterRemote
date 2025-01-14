using System;

namespace MCERemote.Core
{
    public enum PlayerState
    {
        Unknown = 0,
        Playing,
        Paused,
        Stopped,
        Loading,
        Ready,
        Error
    }
}
