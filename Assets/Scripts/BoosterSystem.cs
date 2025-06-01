using UnityEngine;
using Unity.Netcode;
using System;
using CarBlade.Core;

namespace CarBlade.Physics
{
    // 부스터 시스템 구현
    public class BoosterSystem : NetworkBehaviour, IBoosterSystem
    {
        private VehicleController vehicleController;
        private VehicleData vehicleData;

        [Header("Booster State")]
        [SerializeField] private float currentBoostAmount = 0f;
        [SerializeField] private bool isBoosterActive = false;
        private float boosterTimeRemaining = 0f;

        // Constants
        private const float MAX_BOOST = 100f;
        private const float MIN_BOOST_TO_ACTIVATE = 20f;

        // Properties
        public float CurrentBoost => currentBoostAmount;
        public bool IsBoosterActive => isBoosterActive;
        public float BoostPercentage => currentBoostAmount / MAX_BOOST;

        // Events
        public event Action<float> OnBoostChanged;
        public event Action<bool> OnBoosterStateChanged;
        public event Action OnBoosterActivated;
        public event Action OnBoosterDeactivated;

        // Network sync
        private NetworkVariable<float> networkBoostAmount = new NetworkVariable<float>(0f);
        private NetworkVariable<bool> networkBoosterActive = new NetworkVariable<bool>(false);

        public void Initialize(VehicleController controller, VehicleData data)
        {
            vehicleController = controller;
            vehicleData = data;

            // ScoreSystem 이벤트 구독
            var scoreSystem = GameManager.Instance?.GetScoreSystem() as ScoreSystem;
            if (scoreSystem != null)
            {
                scoreSystem.OnOneShotKill += HandleOneShotKill;
            }
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해제
            var scoreSystem = GameManager.Instance?.GetScoreSystem() as ScoreSystem;
            if (scoreSystem != null)
            {
                scoreSystem.OnOneShotKill -= HandleOneShotKill;
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            // 부스터 활성 상태 업데이트
            if (isBoosterActive)
            {
                UpdateBooster();
            }

            // 네트워크 동기화
            if (IsServer)
            {
                networkBoostAmount.Value = currentBoostAmount;
                networkBoosterActive.Value = isBoosterActive;
            }
            else if (IsClient)
            {
                currentBoostAmount = networkBoostAmount.Value;
                isBoosterActive = networkBoosterActive.Value;
            }
        }

        // IBoosterSystem 구현
        public void ChargeBoost(float amount)
        {
            if (isBoosterActive) return; // 부스터 사용 중에는 충전 불가

            float previousBoost = currentBoostAmount;
            currentBoostAmount = Mathf.Clamp(currentBoostAmount + amount, 0f, MAX_BOOST);

            if (currentBoostAmount != previousBoost)
            {
                OnBoostChanged?.Invoke(currentBoostAmount);
            }
        }

        public void ConsumeBoost()
        {
            float consumeAmount = vehicleData.boosterConsumptionRate * Time.deltaTime;
            currentBoostAmount = Mathf.Max(0f, currentBoostAmount - consumeAmount);
            OnBoostChanged?.Invoke(currentBoostAmount);
        }

        // 부스터 활성화
        public void ActivateBooster()
        {
            if (isBoosterActive || currentBoostAmount < MIN_BOOST_TO_ACTIVATE)
            {
                Debug.Log($"Cannot activate booster. Active: {isBoosterActive}, Boost: {currentBoostAmount}");
                return;
            }

            isBoosterActive = true;
            boosterTimeRemaining = vehicleData.boosterDuration;

            OnBoosterStateChanged?.Invoke(true);
            OnBoosterActivated?.Invoke();

            if (IsOwner)
            {
                ActivateBoosterServerRpc();
            }

            Debug.Log("Booster activated!");
        }

        // 부스터 비활성화
        private void DeactivateBooster()
        {
            isBoosterActive = false;
            boosterTimeRemaining = 0f;

            OnBoosterStateChanged?.Invoke(false);
            OnBoosterDeactivated?.Invoke();

            Debug.Log("Booster deactivated!");
        }

        // 부스터 업데이트
        private void UpdateBooster()
        {
            // 부스터 소비
            ConsumeBoost();

            // 시간 감소
            boosterTimeRemaining -= Time.deltaTime;

            // 부스터 종료 조건
            if (currentBoostAmount <= 0f || boosterTimeRemaining <= 0f)
            {
                DeactivateBooster();
            }
        }

        // 원샷 킬 처리
        private void HandleOneShotKill(int playerId)
        {
            // 자신의 원샷 킬인 경우만 처리
            if (IsOwner && playerId.Equals(NetworkManager.Singleton.LocalClientId))
            {
                InstantChargeBoost();
            }
        }

        // 즉시 부스터 100% 충전
        public void InstantChargeBoost()
        {
            currentBoostAmount = MAX_BOOST;
            OnBoostChanged?.Invoke(currentBoostAmount);

            // 특수 이펙트 재생
            Debug.Log("Booster instantly charged to 100%!");
        }

        // 부스터 게이지 리셋
        public void ResetBoost()
        {
            currentBoostAmount = 0f;
            isBoosterActive = false;
            boosterTimeRemaining = 0f;
            OnBoostChanged?.Invoke(currentBoostAmount);
            OnBoosterStateChanged?.Invoke(false);
        }

        // 네트워크 RPC
        [ServerRpc]
        private void ActivateBoosterServerRpc()
        {
            ActivateBoosterClientRpc();
        }

        [ClientRpc]
        private void ActivateBoosterClientRpc()
        {
            if (!IsOwner)
            {
                // 다른 플레이어의 부스터 이펙트 표시
                OnBoosterActivated?.Invoke();
            }
        }

        // 디버그 정보
        public string GetDebugInfo()
        {
            return $"Boost: {currentBoostAmount:F1}% | Active: {isBoosterActive} | Time: {boosterTimeRemaining:F1}s";
        }

        private void OnGUI()
        {
            if (!IsOwner || !Application.isEditor) return;

            // 에디터에서 부스터 상태 표시
            GUI.Label(new Rect(10, 100, 300, 20), GetDebugInfo());
        }
    }
}