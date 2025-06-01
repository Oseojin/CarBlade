using Unity.Collections;
using Unity.Netcode;

namespace CarBlade.Networking
{
    [System.Serializable]
    public struct PlayerData : INetworkSerializable
    {
        public ulong clientId;
        public FixedString64Bytes playerName;
        public int vehicleType;
        public int skinId;
        public bool isReady;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientId);
            serializer.SerializeValue(ref playerName);
            serializer.SerializeValue(ref vehicleType);
            serializer.SerializeValue(ref skinId);
            serializer.SerializeValue(ref isReady);
        }
    }
}