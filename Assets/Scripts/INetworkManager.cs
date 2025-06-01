namespace CarBlade.Networking
{
    // 네트워크 매니저 인터페이스
    public interface INetworkManager
    {
        void HostMatch();
        void JoinMatch(string matchId);
        void SyncPlayerState(PlayerData data);
        void RequestRespawn(int playerId);
    }
}