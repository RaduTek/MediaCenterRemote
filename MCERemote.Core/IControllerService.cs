using System;
using System.Collections.Generic;
using System.ServiceModel;

namespace MCERemote.Core
{
    [ServiceContract]
    public interface IControllerService
    {
        [OperationContract]
        void Announce(string message);

        [OperationContract]
        PlayerStatus GetPlayerStatus();

        [OperationContract]
        PlayerState GetPlayerState();

        [OperationContract]
        void SetPlayerState(PlayerState state);

        [OperationContract]
        void SendPlayerAction(PlayerAction action);

        [OperationContract]
        int GetVolume();

        [OperationContract]
        void SetVolume(int volume);

        [OperationContract]
        bool GetMute();

        [OperationContract]
        void SetMute(bool mute);

        [OperationContract]
        void Seek(double seconds);

        [OperationContract]
        MediaInfo GetNowPlayingMedia();

        [OperationContract]
        byte[] GetNowPlayingAlbumArt(bool highQuality = false);

        [OperationContract]
        int GetQueuePosition();

        [OperationContract]
        List<PlaylistItem> GetNowPlayingPlaylist();

        [OperationContract]
        List<PlaylistItem> GetQueueRange(int start, int count);

        [OperationContract]
        void PlayQueueItem(int index);
    }
}
