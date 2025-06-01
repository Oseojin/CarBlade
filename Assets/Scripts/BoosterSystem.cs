using UnityEngine;
using Unity.Netcode;
using System;
using CarBlade.Core;

namespace CarBlade.Physics
{
    // �ν��� �ý��� ����
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

            // ScoreSystem �̺�Ʈ ����
            var scoreSystem = GameManager.Instance?.GetScoreSystem() as ScoreSystem;
            if (scoreSystem != null)
            {
                scoreSystem.OnOneShotKill += HandleOneShotKill;
            }
        }

        private void OnDestroy()
        {
            // �̺�Ʈ ���� ����
            var scoreSystem = GameManager.Instance?.GetScoreSystem() as ScoreSystem;
            if (scoreSystem != null)
            {
                scoreSystem.OnOneShotKill -= HandleOneShotKill;
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            // �ν��� Ȱ�� ���� ������Ʈ
            if (isBoosterActive)
            {
                UpdateBooster();
            }

            // ��Ʈ��ũ ����ȭ
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

        // IBoosterSystem ����
        public void ChargeBoost(float amount)
        {
            if (isBoosterActive) return; // �ν��� ��� �߿��� ���� �Ұ�

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

        // �ν��� Ȱ��ȭ
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

        // �ν��� ��Ȱ��ȭ
        private void DeactivateBooster()
        {
            isBoosterActive = false;
            boosterTimeRemaining = 0f;

            OnBoosterStateChanged?.Invoke(false);
            OnBoosterDeactivated?.Invoke();

            Debug.Log("Booster deactivated!");
        }

        // �ν��� ������Ʈ
        private void UpdateBooster()
        {
            // �ν��� �Һ�
            ConsumeBoost();

            // �ð� ����
            boosterTimeRemaining -= Time.deltaTime;

            // �ν��� ���� ����
            if (currentBoostAmount <= 0f || boosterTimeRemaining <= 0f)
            {
                DeactivateBooster();
            }
        }

        // ���� ų ó��
        private void HandleOneShotKill(int playerId)
        {
            // �ڽ��� ���� ų�� ��츸 ó��
            if (IsOwner && playerId.Equals(NetworkManager.Singleton.LocalClientId))
            {
                InstantChargeBoost();
            }
        }

        // ��� �ν��� 100% ����
        public void InstantChargeBoost()
        {
            currentBoostAmount = MAX_BOOST;
            OnBoostChanged?.Invoke(currentBoostAmount);

            // Ư�� ����Ʈ ���
            Debug.Log("Booster instantly charged to 100%!");
        }

        // �ν��� ������ ����
        public void ResetBoost()
        {
            currentBoostAmount = 0f;
            isBoosterActive = false;
            boosterTimeRemaining = 0f;
            OnBoostChanged?.Invoke(currentBoostAmount);
            OnBoosterStateChanged?.Invoke(false);
        }

        // ��Ʈ��ũ RPC
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
                // �ٸ� �÷��̾��� �ν��� ����Ʈ ǥ��
                OnBoosterActivated?.Invoke();
            }
        }

        // ����� ����
        public string GetDebugInfo()
        {
            return $"Boost: {currentBoostAmount:F1}% | Active: {isBoosterActive} | Time: {boosterTimeRemaining:F1}s";
        }

        private void OnGUI()
        {
            if (!IsOwner || !Application.isEditor) return;

            // �����Ϳ��� �ν��� ���� ǥ��
            GUI.Label(new Rect(10, 100, 300, 20), GetDebugInfo());
        }
    }
}