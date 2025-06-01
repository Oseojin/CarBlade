using UnityEngine;
using Unity.Netcode;
using CarBlade.Core;
using CarBlade.Networking;
using CarBlade.Environment;
using CarBlade.UI;

namespace CarBlade.Integration
{
    // ���� �ʱ�ȭ ������
    public class GameInitializer : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Transform spawnContainer;
        [SerializeField] private GameObject mapPrefab;
        [SerializeField] private GameObject uiPrefab;

        [Header("Game Settings")]
        [SerializeField] private bool autoStartAsHost = false;
        [SerializeField] private bool debugMode = false;

        // �ý��� ����
        private GameManager gameManager;
        private CarBladeNetworkManager networkManager;
        private MapManager mapManager;
        private UIManager uiManager;

        private void Awake()
        {
            // �ʼ� �ý��� �ʱ�ȭ
            InitializeSystems();

            // ����� ��� ����
            if (debugMode)
            {
                EnableDebugMode();
            }
        }

        private void Start()
        {
            // �� �ε�
            LoadMap();

            // UI �ʱ�ȭ
            InitializeUI();

            // �ڵ� ȣ��Ʈ ���� (�׽�Ʈ��)
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
            // GameManager �ʱ�ȭ
            if (GameManager.Instance == null)
            {
                GameObject gmObj = new GameObject("GameManager");
                gmObj.AddComponent<GameManager>();
            }
            gameManager = GameManager.Instance;

            // NetworkManager �ʱ�ȭ
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
            // ���� �޴� ǥ�� (UI ������ ����)
            Debug.Log("Showing main menu...");

            // �ӽ� UI
            if (uiManager != null)
            {
                uiManager.ShowNotification("Press H to Host, J to Join", 10f);
            }
        }

        private void StartAsHost()
        {
            networkManager.HostMatch();

            // ȣ��Ʈ ���� �� ���
            Invoke(nameof(CheckHostStarted), 1f);
        }

        private void CheckHostStarted()
        {
            if (NetworkManager.Singleton.IsHost)
            {
                Debug.Log("Host started successfully");

                // ���� ���¸� Lobby�� ����
                if (gameManager.GetCurrentState() != GameState.Lobby)
                {
                    // GameManager�� �̹� Lobby ���·� �ʱ�ȭ��
                }

                // UI �˸�
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
            // �ӽ� �Է� ó��
            HandleDebugInputs();
        }

        private void HandleDebugInputs()
        {
            // ȣ��Ʈ ����
            if (UnityEngine.Input.GetKeyDown(KeyCode.H))
            {
                if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
                {
                    StartAsHost();
                }
            }

            // Ŭ���̾�Ʈ�� ����
            if (UnityEngine.Input.GetKeyDown(KeyCode.J))
            {
                if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
                {
                    networkManager.JoinMatch("test-room");
                }
            }

            // ��ġ ���� (ȣ��Ʈ��)
            if (UnityEngine.Input.GetKeyDown(KeyCode.Return))
            {
                if (NetworkManager.Singleton.IsHost && gameManager.GetCurrentState() == GameState.Lobby)
                {
                    gameManager.StartMatch();
                }
            }

            // ���� ����
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                networkManager.Disconnect();
            }

            // ����� ���� ���
            if (UnityEngine.Input.GetKeyDown(KeyCode.F1))
            {
                debugMode = !debugMode;
                EnableDebugMode();
            }
        }

        private void EnableDebugMode()
        {
            // ����� ǥ�� Ȱ��ȭ
            Debug.unityLogger.logEnabled = debugMode;

            // FPS ǥ��
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