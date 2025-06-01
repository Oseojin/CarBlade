namespace CarBlade.Networking
{
    // ��Ʈ��ũ �Ŵ��� �������̽�
    public interface INetworkManager
    {
        void HostMatch();
        void JoinMatch(string matchId);
        void SyncPlayerState(PlayerData data);
        void RequestRespawn(int playerId);
    }
}