using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Collections;
using System;
using System.Collections.Generic;
using CarBlade.Core;

namespace CarBlade.Networking
{
    // CarBlade ���� ��Ʈ��ũ �Ŵ���
    public class CarBladeNetworkManager : MonoBehaviour, INetworkManager
    {
        private static CarBladeNetworkManager _instance;
        public static CarBladeNetworkManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CarBladeNetworkManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("CarBladeNetworkManager");
                        _instance = go.AddComponent<CarBladeNetworkManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("Network Settings")]
        [SerializeField] private string defaultRoomName = "CarBlade-Room";
        [SerializeField] private int maxPlayers = 20;
        [SerializeField] private ushort port = 7777;

        [Header("Prefabs")]
        [SerializeField] private GameObject[] vehiclePrefabs; // ���� Ÿ�Ժ� ������
        [SerializeField] private GameObject playerInfoPrefab; // �÷��̾� ���� UI ������

        // Network Manager
        private NetworkManager networkManager;
        private UnityTransport transport;

        // �÷��̾� ����
        private Dictionary<ulong, PlayerData> connectedPlayers = new Dictionary<ulong, PlayerData>();
        private NetworkVariable<int> currentPlayerCount = new NetworkVariable<int>(0);

        // �̺�Ʈ
        public event Action<ulong, PlayerData> OnPlayerConnected;
        public event Action<ulong> OnPlayerDisconnected;
        public event Action<bool> OnConnectionStatusChanged;
        public event Action<string> OnMatchCreated;
        public event Action OnMatchStarted;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeNetworkManager();
        }

        private void InitializeNetworkManager()
        {
            networkManager = GetComponent<NetworkManager>();
            if (networkManager == null)
            {
                networkManager = gameObject.AddComponent<NetworkManager>();
            }

            transport = GetComponent<UnityTransport>();
            if (transport == null)
            {
                transport = gameObject.AddComponent<UnityTransport>();
            }

            networkManager.NetworkConfig.NetworkTransport = transport;

            // ��Ʈ��ũ �̺�Ʈ ����
            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            networkManager.OnServerStarted += OnServerStarted;

            // ��Ʈ��ũ ����
            ConfigureNetworkSettings();
        }

        private void ConfigureNetworkSettings()
        {
            var networkConfig = networkManager.NetworkConfig;

            // ���� ���� Ȱ��ȭ
            networkConfig.ConnectionApproval = true;
            networkManager.ConnectionApprovalCallback = ApprovalCheck;

            // �� ���� ��Ȱ��ȭ (Ŀ���� �� �ε� ���)
            networkConfig.EnableSceneManagement = false;

            // ƽ ����Ʈ ���� (60Hz)
            networkConfig.TickRate = 60;

            // �÷��̾� ������ ����
            if (vehiclePrefabs != null && vehiclePrefabs.Length > 0)
            {
                networkConfig.PlayerPrefab = vehiclePrefabs[0]; // �⺻ ����
            }
        }

        // INetworkManager ����
        public void HostMatch()
        {
            if (networkManager.IsClient || networkManager.IsServer)
            {
                Debug.LogWarning("Already connected to a network");
                return;
            }

            transport.SetConnectionData("127.0.0.1", port);

            if (networkManager.StartHost())
            {
                string matchId = GenerateMatchId();
                OnMatchCreated?.Invoke(matchId);
                Debug.Log($"Host started. Match ID: {matchId}");
            }
            else
            {
                Debug.LogError("Failed to start host");
            }
        }

        public void JoinMatch(string matchId)
        {
            if (networkManager.IsClient || networkManager.IsServer)
            {
                Debug.LogWarning("Already connected to a network");
                return;
            }

            // ���� ���������� matchId�� ����Ͽ� ��ġ����ŷ �������� IP �ּҸ� ������
            // ���⼭�� ���� �׽�Ʈ�� ���� �ϵ��ڵ�
            string ipAddress = "127.0.0.1";

            transport.SetConnectionData(ipAddress, port);

            if (networkManager.StartClient())
            {
                Debug.Log($"Joining match: {matchId}");
            }
            else
            {
                Debug.LogError("Failed to start client");
            }
        }

        public void SyncPlayerState(PlayerData data)
        {
            if (!networkManager.IsServer) return;

            if (connectedPlayers.ContainsKey(data.clientId))
            {
                connectedPlayers[data.clientId] = data;
                SyncPlayerDataClientRpc(data);
            }
        }

        public void RequestRespawn(int playerId)
        {
            if (!networkManager.IsServer) return;

            ulong clientId = (ulong)playerId;
            if (networkManager.ConnectedClients.ContainsKey(clientId))
            {
                var playerObject = networkManager.ConnectedClients[clientId].PlayerObject;
                if (playerObject != null)
                {
                    // MapManager���� ���� ����Ʈ ��������
                    // Vector3 spawnPoint = MapManager.Instance.GetRandomSpawnPoint();
                    Vector3 spawnPoint = GetRandomSpawnPoint(); // �ӽ�

                    playerObject.transform.position = spawnPoint;
                    playerObject.transform.rotation = Quaternion.identity;

                    // ü�� �ý��� ����
                    var healthSystem = playerObject.GetComponent<Combat.VehicleHealthSystem>();
                    healthSystem?.Heal(999); // �ִ� ü������ ȸ��
                }
            }
        }

        // ���� ���� üũ
        private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            // �ִ� �÷��̾� �� üũ
            if (networkManager.ConnectedClients.Count >= maxPlayers)
            {
                response.Approved = false;
                response.Reason = "Server is full";
                return;
            }

            // ������ �̹� ���� ������ üũ
            if (GameManager.Instance.GetCurrentState() == GameState.InProgress)
            {
                response.Approved = false;
                response.Reason = "Match already in progress";
                return;
            }

            response.Approved = true;
            response.CreatePlayerObject = true;
            response.PlayerPrefabHash = null; // �⺻ ������ ���

            // ���� ��ġ ����
            response.Position = GetRandomSpawnPoint();
            response.Rotation = Quaternion.identity;
        }

        // Ŭ���̾�Ʈ ���� �̺�Ʈ
        private void OnClientConnected(ulong clientId)
        {
            if (!networkManager.IsServer) return;

            // �� �÷��̾� ������ ����
            PlayerData newPlayer = new PlayerData
            {
                clientId = clientId,
                playerName = $"Player_{clientId}",
                vehicleType = 0, // �⺻ ����
                skinId = 0,
                isReady = false
            };

            connectedPlayers[clientId] = newPlayer;
            currentPlayerCount.Value = connectedPlayers.Count;

            // GameManager�� �÷��̾� ���
            GameManager.Instance.RegisterPlayer((int)clientId, newPlayer.playerName.ToString());

            // ��� Ŭ���̾�Ʈ�� �˸�
            OnPlayerConnected?.Invoke(clientId, newPlayer);
            NotifyPlayerConnectedClientRpc(newPlayer);

            Debug.Log($"Client {clientId} connected. Total players: {currentPlayerCount.Value}");
        }

        // Ŭ���̾�Ʈ ���� ���� �̺�Ʈ
        private void OnClientDisconnected(ulong clientId)
        {
            if (!networkManager.IsServer) return;

            if (connectedPlayers.ContainsKey(clientId))
            {
                connectedPlayers.Remove(clientId);
                currentPlayerCount.Value = connectedPlayers.Count;

                // GameManager���� �÷��̾� ����
                GameManager.Instance.UnregisterPlayer((int)clientId);

                // ��� Ŭ���̾�Ʈ�� �˸�
                OnPlayerDisconnected?.Invoke(clientId);
                NotifyPlayerDisconnectedClientRpc(clientId);

                Debug.Log($"Client {clientId} disconnected. Total players: {currentPlayerCount.Value}");
            }
        }

        // ���� ���� �̺�Ʈ
        private void OnServerStarted()
        {
            Debug.Log("Server started successfully");
            OnConnectionStatusChanged?.Invoke(true);
        }

        // ��ġ ID ����
        private string GenerateMatchId()
        {
            return $"{defaultRoomName}-{System.Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        // �ӽ� ���� ����Ʈ (MapManager ���� ��)
        private Vector3 GetRandomSpawnPoint()
        {
            float radius = 20f;
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float x = Mathf.Sin(angle) * radius;
            float z = Mathf.Cos(angle) * radius;
            return new Vector3(x, 1f, z);
        }

        // ��Ʈ��ũ RPC
        [ClientRpc]
        private void NotifyPlayerConnectedClientRpc(PlayerData playerData)
        {
            if (!networkManager.IsServer)
            {
                OnPlayerConnected?.Invoke(playerData.clientId, playerData);
            }
        }

        [ClientRpc]
        private void NotifyPlayerDisconnectedClientRpc(ulong clientId)
        {
            if (!networkManager.IsServer)
            {
                OnPlayerDisconnected?.Invoke(clientId);
            }
        }

        [ClientRpc]
        private void SyncPlayerDataClientRpc(PlayerData playerData)
        {
            connectedPlayers[playerData.clientId] = playerData;
        }

        // ���� ����
        public void Disconnect()
        {
            if (networkManager.IsHost)
            {
                networkManager.Shutdown();
            }
            else if (networkManager.IsClient)
            {
                networkManager.Shutdown();
            }

            connectedPlayers.Clear();
            OnConnectionStatusChanged?.Invoke(false);
        }

        private void OnDestroy()
        {
            if (networkManager != null)
            {
                networkManager.OnClientConnectedCallback -= OnClientConnected;
                networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
                networkManager.OnServerStarted -= OnServerStarted;
            }
        }
    }
}