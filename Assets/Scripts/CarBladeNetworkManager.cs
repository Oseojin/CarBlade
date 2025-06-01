using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Collections;
using System;
using System.Collections.Generic;
using CarBlade.Core;

namespace CarBlade.Networking
{
    // CarBlade 전용 네트워크 매니저
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
        [SerializeField] private GameObject[] vehiclePrefabs; // 차량 타입별 프리팹
        [SerializeField] private GameObject playerInfoPrefab; // 플레이어 정보 UI 프리팹

        // Network Manager
        private NetworkManager networkManager;
        private UnityTransport transport;

        // 플레이어 관리
        private Dictionary<ulong, PlayerData> connectedPlayers = new Dictionary<ulong, PlayerData>();
        private NetworkVariable<int> currentPlayerCount = new NetworkVariable<int>(0);

        // 이벤트
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

            // 네트워크 이벤트 구독
            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            networkManager.OnServerStarted += OnServerStarted;

            // 네트워크 설정
            ConfigureNetworkSettings();
        }

        private void ConfigureNetworkSettings()
        {
            var networkConfig = networkManager.NetworkConfig;

            // 연결 승인 활성화
            networkConfig.ConnectionApproval = true;
            networkManager.ConnectionApprovalCallback = ApprovalCheck;

            // 씬 관리 비활성화 (커스텀 씬 로딩 사용)
            networkConfig.EnableSceneManagement = false;

            // 틱 레이트 설정 (60Hz)
            networkConfig.TickRate = 60;

            // 플레이어 프리팹 설정
            if (vehiclePrefabs != null && vehiclePrefabs.Length > 0)
            {
                networkConfig.PlayerPrefab = vehiclePrefabs[0]; // 기본 차량
            }
        }

        // INetworkManager 구현
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

            // 실제 구현에서는 matchId를 사용하여 매치메이킹 서버에서 IP 주소를 가져옴
            // 여기서는 로컬 테스트를 위해 하드코딩
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
                    // MapManager에서 스폰 포인트 가져오기
                    // Vector3 spawnPoint = MapManager.Instance.GetRandomSpawnPoint();
                    Vector3 spawnPoint = GetRandomSpawnPoint(); // 임시

                    playerObject.transform.position = spawnPoint;
                    playerObject.transform.rotation = Quaternion.identity;

                    // 체력 시스템 리셋
                    var healthSystem = playerObject.GetComponent<Combat.VehicleHealthSystem>();
                    healthSystem?.Heal(999); // 최대 체력으로 회복
                }
            }
        }

        // 연결 승인 체크
        private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            // 최대 플레이어 수 체크
            if (networkManager.ConnectedClients.Count >= maxPlayers)
            {
                response.Approved = false;
                response.Reason = "Server is full";
                return;
            }

            // 게임이 이미 진행 중인지 체크
            if (GameManager.Instance.GetCurrentState() == GameState.InProgress)
            {
                response.Approved = false;
                response.Reason = "Match already in progress";
                return;
            }

            response.Approved = true;
            response.CreatePlayerObject = true;
            response.PlayerPrefabHash = null; // 기본 프리팹 사용

            // 스폰 위치 설정
            response.Position = GetRandomSpawnPoint();
            response.Rotation = Quaternion.identity;
        }

        // 클라이언트 연결 이벤트
        private void OnClientConnected(ulong clientId)
        {
            if (!networkManager.IsServer) return;

            // 새 플레이어 데이터 생성
            PlayerData newPlayer = new PlayerData
            {
                clientId = clientId,
                playerName = $"Player_{clientId}",
                vehicleType = 0, // 기본 차량
                skinId = 0,
                isReady = false
            };

            connectedPlayers[clientId] = newPlayer;
            currentPlayerCount.Value = connectedPlayers.Count;

            // GameManager에 플레이어 등록
            GameManager.Instance.RegisterPlayer((int)clientId, newPlayer.playerName.ToString());

            // 모든 클라이언트에 알림
            OnPlayerConnected?.Invoke(clientId, newPlayer);
            NotifyPlayerConnectedClientRpc(newPlayer);

            Debug.Log($"Client {clientId} connected. Total players: {currentPlayerCount.Value}");
        }

        // 클라이언트 연결 해제 이벤트
        private void OnClientDisconnected(ulong clientId)
        {
            if (!networkManager.IsServer) return;

            if (connectedPlayers.ContainsKey(clientId))
            {
                connectedPlayers.Remove(clientId);
                currentPlayerCount.Value = connectedPlayers.Count;

                // GameManager에서 플레이어 제거
                GameManager.Instance.UnregisterPlayer((int)clientId);

                // 모든 클라이언트에 알림
                OnPlayerDisconnected?.Invoke(clientId);
                NotifyPlayerDisconnectedClientRpc(clientId);

                Debug.Log($"Client {clientId} disconnected. Total players: {currentPlayerCount.Value}");
            }
        }

        // 서버 시작 이벤트
        private void OnServerStarted()
        {
            Debug.Log("Server started successfully");
            OnConnectionStatusChanged?.Invoke(true);
        }

        // 매치 ID 생성
        private string GenerateMatchId()
        {
            return $"{defaultRoomName}-{System.Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        // 임시 스폰 포인트 (MapManager 구현 전)
        private Vector3 GetRandomSpawnPoint()
        {
            float radius = 20f;
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float x = Mathf.Sin(angle) * radius;
            float z = Mathf.Cos(angle) * radius;
            return new Vector3(x, 1f, z);
        }

        // 네트워크 RPC
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

        // 연결 해제
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