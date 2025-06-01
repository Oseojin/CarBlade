using UnityEngine;
using Unity.Netcode;
using System;

namespace CarBlade.Physics
{
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleController : NetworkBehaviour, IVehicleController
    {
        [Header("Vehicle Setup")]
        [SerializeField] private VehicleData vehicleData;
        [SerializeField] private Transform[] wheels;
        [SerializeField] private Transform vehicleModel;

        [Header("Ground Check")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckDistance = 0.3f;

        // Components
        private Rigidbody rb;
        private BoosterSystem boosterSystem;

        // State
        private float currentSpeed;
        private float currentSteerAngle;
        private bool isDrifting;
        private bool isGrounded;
        private float driftDirection;

        // Input
        private float accelerationInput;
        private float steerInput;

        // Properties
        public float CurrentSpeed => currentSpeed;
        public float AngularVelocity => rb ? rb.angularVelocity.y : 0f;
        public VehicleData VehicleData => vehicleData;
        public bool IsGrounded => isGrounded;
        public bool IsDrifting => isDrifting;

        // Events
        public event Action<float> OnSpeedChanged;
        public event Action<bool> OnDriftStateChanged;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            boosterSystem = GetComponent<BoosterSystem>();

            if (vehicleData != null)
            {
                ApplyVehicleData();
            }
        }

        private void Start()
        {
            // �ν��� �ý��� �ʱ�ȭ
            if (boosterSystem == null)
            {
                boosterSystem = gameObject.AddComponent<BoosterSystem>();
            }
            boosterSystem.Initialize(this, vehicleData);
        }

        private void ApplyVehicleData()
        {
            rb.mass = vehicleData.vehicleMass;
            rb.centerOfMass = vehicleData.centerOfMass;
            rb.linearDamping = vehicleData.airDrag;
            rb.angularDamping = 5f;

            // �ִ� ���ӵ� ����
            rb.maxAngularVelocity = vehicleData.maxAngularVelocity;
        }

        private void Update()
        {
            if (!IsOwner) return;

            // �ӵ� ���
            currentSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            OnSpeedChanged?.Invoke(currentSpeed);

            // ���� üũ
            CheckGroundStatus();

            // �� ȸ�� �ִϸ��̼�
            AnimateWheels();
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;

            ApplyAcceleration();
            ApplySteering();
            ApplyDrift();
            ApplyDownforce();

            // �帮��Ʈ �� �ν��� ����
            if (isDrifting && Mathf.Abs(currentSpeed) > 5f)
            {
                boosterSystem.ChargeBoost(vehicleData.driftBoostChargeRate * Time.fixedDeltaTime);
            }
        }

        // IVehicleController ����
        public void Accelerate(float input)
        {
            accelerationInput = Mathf.Clamp(input, -1f, 1f);
        }

        public void Steer(float input)
        {
            steerInput = Mathf.Clamp(input, -1f, 1f);
        }

        public void Drift(bool drifting)
        {
            if (isDrifting != drifting)
            {
                isDrifting = drifting;
                OnDriftStateChanged?.Invoke(isDrifting);

                if (isDrifting)
                {
                    // �帮��Ʈ ���� �� ���� ȸ�� ���� ����
                    driftDirection = Mathf.Sign(steerInput);
                }
            }
        }

        public void ActivateBooster()
        {
            boosterSystem.ActivateBooster();
        }

        // ���� ����
        private void ApplyAcceleration()
        {
            if (!isGrounded) return;

            float targetSpeed = accelerationInput * vehicleData.maxSpeed;

            // �ν��� ����
            if (boosterSystem.IsBoosterActive)
            {
                targetSpeed *= vehicleData.boosterSpeedMultiplier;
            }

            // ���� �ӵ��� ��ǥ �ӵ��� ����
            float speedDifference = targetSpeed - currentSpeed;

            // ���ӷ� ���
            float accelerationForce = speedDifference * vehicleData.acceleration;

            // �극��ũ
            if (Mathf.Sign(accelerationInput) != Mathf.Sign(currentSpeed) && accelerationInput != 0)
            {
                accelerationForce = accelerationInput * vehicleData.brakeForce;
            }

            // �� ����
            rb.AddForce(transform.forward * accelerationForce * rb.mass, ForceMode.Force);
        }

        // ���� ����
        private void ApplySteering()
        {
            if (!isGrounded || Mathf.Abs(currentSpeed) < 0.1f) return;

            float steerAmount = steerInput * vehicleData.handling;

            // �ӵ��� ���� ���� ����
            float speedFactor = Mathf.Clamp01(1f - (Mathf.Abs(currentSpeed) / vehicleData.maxSpeed) * 0.5f);
            steerAmount *= speedFactor;

            // �帮��Ʈ ���� �� ���� Ư�� ����
            if (isDrifting)
            {
                steerAmount *= 1.5f;
            }

            // ȸ�� ����
            rb.AddTorque(transform.up * steerAmount * Mathf.Sign(currentSpeed), ForceMode.VelocityChange);
        }

        // �帮��Ʈ ���� ����
        private void ApplyDrift()
        {
            if (!isGrounded) return;

            Vector3 forwardVelocity = Vector3.Project(rb.linearVelocity, transform.forward);
            Vector3 rightVelocity = Vector3.Project(rb.linearVelocity, transform.right);

            // �帮��Ʈ ���� ����
            float driftFactor = isDrifting ? vehicleData.driftFactor : 1f;

            // Ⱦ���� �ӵ� ����
            rb.linearVelocity = forwardVelocity + rightVelocity * driftFactor;

            // �帮��Ʈ �� �߰� ȸ����
            if (isDrifting && Mathf.Abs(currentSpeed) > 5f)
            {
                float additionalRotation = driftDirection * 2f * vehicleData.handling;
                rb.AddTorque(transform.up * additionalRotation, ForceMode.VelocityChange);
            }
        }

        // �ٿ����� ����
        private void ApplyDownforce()
        {
            if (!isGrounded) return;

            float speedPercent = Mathf.Abs(currentSpeed) / vehicleData.maxSpeed;
            float downforceAmount = speedPercent * vehicleData.downForce * 1000f;

            rb.AddForce(-transform.up * downforceAmount, ForceMode.Force);
        }

        // ���� üũ
        private void CheckGroundStatus()
        {
            RaycastHit hit;
            isGrounded = UnityEngine.Physics.Raycast(
                transform.position + Vector3.up * 0.1f,
                -transform.up,
                out hit,
                groundCheckDistance + 0.1f,
                groundLayer
            );
        }

        // �� �ִϸ��̼�
        private void AnimateWheels()
        {
            if (wheels == null || wheels.Length == 0) return;

            float rotationSpeed = currentSpeed * 360f / (2f * Mathf.PI * 0.35f); // �� �ݰ� 0.35m ����

            foreach (var wheel in wheels)
            {
                if (wheel != null)
                {
                    wheel.Rotate(rotationSpeed * Time.deltaTime, 0, 0, Space.Self);
                }
            }
        }

        // ���� ������ üũ
        public bool IsFlipped()
        {
            return Vector3.Dot(transform.up, Vector3.up) < 0.1f;
        }

        // ��Ʈ��ũ ����ȭ
        [ServerRpc]
        public void UpdateVehicleStateServerRpc(Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            transform.position = position;
            transform.rotation = rotation;
            rb.linearVelocity = velocity;
        }

        private void OnDrawGizmos()
        {
            // ���� üũ �ð�ȭ
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(
                transform.position + Vector3.up * 0.1f,
                transform.position - Vector3.up * groundCheckDistance
            );
        }
    }
}