using System;

namespace MCERemote.Core
{

    public class PlayerStatus
    {
        public string SourceURL { get; set; }
        public string PlaylistName { get; set; }
        public PlayerState State { get; set; }
        public TimeSpan Position { get; set; }
        public int Volume { get; set; }
        public bool Mute { get; set; }
        public bool Shuffle { get; set; }
        public bool Repeat { get; set; }

        /// <summary>
        /// Number of items in the playback queue.
        /// </summary>
        public int QueueLength { get; set; }

        /// <summary>
        /// 1-based position of the current item in the playback queue, or 0 if unknown.
        /// </summary>
        public int QueuePosition { get; set; }

        public static PlayerStatus Unknown { get; private set; } = new PlayerStatus
        {
            QueueLength = 0,
            QueuePosition = 0,
            SourceURL = "",
            PlaylistName = "",
            State = PlayerState.Unknown,
            Position = TimeSpan.Zero,
            Volume = 0,
            Mute = false,
            Shuffle = false, 
            Repeat = false
        };
    }
}
