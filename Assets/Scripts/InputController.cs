using UnityEngine;
using Unity.Netcode;
using CarBlade.Physics;
using CarBlade.UI;

namespace CarBlade.InputEvent
{
    // �Է� ��Ʈ�ѷ�
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

            // ī�޶� ����
            SetupCamera();

            // UI ����
            UIManager.Instance?.SetLocalPlayer(gameObject);
        }

        private void Update()
        {
            if (!IsOwner || !isInputEnabled) return;

            // �Է� ����
            CollectInput();

            // �Է� ������
            ApplyInputSmoothing();

            // ������ �Է� ����
            ApplyInputToVehicle();

            // Ư�� �Է� ó��
            HandleSpecialInputs();
        }

        private void CollectInput()
        {
            // ����/�극��ũ
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

            // ����
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

            // �帮��Ʈ
            isDrifting = false;
            if (useKeyboard)
            {
                isDrifting = UnityEngine.Input.GetKey(driftKey);
            }

            if (useGamepad)
            {
                isDrifting = UnityEngine.Input.GetButton("Fire1");
            }

            // �Է� Ŭ����
            accelerationInput = Mathf.Clamp(accelerationInput, -1f, 1f);
            steeringInput = Mathf.Clamp(steeringInput, -1f, 1f);
        }

        private void ApplyInputSmoothing()
        {
            // ���� ������
            currentAcceleration = Mathf.SmoothDamp(
                currentAcceleration,
                accelerationInput,
                ref accelerationVelocity,
                accelerationSmoothTime
            );

            // ���� ������
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
            // �ν���
            if (UnityEngine.Input.GetKeyDown(boosterKey))
            {
                boosterSystem?.ActivateBooster();
            }

            // ����
            if (UnityEngine.Input.GetKeyDown(hornKey))
            {
                PlayHornServerRpc();
            }

            // ī�޶� ��ȯ
            if (UnityEngine.Input.GetKeyDown(cameraToggleKey))
            {
                cameraController?.ToggleCamera();
            }
        }

        private void SetupCamera()
        {
            // ī�޶� ��Ʈ�ѷ� ã�� �Ǵ� ����
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
                // �Է� ��Ȱ��ȭ �� ��� �Է� �ʱ�ȭ
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
            // Audio ������ ������ ���� ���
            Debug.Log($"Player {OwnerClientId} honked!");
        }

        // ����� ����
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