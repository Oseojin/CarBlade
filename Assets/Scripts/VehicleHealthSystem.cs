using UnityEngine;
using Unity.Netcode;
using System;
using System.Collections;
using CarBlade.Core;
using CarBlade.Physics;

namespace CarBlade.Combat
{
    // 차량 체력 시스템
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

            // 네트워크 변수 초기화
            if (IsServer)
            {
                networkHealth.Value = currentHealth;
                networkIsDestroyed.Value = false;
            }

            // 네트워크 변수 변경 이벤트
            networkHealth.OnValueChanged += OnNetworkHealthChanged;
            networkIsDestroyed.OnValueChanged += OnNetworkDestroyedChanged;
        }

        private void Update()
        {
            if (!IsOwner) return;

            // 뒤집힘 체크
            CheckFlipStatus();

            // 네트워크 동기화
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

        // IHealthSystem 구현
        public bool TakeDamage(int damage, ulong attackerId)
        {
            if (IsDestroyed || isInvulnerable) return false;

            // 서버에서만 데미지 처리
            if (!IsServer) return false;

            int previousHealth = currentHealth;
            currentHealth = Mathf.Max(0, currentHealth - damage);
            networkHealth.Value = currentHealth;

            // 데미지 이벤트
            OnDamageTakenClientRpc(damage);

            // 파괴 처리
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

        // 차량 파괴
        private void DestroyVehicle(ulong destroyerId)
        {
            if (!IsServer) return;

            networkIsDestroyed.Value = true;

            // 파괴 이벤트 알림
            OnVehicleDestroyedClientRpc();

            // 리스폰 스케줄
            StartCoroutine(RespawnCoroutine());
        }

        // 리스폰 코루틴
        private IEnumerator RespawnCoroutine()
        {
            yield return new WaitForSeconds(gameManager.RespawnDelay);

            if (IsServer)
            {
                RespawnVehicle();
            }
        }

        // 차량 리스폰
        private void RespawnVehicle()
        {
            // 체력 회복
            currentHealth = maxHealth;
            networkHealth.Value = currentHealth;
            networkIsDestroyed.Value = false;

            // 무적 시간 부여
            StartCoroutine(InvulnerabilityCoroutine());

            // 위치 리셋 (Map Team에서 제공하는 스폰 포인트 사용)
            // transform.position = MapManager.Instance.GetRandomSpawnPoint();

            // 리스폰 이벤트
            OnVehicleRespawnClientRpc();
        }

        // 무적 시간 코루틴
        private IEnumerator InvulnerabilityCoroutine()
        {
            isInvulnerable = true;
            yield return new WaitForSeconds(gameManager.RespawnInvulnerabilityDuration);
            isInvulnerable = false;
        }

        // 뒤집힘 체크
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

                // 5초 이상 뒤집혀 있으면 자동 파괴
                if (currentFlipTime >= flipTimeThreshold)
                {
                    if (IsServer)
                    {
                        DestroyVehicle(0); // 자폭
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

        // 체력 회복 (아이템 등을 위한 예비)
        public void Heal(int amount)
        {
            if (IsDestroyed || !IsServer) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            networkHealth.Value = currentHealth;

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        // 즉시 파괴 (디버그용)
        [ContextMenu("Destroy Vehicle")]
        public void DestroyImmediate()
        {
            if (IsServer)
            {
                DestroyVehicle(0);
            }
        }

        // 네트워크 이벤트 핸들러
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

        // 네트워크 RPC
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

            // 차량 비활성화
            if (IsOwner)
            {
                vehicleController.enabled = false;
            }
        }

        [ClientRpc]
        private void OnVehicleRespawnClientRpc()
        {
            OnVehicleRespawn?.Invoke();

            // 차량 재활성화
            if (IsOwner)
            {
                vehicleController.enabled = true;
            }
        }

        // UI용 정보
        public string GetHealthDisplay()
        {
            return $"{currentHealth}/{maxHealth}";
        }

        // 디버그 표시
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