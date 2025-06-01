using UnityEngine;
using Unity.Netcode;
using System.Collections;
using CarBlade.Physics;

namespace CarBlade.Environment
{
    // 점프대 컴포넌트
    public class JumpRamp : NetworkBehaviour, IInteractable
    {
        [Header("Jump Settings")]
        [SerializeField] private float jumpForce = 15f;
        [SerializeField] private float boostMultiplier = 1.5f;
        [SerializeField] private Vector3 jumpDirection = Vector3.up;
        [SerializeField] private bool useRampAngle = true;

        [Header("Visual Feedback")]
        [SerializeField] private GameObject activationEffect;
        [SerializeField] private Light rampLight;
        [SerializeField] private Color activeColor = Color.cyan;
        [SerializeField] private Color inactiveColor = Color.gray;

        [Header("Cooldown")]
        [SerializeField] private float cooldownTime = 0.5f;
        private float lastActivationTime;

        private bool isActive = true;
        public bool IsActive => isActive;

        private void Start()
        {
            if (rampLight != null)
            {
                rampLight.color = activeColor;
            }

            // MapManager에 등록
            MapManager.Instance?.RegisterInteractableObject(this);
        }

        public void OnVehicleEnter(GameObject vehicle)
        {
            if (!isActive || Time.time - lastActivationTime < cooldownTime)
                return;

            var rb = vehicle.GetComponent<Rigidbody>();
            var vehicleController = vehicle.GetComponent<VehicleController>();

            if (rb != null && vehicleController != null)
            {
                // 점프 방향 계산
                Vector3 launchDirection = jumpDirection;
                if (useRampAngle)
                {
                    launchDirection = transform.up + transform.forward * 0.5f;
                    launchDirection.Normalize();
                }

                // 차량 속도에 따른 점프력 조정
                float speedFactor = Mathf.Clamp01(vehicleController.CurrentSpeed / 20f);
                float actualJumpForce = jumpForce * (1f + speedFactor * 0.5f);

                // 부스터 사용 중이면 추가 점프력
                var boosterSystem = vehicle.GetComponent<BoosterSystem>();
                if (boosterSystem != null && boosterSystem.IsBoosterActive)
                {
                    actualJumpForce *= boostMultiplier;
                }

                // 점프 적용
                rb.AddForce(launchDirection * actualJumpForce, ForceMode.Impulse);

                // 회전 추가 (스타일 점수용)
                rb.AddTorque(transform.right * actualJumpForce * 0.1f, ForceMode.Impulse);

                lastActivationTime = Time.time;

                if (IsServer)
                {
                    TriggerJumpEffectClientRpc();
                }
            }
        }

        public void OnVehicleExit(GameObject vehicle)
        {
            // 점프대는 Exit 이벤트를 사용하지 않음
        }

        [ClientRpc]
        private void TriggerJumpEffectClientRpc()
        {
            StartCoroutine(JumpEffectCoroutine());
        }

        private IEnumerator JumpEffectCoroutine()
        {
            // 이펙트 재생
            if (activationEffect != null)
            {
                activationEffect.SetActive(true);
            }

            // 라이트 색상 변경
            if (rampLight != null)
            {
                rampLight.color = Color.white;
                rampLight.intensity = 5f;
            }

            yield return new WaitForSeconds(0.2f);

            // 원래 상태로 복구
            if (activationEffect != null)
            {
                activationEffect.SetActive(false);
            }

            if (rampLight != null)
            {
                rampLight.color = activeColor;
                rampLight.intensity = 1f;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Vehicle"))
            {
                OnVehicleEnter(other.gameObject);
            }
        }
    }
}