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
            // 부스터 시스템 초기화
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

            // 최대 각속도 설정
            rb.maxAngularVelocity = vehicleData.maxAngularVelocity;
        }

        private void Update()
        {
            if (!IsOwner) return;

            // 속도 계산
            currentSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            OnSpeedChanged?.Invoke(currentSpeed);

            // 지면 체크
            CheckGroundStatus();

            // 휠 회전 애니메이션
            AnimateWheels();
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;

            ApplyAcceleration();
            ApplySteering();
            ApplyDrift();
            ApplyDownforce();

            // 드리프트 중 부스터 충전
            if (isDrifting && Mathf.Abs(currentSpeed) > 5f)
            {
                boosterSystem.ChargeBoost(vehicleData.driftBoostChargeRate * Time.fixedDeltaTime);
            }
        }

        // IVehicleController 구현
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
                    // 드리프트 시작 시 현재 회전 방향 저장
                    driftDirection = Mathf.Sign(steerInput);
                }
            }
        }

        public void ActivateBooster()
        {
            boosterSystem.ActivateBooster();
        }

        // 가속 적용
        private void ApplyAcceleration()
        {
            if (!isGrounded) return;

            float targetSpeed = accelerationInput * vehicleData.maxSpeed;

            // 부스터 적용
            if (boosterSystem.IsBoosterActive)
            {
                targetSpeed *= vehicleData.boosterSpeedMultiplier;
            }

            // 현재 속도와 목표 속도의 차이
            float speedDifference = targetSpeed - currentSpeed;

            // 가속력 계산
            float accelerationForce = speedDifference * vehicleData.acceleration;

            // 브레이크
            if (Mathf.Sign(accelerationInput) != Mathf.Sign(currentSpeed) && accelerationInput != 0)
            {
                accelerationForce = accelerationInput * vehicleData.brakeForce;
            }

            // 힘 적용
            rb.AddForce(transform.forward * accelerationForce * rb.mass, ForceMode.Force);
        }

        // 조향 적용
        private void ApplySteering()
        {
            if (!isGrounded || Mathf.Abs(currentSpeed) < 0.1f) return;

            float steerAmount = steerInput * vehicleData.handling;

            // 속도에 따른 조향 감소
            float speedFactor = Mathf.Clamp01(1f - (Mathf.Abs(currentSpeed) / vehicleData.maxSpeed) * 0.5f);
            steerAmount *= speedFactor;

            // 드리프트 중일 때 조향 특성 변경
            if (isDrifting)
            {
                steerAmount *= 1.5f;
            }

            // 회전 적용
            rb.AddTorque(transform.up * steerAmount * Mathf.Sign(currentSpeed), ForceMode.VelocityChange);
        }

        // 드리프트 물리 적용
        private void ApplyDrift()
        {
            if (!isGrounded) return;

            Vector3 forwardVelocity = Vector3.Project(rb.linearVelocity, transform.forward);
            Vector3 rightVelocity = Vector3.Project(rb.linearVelocity, transform.right);

            // 드리프트 팩터 적용
            float driftFactor = isDrifting ? vehicleData.driftFactor : 1f;

            // 횡방향 속도 감소
            rb.linearVelocity = forwardVelocity + rightVelocity * driftFactor;

            // 드리프트 중 추가 회전력
            if (isDrifting && Mathf.Abs(currentSpeed) > 5f)
            {
                float additionalRotation = driftDirection * 2f * vehicleData.handling;
                rb.AddTorque(transform.up * additionalRotation, ForceMode.VelocityChange);
            }
        }

        // 다운포스 적용
        private void ApplyDownforce()
        {
            if (!isGrounded) return;

            float speedPercent = Mathf.Abs(currentSpeed) / vehicleData.maxSpeed;
            float downforceAmount = speedPercent * vehicleData.downForce * 1000f;

            rb.AddForce(-transform.up * downforceAmount, ForceMode.Force);
        }

        // 지면 체크
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

        // 휠 애니메이션
        private void AnimateWheels()
        {
            if (wheels == null || wheels.Length == 0) return;

            float rotationSpeed = currentSpeed * 360f / (2f * Mathf.PI * 0.35f); // 휠 반경 0.35m 가정

            foreach (var wheel in wheels)
            {
                if (wheel != null)
                {
                    wheel.Rotate(rotationSpeed * Time.deltaTime, 0, 0, Space.Self);
                }
            }
        }

        // 차량 뒤집힘 체크
        public bool IsFlipped()
        {
            return Vector3.Dot(transform.up, Vector3.up) < 0.1f;
        }

        // 네트워크 동기화
        [ServerRpc]
        public void UpdateVehicleStateServerRpc(Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            transform.position = position;
            transform.rotation = rotation;
            rb.linearVelocity = velocity;
        }

        private void OnDrawGizmos()
        {
            // 지면 체크 시각화
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawLine(
                transform.position + Vector3.up * 0.1f,
                transform.position - Vector3.up * groundCheckDistance
            );
        }
    }
}