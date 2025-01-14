using System;

namespace MCERemote.Core
{

    public class PlayerStatus
    {
        public PlayerState State { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public TimeSpan Duration { get; set; }
        public TimeSpan Position { get; set; }
        public int Volume { get; set; }

        public bool Mute { get; set; }

        public static PlayerStatus Unknown { get; private set; } = new PlayerStatus
        {
            State = PlayerState.Unknown,
            Title = null,
            Artist = null,
            Album = null,
            Duration = TimeSpan.Zero,
            Position = TimeSpan.Zero,
            Volume = 0,
            Mute = false
        };
    }
}
