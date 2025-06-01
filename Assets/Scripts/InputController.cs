using UnityEngine;
using Unity.Netcode;
using CarBlade.Physics;
using CarBlade.UI;

namespace CarBlade.InputEvent
{
    // 입력 컨트롤러
    public class InputController : NetworkBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private bool useKeyboard = true;
        [SerializeField] private bool useGamepad = true;

        [Header("Keyboard Bindings")]
        [SerializeField] private KeyCode accelerateKey = KeyCode.W;
        [SerializeField] private KeyCode brakeKey = KeyCode.S;
        [SerializeField] private KeyCode steerLeftKey = KeyCode.A;
        [SerializeField] private KeyCode steerRightKey = KeyCode.D;
        [SerializeField] private KeyCode driftKey = KeyCode.Space;
        [SerializeField] private KeyCode boosterKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode hornKey = KeyCode.H;
        [SerializeField] private KeyCode cameraToggleKey = KeyCode.C;

        [Header("Input Smoothing")]
        [SerializeField] private float steeringSmoothTime = 0.1f;
        [SerializeField] private float accelerationSmoothTime = 0.2f;

        // Components
        private VehicleController vehicleController;
        private BoosterSystem boosterSystem;
        private CameraController cameraController;

        // Input values
        private float accelerationInput;
        private float steeringInput;
        private bool isDrifting;

        // Smoothing
        private float currentAcceleration;
        private float accelerationVelocity;
        private float currentSteering;
        private float steeringVelocity;

        // State
        private bool isInputEnabled = true;

        private void Awake()
        {
            vehicleController = GetComponent<VehicleController>();
            boosterSystem = GetComponent<BoosterSystem>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner) return;

            // 카메라 설정
            SetupCamera();

            // UI 설정
            UIManager.Instance?.SetLocalPlayer(gameObject);
        }

        private void Update()
        {
            if (!IsOwner || !isInputEnabled) return;

            // 입력 수집
            CollectInput();

            // 입력 스무딩
            ApplyInputSmoothing();

            // 차량에 입력 적용
            ApplyInputToVehicle();

            // 특수 입력 처리
            HandleSpecialInputs();
        }

        private void CollectInput()
        {
            // 가속/브레이크
            accelerationInput = 0f;
            if (useKeyboard)
            {
                if (UnityEngine.Input.GetKey(accelerateKey))
                    accelerationInput += 1f;
                if (UnityEngine.Input.GetKey(brakeKey))
                    accelerationInput -= 1f;
            }

            if (useGamepad)
            {
                accelerationInput += UnityEngine.Input.GetAxis("Vertical");
            }

            // 조향
            steeringInput = 0f;
            if (useKeyboard)
            {
                if (UnityEngine.Input.GetKey(steerLeftKey))
                    steeringInput -= 1f;
                if (UnityEngine.Input.GetKey(steerRightKey))
                    steeringInput += 1f;
            }

            if (useGamepad)
            {
                steeringInput += UnityEngine.Input.GetAxis("Horizontal");
            }

            // 드리프트
            isDrifting = false;
            if (useKeyboard)
            {
                isDrifting = UnityEngine.Input.GetKey(driftKey);
            }

            if (useGamepad)
            {
                isDrifting = UnityEngine.Input.GetButton("Fire1");
            }

            // 입력 클램핑
            accelerationInput = Mathf.Clamp(accelerationInput, -1f, 1f);
            steeringInput = Mathf.Clamp(steeringInput, -1f, 1f);
        }

        private void ApplyInputSmoothing()
        {
            // 가속 스무딩
            currentAcceleration = Mathf.SmoothDamp(
                currentAcceleration,
                accelerationInput,
                ref accelerationVelocity,
                accelerationSmoothTime
            );

            // 조향 스무딩
            currentSteering = Mathf.SmoothDamp(
                currentSteering,
                steeringInput,
                ref steeringVelocity,
                steeringSmoothTime
            );
        }

        private void ApplyInputToVehicle()
        {
            if (vehicleController == null) return;

            vehicleController.Accelerate(currentAcceleration);
            vehicleController.Steer(currentSteering);
            vehicleController.Drift(isDrifting);
        }

        private void HandleSpecialInputs()
        {
            // 부스터
            if (UnityEngine.Input.GetKeyDown(boosterKey))
            {
                boosterSystem?.ActivateBooster();
            }

            // 경적
            if (UnityEngine.Input.GetKeyDown(hornKey))
            {
                PlayHornServerRpc();
            }

            // 카메라 전환
            if (UnityEngine.Input.GetKeyDown(cameraToggleKey))
            {
                cameraController?.ToggleCamera();
            }
        }

        private void SetupCamera()
        {
            // 카메라 컨트롤러 찾기 또는 생성
            cameraController = FindFirstObjectByType<CameraController>();
            if (cameraController == null)
            {
                GameObject cameraObj = new GameObject("CameraController");
                cameraController = cameraObj.AddComponent<CameraController>();
            }

            cameraController.SetTarget(transform);
        }

        public void EnableInput(bool enable)
        {
            isInputEnabled = enable;

            if (!enable)
            {
                // 입력 비활성화 시 모든 입력 초기화
                accelerationInput = 0f;
                steeringInput = 0f;
                isDrifting = false;
                currentAcceleration = 0f;
                currentSteering = 0f;
            }
        }

        [ServerRpc]
        private void PlayHornServerRpc()
        {
            PlayHornClientRpc();
        }

        [ClientRpc]
        private void PlayHornClientRpc()
        {
            // Audio 팀에서 구현할 경적 재생
            Debug.Log($"Player {OwnerClientId} honked!");
        }

        // 디버그 정보
        public string GetDebugInfo()
        {
            return $"Accel: {currentAcceleration:F2} | Steer: {currentSteering:F2} | Drift: {isDrifting}";
        }

        private void OnGUI()
        {
            if (!IsOwner || !Application.isEditor) return;

            GUI.Label(new Rect(10, 170, 300, 20), GetDebugInfo());
        }
    }
}