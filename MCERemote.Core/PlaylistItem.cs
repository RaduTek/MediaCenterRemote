using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCERemote.Core
{
    public class PlaylistItem
    {
        public int Index { get; set; }
        public string SourceURL { get; set; }

        public string Title { get; set; }
        public string Artist { get; set; }

        public TimeSpan Duration { get; set; }
    }
}
