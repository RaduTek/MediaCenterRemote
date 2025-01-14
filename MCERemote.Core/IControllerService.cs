using System;
using System.ServiceModel;
using System.Drawing;

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
        Image GetThumbnail();
    }
}
