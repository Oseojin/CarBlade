using UnityEngine;
using Unity.Netcode;
using System;
using CarBlade.Core;
using CarBlade.Physics;

namespace CarBlade.Combat
{
    public class BladeCombatSystem : NetworkBehaviour, ICombatSystem
    {
        [Header("Combat Settings")]
        [SerializeField] private float minSpeedForDamage = 5f; // 최소 데미지 속도 (m/s)
        [SerializeField] private float baseDamage = 20f;
        [SerializeField] private float speedDamageMultiplier = 2f;
        [SerializeField] private float angularDamageMultiplier = 1.5f;
        [SerializeField] private float clashKnockbackForce = 1000f;

        [Header("Blade Setup")]
        [SerializeField] private Transform bladeTransform;
        [SerializeField] private BoxCollider bladeCollider;
        [SerializeField] private LayerMask vehicleLayer;

        // Components
        private VehicleController vehicleController;
        private VehicleHealthSystem healthSystem;
        private GameManager gameManager;
        private ScoreSystem scoreSystem;

        // State
        private float lastClashTime = 0f;
        private const float CLASH_COOLDOWN = 0.5f;

        // Events
        public event Action<int, int, float> OnBladeHit; // attacker, target, damage
        public event Action<Vector3> OnBladeClash;
        public event Action<int> OnVehicleDestroyed;

        private void Awake()
        {
            vehicleController = GetComponent<VehicleController>();
            healthSystem = GetComponent<VehicleHealthSystem>();

            if (bladeCollider == null && bladeTransform != null)
            {
                bladeCollider = bladeTransform.GetComponent<BoxCollider>();
            }
        }

        private void Start()
        {
            gameManager = GameManager.Instance;
            scoreSystem = gameManager?.GetScoreSystem() as ScoreSystem;

            // 블레이드 콜라이더 설정
            if (bladeCollider != null)
            {
                bladeCollider.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsOwner) return;

            // 다른 차량의 블레이드와 충돌
            if (other.CompareTag("Blade"))
            {
                ProcessBladeCollision(other);
            }
            // 다른 차량의 본체와 충돌
            else if (other.CompareTag("Vehicle"))
            {
                ProcessVehicleHit(other);
            }
        }

        // 블레이드 vs 블레이드 충돌 처리
        private void ProcessBladeCollision(Collider otherBlade)
        {
            // 클래시 쿨다운 체크
            if (Time.time - lastClashTime < CLASH_COOLDOWN) return;

            var otherCombat = otherBlade.GetComponentInParent<BladeCombatSystem>();
            if (otherCombat == null) return;

            // 상대적 속도 계산
            float relativeSpeed = (vehicleController.CurrentSpeed - otherCombat.vehicleController.CurrentSpeed);

            // 일정 속도 이상일 때만 클래시 발생
            if (Mathf.Abs(relativeSpeed) > minSpeedForDamage)
            {
                Vector3 clashPoint = otherBlade.ClosestPoint(transform.position);
                ProcessBladeClashServerRpc(NetworkObjectId, otherCombat.NetworkObjectId, clashPoint);

                lastClashTime = Time.time;
            }
        }

        // 차량 타격 처리
        private void ProcessVehicleHit(Collider vehicleCollider)
        {
            // 자신의 속도 체크
            if (Mathf.Abs(vehicleController.CurrentSpeed) < minSpeedForDamage) return;

            var targetHealth = vehicleCollider.GetComponent<VehicleHealthSystem>();
            if (targetHealth == null || targetHealth == healthSystem) return;

            // 데미지 계산
            float damage = CalculateDamage(
                Mathf.Abs(vehicleController.CurrentSpeed),
                Mathf.Abs(vehicleController.AngularVelocity)
            );

            // 서버에 데미지 요청
            ProcessBladeHitServerRpc(
                NetworkObjectId,
                targetHealth.NetworkObjectId,
                damage,
                targetHealth.CurrentHealth == targetHealth.MaxHealth
            );
        }

        // ICombatSystem 구현
        public float CalculateDamage(float speed, float angularVelocity)
        {
            // 기본 데미지
            float damage = baseDamage;

            // 속도 배율
            float speedRatio = speed / vehicleController.VehicleData.maxSpeed;
            damage *= (1f + speedRatio * speedDamageMultiplier);

            // 각속도 배율 (드리프트 보너스)
            float angularRatio = angularVelocity / vehicleController.VehicleData.maxAngularVelocity;
            damage *= (1f + angularRatio * angularDamageMultiplier);

            // 차량 타입에 따른 데미지 보정
            damage *= vehicleController.VehicleData.collisionDamageMultiplier;

            return Mathf.Round(damage);
        }

        public void ProcessBladeHit(int attackerId, int targetId)
        {
            // 서버에서 처리
            if (!IsServer) return;

            var attacker = NetworkManager.Singleton.ConnectedClients[((uint)attackerId)].PlayerObject;
            var target = NetworkManager.Singleton.ConnectedClients[((uint)targetId)].PlayerObject;

            if (attacker == null || target == null) return;

            OnBladeHit?.Invoke(attackerId, targetId, 0);
        }

        public void ProcessBladeClash(int player1Id, int player2Id)
        {
            // 서버에서 처리
            if (!IsServer) return;

            Debug.Log($"Blade Clash between Player {player1Id} and Player {player2Id}!");
        }

        // 네트워크 RPC
        [ServerRpc]
        private void ProcessBladeHitServerRpc(ulong attackerNetId, ulong targetNetId, float damage, bool isPotentialOneShot)
        {
            var attackerObj = NetworkManager.SpawnManager.SpawnedObjects[attackerNetId];
            var targetObj = NetworkManager.SpawnManager.SpawnedObjects[targetNetId];

            if (attackerObj == null || targetObj == null) return;

            var targetHealth = targetObj.GetComponent<VehicleHealthSystem>();
            var attackerCombat = attackerObj.GetComponent<BladeCombatSystem>();

            if (targetHealth == null || attackerCombat == null) return;

            // 데미지 적용
            bool wasDestroyed = targetHealth.TakeDamage((int)damage, attackerNetId);

            // 점수 시스템 업데이트
            if (scoreSystem != null)
            {
                int attackerId = (int)attackerObj.GetComponent<NetworkObject>().OwnerClientId;
                int targetId = (int)targetObj.GetComponent<NetworkObject>().OwnerClientId;

                // 피해 기록
                scoreSystem.RecordDamage(attackerId, targetId, damage);

                // 처치 처리
                if (wasDestroyed)
                {
                    bool isOneShot = isPotentialOneShot && damage >= targetHealth.MaxHealth;
                    scoreSystem.ProcessKill(attackerId, targetId, isOneShot);

                    // 원샷 킬인 경우 부스터 즉시 충전
                    if (isOneShot)
                    {
                        var attackerBooster = attackerObj.GetComponent<BoosterSystem>();
                        attackerBooster?.InstantChargeBoost();
                    }
                }
            }

            // 클라이언트에 알림
            NotifyBladeHitClientRpc(attackerNetId, targetNetId, damage, wasDestroyed);
        }

        [ServerRpc]
        private void ProcessBladeClashServerRpc(ulong player1NetId, ulong player2NetId, Vector3 clashPoint)
        {
            var player1Obj = NetworkManager.SpawnManager.SpawnedObjects[player1NetId];
            var player2Obj = NetworkManager.SpawnManager.SpawnedObjects[player2NetId];

            if (player1Obj == null || player2Obj == null) return;

            // 양쪽 플레이어를 밀어냄
            var rb1 = player1Obj.GetComponent<Rigidbody>();
            var rb2 = player2Obj.GetComponent<Rigidbody>();

            if (rb1 != null && rb2 != null)
            {
                Vector3 pushDir1 = (player1Obj.transform.position - player2Obj.transform.position).normalized;
                Vector3 pushDir2 = -pushDir1;

                rb1.AddForce(pushDir1 * clashKnockbackForce, ForceMode.Impulse);
                rb2.AddForce(pushDir2 * clashKnockbackForce, ForceMode.Impulse);
            }

            // 클래시 이펙트
            NotifyBladeClashClientRpc(clashPoint);
        }

        [ClientRpc]
        private void NotifyBladeHitClientRpc(ulong attackerNetId, ulong targetNetId, float damage, bool wasDestroyed)
        {
            // UI 업데이트, 사운드 재생 등
            OnBladeHit?.Invoke((int)attackerNetId, (int)targetNetId, damage);

            if (wasDestroyed)
            {
                OnVehicleDestroyed?.Invoke((int)targetNetId);
            }
        }

        [ClientRpc]
        private void NotifyBladeClashClientRpc(Vector3 clashPoint)
        {
            OnBladeClash?.Invoke(clashPoint);

            // VFX/Audio 팀에서 이펙트 재생
            Debug.Log($"Blade Clash at {clashPoint}!");
        }

        // 디버그 표시
        private void OnDrawGizmos()
        {
            if (bladeTransform == null) return;

            // 블레이드 영역 표시
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            if (bladeCollider != null)
            {
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(bladeTransform.position, bladeTransform.rotation, bladeTransform.lossyScale);
                Gizmos.DrawCube(bladeCollider.center, bladeCollider.size);
                Gizmos.matrix = oldMatrix;
            }
        }
    }
}