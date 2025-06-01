using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections;
using CarBlade.Core;
using CarBlade.Physics;

namespace CarBlade.Combat
{
    // ���� ü�� �ý���
    public class VehicleHealthSystem : NetworkBehaviour, IHealthSystem
    {
        [Header("Health Settings")]
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth;
        [SerializeField] private bool isInvulnerable = false;
        [SerializeField] private float invulnerabilityDuration = 2f;

        [Header("Flip Detection")]
        [SerializeField] private float flipTimeThreshold = 5f;
        private float currentFlipTime = 0f;
        private bool isFlipped = false;

        // Components
        private VehicleController vehicleController;
        private VehicleData vehicleData;
        private GameManager gameManager;

        // Network
        private NetworkVariable<int> networkHealth = new NetworkVariable<int>(100);
        private NetworkVariable<bool> networkIsDestroyed = new NetworkVariable<bool>(false);

        // Properties
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDestroyed => networkIsDestroyed.Value;
        public bool IsInvulnerable => isInvulnerable;
        public float HealthPercentage => (float)currentHealth / maxHealth;

        // Events
        public event Action<int, int> OnHealthChanged; // current, max
        public event Action<float> OnDamageTaken; // damage amount
        public event Action OnVehicleDestroyed;
        public event Action OnVehicleRespawn;

        private void Awake()
        {
            vehicleController = GetComponent<VehicleController>();
            vehicleData = vehicleController?.VehicleData;

            if (vehicleData != null)
            {
                maxHealth = vehicleData.maxHealth;
                currentHealth = maxHealth;
            }
        }

        private void Start()
        {
            gameManager = GameManager.Instance;

            // ��Ʈ��ũ ���� �ʱ�ȭ
            if (IsServer)
            {
                networkHealth.Value = currentHealth;
                networkIsDestroyed.Value = false;
            }

            // ��Ʈ��ũ ���� ���� �̺�Ʈ
            networkHealth.OnValueChanged += OnNetworkHealthChanged;
            networkIsDestroyed.OnValueChanged += OnNetworkDestroyedChanged;
        }

        private void Update()
        {
            if (!IsOwner) return;

            // ������ üũ
            CheckFlipStatus();

            // ��Ʈ��ũ ����ȭ
            if (IsClient && !IsServer)
            {
                currentHealth = networkHealth.Value;
            }
        }

        private void OnDestroy()
        {
            networkHealth.OnValueChanged -= OnNetworkHealthChanged;
            networkIsDestroyed.OnValueChanged -= OnNetworkDestroyedChanged;
        }

        // IHealthSystem ����
        public bool TakeDamage(int damage, ulong attackerId)
        {
            if (IsDestroyed || isInvulnerable) return false;

            // ���������� ������ ó��
            if (!IsServer) return false;

            int previousHealth = currentHealth;
            currentHealth = Mathf.Max(0, currentHealth - damage);
            networkHealth.Value = currentHealth;

            // ������ �̺�Ʈ
            OnDamageTakenClientRpc(damage);

            // �ı� ó��
            if (currentHealth <= 0)
            {
                DestroyVehicle(attackerId);
                return true;
            }

            return false;
        }

        public void TakeDamage(int damage)
        {
            TakeDamage(damage, 0);
        }

        // ���� �ı�
        private void DestroyVehicle(ulong destroyerId)
        {
            if (!IsServer) return;

            networkIsDestroyed.Value = true;

            // �ı� �̺�Ʈ �˸�
            OnVehicleDestroyedClientRpc();

            // ������ ������
            StartCoroutine(RespawnCoroutine());
        }

        // ������ �ڷ�ƾ
        private IEnumerator RespawnCoroutine()
        {
            yield return new WaitForSeconds(gameManager.RespawnDelay);

            if (IsServer)
            {
                RespawnVehicle();
            }
        }

        // ���� ������
        private void RespawnVehicle()
        {
            // ü�� ȸ��
            currentHealth = maxHealth;
            networkHealth.Value = currentHealth;
            networkIsDestroyed.Value = false;

            // ���� �ð� �ο�
            StartCoroutine(InvulnerabilityCoroutine());

            // ��ġ ���� (Map Team���� �����ϴ� ���� ����Ʈ ���)
            // transform.position = MapManager.Instance.GetRandomSpawnPoint();

            // ������ �̺�Ʈ
            OnVehicleRespawnClientRpc();
        }

        // ���� �ð� �ڷ�ƾ
        private IEnumerator InvulnerabilityCoroutine()
        {
            isInvulnerable = true;
            yield return new WaitForSeconds(gameManager.RespawnInvulnerabilityDuration);
            isInvulnerable = false;
        }

        // ������ üũ
        private void CheckFlipStatus()
        {
            bool currentlyFlipped = vehicleController.IsFlipped();

            if (currentlyFlipped)
            {
                if (!isFlipped)
                {
                    isFlipped = true;
                    currentFlipTime = 0f;
                }

                currentFlipTime += Time.deltaTime;

                // 5�� �̻� ������ ������ �ڵ� �ı�
                if (currentFlipTime >= flipTimeThreshold)
                {
                    if (IsServer)
                    {
                        DestroyVehicle(0); // ����
                    }
                    else
                    {
                        RequestSelfDestructServerRpc();
                    }
                }
            }
            else
            {
                isFlipped = false;
                currentFlipTime = 0f;
            }
        }

        // ü�� ȸ�� (������ ���� ���� ����)
        public void Heal(int amount)
        {
            if (IsDestroyed || !IsServer) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            networkHealth.Value = currentHealth;

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        // ��� �ı� (����׿�)
        [ContextMenu("Destroy Vehicle")]
        public void DestroyImmediate()
        {
            if (IsServer)
            {
                DestroyVehicle(0);
            }
        }

        // ��Ʈ��ũ �̺�Ʈ �ڵ鷯
        private void OnNetworkHealthChanged(int oldValue, int newValue)
        {
            currentHealth = newValue;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void OnNetworkDestroyedChanged(bool oldValue, bool newValue)
        {
            if (newValue && !oldValue)
            {
                OnVehicleDestroyed?.Invoke();
            }
        }

        // ��Ʈ��ũ RPC
        [ServerRpc]
        private void RequestSelfDestructServerRpc()
        {
            DestroyVehicle(0);
        }

        [ClientRpc]
        private void OnDamageTakenClientRpc(float damage)
        {
            OnDamageTaken?.Invoke(damage);
        }

        [ClientRpc]
        private void OnVehicleDestroyedClientRpc()
        {
            OnVehicleDestroyed?.Invoke();

            // ���� ��Ȱ��ȭ
            if (IsOwner)
            {
                vehicleController.enabled = false;
            }
        }

        [ClientRpc]
        private void OnVehicleRespawnClientRpc()
        {
            OnVehicleRespawn?.Invoke();

            // ���� ��Ȱ��ȭ
            if (IsOwner)
            {
                vehicleController.enabled = true;
            }
        }

        // UI�� ����
        public string GetHealthDisplay()
        {
            return $"{currentHealth}/{maxHealth}";
        }

        // ����� ǥ��
        private void OnGUI()
        {
            if (!IsOwner || !Application.isEditor) return;

            GUI.Label(new Rect(10, 130, 300, 20), $"Health: {GetHealthDisplay()} | Invulnerable: {isInvulnerable}");

            if (isFlipped)
            {
                GUI.Label(new Rect(10, 150, 300, 20), $"FLIPPED! Time: {currentFlipTime:F1}/{flipTimeThreshold}");
            }
        }
    }
}