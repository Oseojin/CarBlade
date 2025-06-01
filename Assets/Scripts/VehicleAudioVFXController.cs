using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using CarBlade.Physics;
using CarBlade.Combat;

namespace CarBlade.AudioVFX
{
    public class VehicleAudioVFXController : NetworkBehaviour
    {
        [Header("Components")]
        private VehicleController vehicleController;
        private BoosterSystem boosterSystem;
        private BladeCombatSystem combatSystem;
        private VehicleHealthSystem healthSystem;

        [Header("Effects")]
        private GameObject currentBoosterEffect;
        private GameObject currentDriftSmoke;
        private AudioSource engineAudioSource;

        [Header("State")]
        private bool isDrifting = false;
        private bool isBoosterActive = false;

        private void Awake()
        {
            vehicleController = GetComponent<VehicleController>();
            boosterSystem = GetComponent<BoosterSystem>();
            combatSystem = GetComponent<BladeCombatSystem>();
            healthSystem = GetComponent<VehicleHealthSystem>();
        }

        public override void OnNetworkSpawn()
        {
            // 이벤트 구독
            if (vehicleController != null)
            {
                vehicleController.OnDriftStateChanged += OnDriftStateChanged;
            }

            if (boosterSystem != null)
            {
                boosterSystem.OnBoosterActivated += OnBoosterActivated;
                boosterSystem.OnBoosterDeactivated += OnBoosterDeactivated;
            }

            if (combatSystem != null)
            {
                combatSystem.OnBladeClash += OnBladeClash;
            }

            if (healthSystem != null)
            {
                healthSystem.OnVehicleDestroyed += OnVehicleDestroyed;
            }

            // 로컬 플레이어인 경우 엔진 사운드 설정
            if (IsOwner)
            {
                SetupEngineSound();
            }
        }

        public override void OnNetworkDespawn()
        {
            // 이벤트 구독 해제
            if (vehicleController != null)
            {
                vehicleController.OnDriftStateChanged -= OnDriftStateChanged;
            }

            if (boosterSystem != null)
            {
                boosterSystem.OnBoosterActivated -= OnBoosterActivated;
                boosterSystem.OnBoosterDeactivated -= OnBoosterDeactivated;
            }

            if (combatSystem != null)
            {
                combatSystem.OnBladeClash -= OnBladeClash;
            }

            if (healthSystem != null)
            {
                healthSystem.OnVehicleDestroyed -= OnVehicleDestroyed;
            }

            // 이펙트 정리
            CleanupEffects();
        }

        private void Update()
        {
            if (!IsOwner) return;

            // 엔진 사운드 업데이트
            UpdateEngineSound();
        }

        private void SetupEngineSound()
        {
            // 로컬 플레이어만 엔진 사운드 재생
            GameObject engineObj = new GameObject("EngineSound");
            engineObj.transform.SetParent(transform);
            engineAudioSource = engineObj.AddComponent<AudioSource>();
            engineAudioSource.spatialBlend = 0.3f; // 약간의 3D
            engineAudioSource.loop = true;
            engineAudioSource.volume = 0.7f;
        }

        private void UpdateEngineSound()
        {
            if (AudioManager.Instance != null && vehicleController != null)
            {
                float normalizedSpeed = Mathf.Abs(vehicleController.CurrentSpeed) / vehicleController.VehicleData.maxSpeed;
                AudioManager.Instance.PlayEngineSound(normalizedSpeed);
            }
        }

        private void OnDriftStateChanged(bool drifting)
        {
            isDrifting = drifting;

            if (drifting)
            {
                // 드리프트 시작
                if (currentDriftSmoke == null && VFXManager.Instance != null)
                {
                    currentDriftSmoke = VFXManager.Instance.AttachDriftSmoke(transform);
                }

                if (IsServer)
                {
                    PlayDriftSoundClientRpc();
                }
            }
            else
            {
                // 드리프트 종료
                if (currentDriftSmoke != null && VFXManager.Instance != null)
                {
                    VFXManager.Instance.ReturnEffect(currentDriftSmoke, "DriftSmoke");
                    currentDriftSmoke = null;
                }
            }
        }

        private void OnBoosterActivated()
        {
            isBoosterActive = true;

            if (currentBoosterEffect == null && VFXManager.Instance != null)
            {
                currentBoosterEffect = VFXManager.Instance.AttachBoosterEffect(transform);
            }

            if (IsServer)
            {
                PlayBoosterSoundClientRpc();
            }
        }

        private void OnBoosterDeactivated()
        {
            isBoosterActive = false;

            if (currentBoosterEffect != null && VFXManager.Instance != null)
            {
                VFXManager.Instance.ReturnEffect(currentBoosterEffect, "Booster");
                currentBoosterEffect = null;
            }
        }

        private void OnBladeClash(Vector3 clashPoint)
        {
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnBladeClash(clashPoint, Quaternion.LookRotation(transform.forward));
            }
        }

        private void OnVehicleDestroyed()
        {
            if (VFXManager.Instance != null)
            {
                VFXManager.Instance.SpawnDestruction(transform.position);
            }

            CleanupEffects();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsOwner) return;

            float impactForce = collision.relativeVelocity.magnitude;
            if (impactForce > 5f)
            {
                ContactPoint contact = collision.GetContact(0);
                AudioManager.Instance?.PlayCollision(contact.point, impactForce);
            }
        }

        private void CleanupEffects()
        {
            if (currentBoosterEffect != null && VFXManager.Instance != null)
            {
                VFXManager.Instance.ReturnEffect(currentBoosterEffect, "Booster");
                currentBoosterEffect = null;
            }

            if (currentDriftSmoke != null && VFXManager.Instance != null)
            {
                VFXManager.Instance.ReturnEffect(currentDriftSmoke, "DriftSmoke");
                currentDriftSmoke = null;
            }

            if (engineAudioSource != null)
            {
                Destroy(engineAudioSource.gameObject);
            }
        }

        // Network RPCs
        [ClientRpc]
        private void PlayDriftSoundClientRpc()
        {
            if (!IsOwner) // 다른 플레이어의 드리프트 사운드
            {
                AudioManager.Instance?.PlayDrift(transform.position);
            }
        }

        [ClientRpc]
        private void PlayBoosterSoundClientRpc()
        {
            AudioManager.Instance?.PlayBoosterActivate(transform.position);
        }

        [ServerRpc]
        public void PlayHornServerRpc(int hornId)
        {
            PlayHornClientRpc(hornId);
        }

        [ClientRpc]
        private void PlayHornClientRpc(int hornId)
        {
            AudioManager.Instance?.PlayHorn(transform.position, hornId);
        }
    }
}