using MCERemote.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using WMPLib;
using WMPUtil;

namespace MCERemote.Receiver.Controller
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
    internal class Service : IControllerService
    {
        public Service()
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

        private int TryParseInt(string s, int defaultValue = 0)
        {
            if (int.TryParse(s, out int result))
                return result;
            return defaultValue;
        }

        private bool CheckCurrentMedia()
        {
            if (InitPlayer())
                try
                {
                    if (player.currentMedia != null)
                        return true;
                }
                catch
                { }

            return false;
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
                return PlayerStatus.Unknown;

            try
            {
                // Each property access is a cross-process COM call, so fetch these once
                var playlist = player.currentPlaylist;
                var current = player.currentMedia;
                var queueLength = playlist?.count ?? 0;

                var status = new PlayerStatus()
                {
                    SourceURL = current?.sourceURL,
                    PlaylistName = playlist?.name,
                    State = GetPlayerState(),
                    Volume = player.settings.volume,
                    Mute = player.settings.mute,
                    Shuffle = player.settings.getMode("shuffle"),
                    Repeat = player.settings.getMode("loop"),
                    Position = TimeSpan.FromSeconds(Math.Floor(player.controls.currentPosition)),
                    QueueLength = queueLength,
                    QueuePosition = FindQueuePosition(playlist, current, queueLength)
                };

                return status;
            }
            catch
            {
                return PlayerStatus.Unknown;
            }
        }

        public int GetQueuePosition()
        {
            if (!InitPlayer())
                return 0;

            try
            {
                var playlist = player.currentPlaylist;
                return FindQueuePosition(playlist, player.currentMedia, playlist.count);
            }
            catch
            {
                return 0;
            }
        }

        // 0-based index where the current media was last found, checked first on the next lookup
        private static int lastQueueIndex = 0;

        // Media and queue length of the last full scan that found nothing, so it isn't repeated every poll
        private static string notFoundSourceURL = null;
        private static int notFoundCount = -1;

        /// <summary>
        /// Finds the 1-based position of the current media in the playlist, or 0 if not found.
        /// Checks around the last known position first, so the status poll usually needs only
        /// a few COM calls instead of scanning the whole queue.
        /// </summary>
        private static int FindQueuePosition(IWMPPlaylist playlist, IWMPMedia current, int count)
        {
            if (playlist == null || current == null || count == 0)
                return 0;

            try
            {
                foreach (var offset in new[] { 0, 1, -1, 2, -2 })
                {
                    var i = lastQueueIndex + offset;
                    if (i >= 0 && i < count && current.isIdentical[playlist.Item[i]])
                    {
                        lastQueueIndex = i;
                        return i + 1;
                    }
                }

                var sourceURL = current.sourceURL;
                if (sourceURL == notFoundSourceURL && count == notFoundCount)
                    return 0;

                for (int i = 0; i < count; i++)
                {
                    if (current.isIdentical[playlist.Item[i]])
                    {
                        lastQueueIndex = i;
                        notFoundSourceURL = null;
                        return i + 1;
                    }
                }

                notFoundSourceURL = sourceURL;
                notFoundCount = count;
            }
            catch
            { }

            return 0;
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

                case PlayerAction.ShuffleOn:
                    player.settings.setMode("shuffle", true);
                    break;
                case PlayerAction.ShuffleOff:
                    player.settings.setMode("shuffle", false);
                    break;

                case PlayerAction.RepeatOn:
                    player.settings.setMode("loop", true);
                    break;
                case PlayerAction.RepeatOff:
                    player.settings.setMode("loop", false);
                    break;


                case PlayerAction.VolumeUp:
                    if (Properties.Settings.Default.UseSystemVolume)
                        Helpers.SystemVolume.VolumeUp();
                    else
                        player.settings.volume = Math.Min(player.settings.volume + volumeStep, 100);
                    break;

                case PlayerAction.VolumeDown:
                    if (Properties.Settings.Default.UseSystemVolume)
                        Helpers.SystemVolume.VolumeDown();
                    else
                        player.settings.volume = Math.Max(0, player.settings.volume - volumeStep);
                    break;

                case PlayerAction.Mute:
                    player.settings.mute = true;
                    break;

                case PlayerAction.Unmute:
                    player.settings.mute = false;
                    break;

                case PlayerAction.ToggleMute:
                    if (Properties.Settings.Default.UseSystemVolume)
                        Helpers.SystemVolume.Mute();
                    else
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

        public MediaInfo GetNowPlayingMedia()
        {
            if (!CheckCurrentMedia())
                return new MediaInfo();

            try
            {
                var media = new MediaInfo();

                media.SourceURL = player.currentMedia.sourceURL;
                media.Title = player.currentMedia.name;
                media.Artist = player.currentMedia.getItemInfo("WM/Artist");
                media.Genre = player.currentMedia.getItemInfo("WM/Genre");
                media.Composer = player.currentMedia.getItemInfo("WM/Composer");
                media.Year = TryParseInt(player.currentMedia.getItemInfo("WM/Year"));
                media.TrackNumber = TryParseInt(player.currentMedia.getItemInfo("WM/TrackNumber"), 1);
                media.Album = player.currentMedia.getItemInfo("WM/AlbumTitle");
                media.AlbumArtist = player.currentMedia.getItemInfo("WM/AlbumArtist");
                media.Duration = TimeSpan.FromSeconds(player.currentMedia.duration);

                return media;
            }
            catch
            {
                return new MediaInfo();
            }
        }

        public byte[] GetNowPlayingAlbumArt(bool highQuality = false)
        {
            if (!CheckCurrentMedia())
                return null;

            try
            {
                var path = player.currentMedia.sourceURL;
                var path1 = Path.Combine(Path.GetDirectoryName(path), "Folder.jpg");
                var path2 = Path.Combine(Path.GetDirectoryName(path), "AlbumArtSmall.jpg");

                if (highQuality && File.Exists(path1))
                    return File.ReadAllBytes(path1);

                if (File.Exists(path2))
                    return File.ReadAllBytes(path2);
            }
            catch
            { }

            return null;
        }

        public List<PlaylistItem> GetNowPlayingPlaylist()
        {
            if (!CheckCurrentMedia())
                return new List<PlaylistItem>();

            var playlist = new List<PlaylistItem>();

            try
            {
                // Each property access is a cross-process COM call, so fetch the playlist once
                var currentPlaylist = player.currentPlaylist;
                var count = currentPlaylist.count;

                for (int i = 0; i < count; i++)
                    playlist.Add(ToPlaylistItem(currentPlaylist.Item[i], i + 1));
            }
            catch
            { }

            return playlist;
        }

        public List<PlaylistItem> GetQueueRange(int start, int count)
        {
            var range = new List<PlaylistItem>();

            if (!CheckCurrentMedia())
                return range;

            try
            {
                // Each property access is a cross-process COM call, so fetch the playlist once
                var currentPlaylist = player.currentPlaylist;
                var length = currentPlaylist.count;

                var first = Math.Max(1, start);
                var last = Math.Min(length, start + count - 1);

                for (int index = first; index <= last; index++)
                    range.Add(ToPlaylistItem(currentPlaylist.Item[index - 1], index));
            }
            catch
            { }

            return range;
        }

        private static Library library;

        private static Library GetLibrary()
        {
            return library ?? (library = new Library(player));
        }

        /// <summary>
        /// Replaces the playback queue with a new temporary one holding only the given media,
        /// and plays it. A new playlist is used so a saved playlist is never cleared.
        /// </summary>
        private static void PlayInNewQueue(IWMPMedia media)
        {
            var queue = player.newPlaylist("", "");
            queue.appendItem(media);
            player.currentPlaylist = queue;
            player.controls.playItem(queue.Item[0]);
        }

        private static bool IsSavedPlaylist(IWMPPlaylist playlist)
        {
            var name = playlist.name;
            if (string.IsNullOrEmpty(name))
                return false;

            var saved = player.playlistCollection.getByName(name);
            var count = saved.count;
            for (int i = 0; i < count; i++)
            {
                if (saved.Item(i).isIdentical[playlist])
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Makes a temporary copy of the queue the current one, continuing playback from the
        /// same item and position.
        /// </summary>
        private static IWMPPlaylist CopyToTemporaryQueue(IWMPPlaylist queue)
        {
            var count = queue.count;
            var position = FindQueuePosition(queue, player.currentMedia, count);
            var seconds = player.controls.currentPosition;
            var wasPlaying = player.playState == WMPPlayState.wmppsPlaying;

            var copy = player.newPlaylist("", "");
            for (int i = 0; i < count; i++)
                copy.appendItem(queue.Item[i]);

            player.currentPlaylist = copy;

            if (position > 0)
            {
                player.controls.playItem(copy.Item[position - 1]);
                player.controls.currentPosition = seconds;
                if (!wasPlaying)
                    player.controls.pause();
            }

            return copy;
        }

        private static PlaylistItem ToPlaylistItem(IWMPMedia item, int index)
        {
            return new PlaylistItem
            {
                Index = index,
                Title = item.name,
                Artist = item.getItemInfo("WM/Artist"),
                SourceURL = item.sourceURL,
                Duration = TimeSpan.FromSeconds(item.duration)
            };
        }

        public void PlayQueueItem(int index)
        {
            if (!CheckCurrentMedia())
                return;

            try
            {
                var playlist = player.currentPlaylist;
                if (index < 1 || index > playlist.count)
                    return;

                player.controls.playItem(playlist.Item[index - 1]);
            }
            catch
            { }
        }

        public LibraryRange GetLibraryRange(LibraryQuery query, int start, int count, bool refresh)
        {
            if (!InitPlayer())
                return new LibraryRange();

            try
            {
                return GetLibrary().GetRange(query, start, count, refresh);
            }
            catch
            {
                return new LibraryRange();
            }
        }

        public LibraryInfo GetLibraryInfo(LibraryQuery query)
        {
            if (!InitPlayer())
                return new LibraryInfo();

            try
            {
                return GetLibrary().GetInfo(query);
            }
            catch
            {
                return new LibraryInfo();
            }
        }

        public void PlaySong(string sourceURL)
        {
            if (!InitPlayer())
                return;

            try
            {
                var media = GetLibrary().FindSong(sourceURL);
                if (media != null)
                    PlayInNewQueue(media);
            }
            catch
            { }
        }

        public void QueueSong(string sourceURL)
        {
            if (!InitPlayer())
                return;

            try
            {
                var media = GetLibrary().FindSong(sourceURL);
                if (media == null)
                    return;

                var queue = player.currentPlaylist;
                if (queue == null || queue.count == 0 || player.currentMedia == null)
                {
                    PlayInNewQueue(media);
                    return;
                }

                // Appending to a saved playlist would change it in the library, so continue
                // playback from a temporary copy instead
                if (IsSavedPlaylist(queue))
                    queue = CopyToTemporaryQueue(queue);

                queue.appendItem(media);
            }
            catch
            { }
        }

        #endregion
    }
}
