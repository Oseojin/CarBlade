using UnityEngine;
using Unity.Netcode;
using CarBlade.Core;
using CarBlade.Networking;
using CarBlade.Environment;
using CarBlade.UI;

namespace CarBlade.Integration
{
    // 게임 초기화 관리자
    public class GameInitializer : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Transform spawnContainer;
        [SerializeField] private GameObject mapPrefab;
        [SerializeField] private GameObject uiPrefab;

        [Header("Game Settings")]
        [SerializeField] private bool autoStartAsHost = false;
        [SerializeField] private bool debugMode = false;

        // 시스템 참조
        private GameManager gameManager;
        private CarBladeNetworkManager networkManager;
        private MapManager mapManager;
        private UIManager uiManager;

        private void Awake()
        {
            // 필수 시스템 초기화
            InitializeSystems();

            // 디버그 모드 설정
            if (debugMode)
            {
                EnableDebugMode();
            }
        }

        private void Start()
        {
            // 맵 로드
            LoadMap();

            // UI 초기화
            InitializeUI();

            // 자동 호스트 시작 (테스트용)
            if (autoStartAsHost)
            {
                StartAsHost();
            }
            else
            {
                ShowMainMenu();
            }
        }

        private void InitializeSystems()
        {
            // GameManager 초기화
            if (GameManager.Instance == null)
            {
                GameObject gmObj = new GameObject("GameManager");
                gmObj.AddComponent<GameManager>();
            }
            gameManager = GameManager.Instance;

            // NetworkManager 초기화
            if (CarBladeNetworkManager.Instance == null)
            {
                GameObject nmObj = new GameObject("NetworkManager");
                nmObj.AddComponent<NetworkManager>();
                nmObj.AddComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                nmObj.AddComponent<CarBladeNetworkManager>();
            }
            networkManager = CarBladeNetworkManager.Instance;

            Debug.Log("Core systems initialized");
        }

        private void LoadMap()
        {
            if (mapPrefab != null)
            {
                GameObject mapObj = Instantiate(mapPrefab);
                mapManager = mapObj.GetComponent<MapManager>();

                if (mapManager == null)
                {
                    mapManager = mapObj.AddComponent<MapManager>();
                }

                Debug.Log("Map loaded");
            }
            else
            {
                Debug.LogError("Map prefab not assigned!");
            }
        }

        private void InitializeUI()
        {
            if (uiPrefab != null)
            {
                GameObject uiObj = Instantiate(uiPrefab);
                uiManager = uiObj.GetComponent<UIManager>();

                if (uiManager == null)
                {
                    uiManager = uiObj.AddComponent<UIManager>();
                }

                Debug.Log("UI initialized");
            }
            else
            {
                Debug.LogError("UI prefab not assigned!");
            }
        }

        private void ShowMainMenu()
        {
            // 메인 메뉴 표시 (UI 팀에서 구현)
            Debug.Log("Showing main menu...");

            // 임시 UI
            if (uiManager != null)
            {
                uiManager.ShowNotification("Press H to Host, J to Join", 10f);
            }
        }

        private void StartAsHost()
        {
            networkManager.HostMatch();

            // 호스트 시작 후 대기
            Invoke(nameof(CheckHostStarted), 1f);
        }

        private void CheckHostStarted()
        {
            if (NetworkManager.Singleton.IsHost)
            {
                Debug.Log("Host started successfully");

                // 게임 상태를 Lobby로 설정
                if (gameManager.GetCurrentState() != GameState.Lobby)
                {
                    // GameManager가 이미 Lobby 상태로 초기화됨
                }

                // UI 알림
                if (uiManager != null)
                {
                    uiManager.ShowNotification("Waiting for players...", 3f);
                }
            }
            else
            {
                Debug.LogError("Failed to start as host");
            }
        }

        private void Update()
        {
            // 임시 입력 처리
            HandleDebugInputs();
        }

        private void HandleDebugInputs()
        {
            // 호스트 시작
            if (UnityEngine.Input.GetKeyDown(KeyCode.H))
            {
                if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
                {
                    StartAsHost();
                }
            }

            // 클라이언트로 참가
            if (UnityEngine.Input.GetKeyDown(KeyCode.J))
            {
                if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
                {
                    networkManager.JoinMatch("test-room");
                }
            }

            // 매치 시작 (호스트만)
            if (UnityEngine.Input.GetKeyDown(KeyCode.Return))
            {
                if (NetworkManager.Singleton.IsHost && gameManager.GetCurrentState() == GameState.Lobby)
                {
                    gameManager.StartMatch();
                }
            }

            // 연결 해제
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                networkManager.Disconnect();
            }

            // 디버그 정보 토글
            if (UnityEngine.Input.GetKeyDown(KeyCode.F1))
            {
                debugMode = !debugMode;
                EnableDebugMode();
            }
        }

        private void EnableDebugMode()
        {
            // 디버그 표시 활성화
            Debug.unityLogger.logEnabled = debugMode;

            // FPS 표시
            if (debugMode)
            {
                if (GetComponent<FPSDisplay>() == null)
                {
                    gameObject.AddComponent<FPSDisplay>();
                }
            }
            else
            {
                FPSDisplay fps = GetComponent<FPSDisplay>();
                if (fps != null)
                {
                    Destroy(fps);
                }
            }
        }
    }
}