using UnityEngine;
using Unity.Netcode;
using System.Collections;
using CarBlade.Physics;

namespace CarBlade.Environment
{
    // ������ ������Ʈ
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

            // MapManager�� ���
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
                // ���� ���� ���
                Vector3 launchDirection = jumpDirection;
                if (useRampAngle)
                {
                    launchDirection = transform.up + transform.forward * 0.5f;
                    launchDirection.Normalize();
                }

                // ���� �ӵ��� ���� ������ ����
                float speedFactor = Mathf.Clamp01(vehicleController.CurrentSpeed / 20f);
                float actualJumpForce = jumpForce * (1f + speedFactor * 0.5f);

                // �ν��� ��� ���̸� �߰� ������
                var boosterSystem = vehicle.GetComponent<BoosterSystem>();
                if (boosterSystem != null && boosterSystem.IsBoosterActive)
                {
                    actualJumpForce *= boostMultiplier;
                }

                // ���� ����
                rb.AddForce(launchDirection * actualJumpForce, ForceMode.Impulse);

                // ȸ�� �߰� (��Ÿ�� ������)
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
            // ������� Exit �̺�Ʈ�� ������� ����
        }

        [ClientRpc]
        private void TriggerJumpEffectClientRpc()
        {
            StartCoroutine(JumpEffectCoroutine());
        }

        private IEnumerator JumpEffectCoroutine()
        {
            // ����Ʈ ���
            if (activationEffect != null)
            {
                activationEffect.SetActive(true);
            }

            // ����Ʈ ���� ����
            if (rampLight != null)
            {
                rampLight.color = Color.white;
                rampLight.intensity = 5f;
            }

            yield return new WaitForSeconds(0.2f);

            // ���� ���·� ����
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