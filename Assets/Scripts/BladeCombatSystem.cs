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
        [SerializeField] private float minSpeedForDamage = 5f; // �ּ� ������ �ӵ� (m/s)
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

            // ���̵� �ݶ��̴� ����
            if (bladeCollider != null)
            {
                bladeCollider.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsOwner) return;

            // �ٸ� ������ ���̵�� �浹
            if (other.CompareTag("Blade"))
            {
                ProcessBladeCollision(other);
            }
            // �ٸ� ������ ��ü�� �浹
            else if (other.CompareTag("Vehicle"))
            {
                ProcessVehicleHit(other);
            }
        }

        // ���̵� vs ���̵� �浹 ó��
        private void ProcessBladeCollision(Collider otherBlade)
        {
            // Ŭ���� ��ٿ� üũ
            if (Time.time - lastClashTime < CLASH_COOLDOWN) return;

            var otherCombat = otherBlade.GetComponentInParent<BladeCombatSystem>();
            if (otherCombat == null) return;

            // ����� �ӵ� ���
            float relativeSpeed = (vehicleController.CurrentSpeed - otherCombat.vehicleController.CurrentSpeed);

            // ���� �ӵ� �̻��� ���� Ŭ���� �߻�
            if (Mathf.Abs(relativeSpeed) > minSpeedForDamage)
            {
                Vector3 clashPoint = otherBlade.ClosestPoint(transform.position);
                ProcessBladeClashServerRpc(NetworkObjectId, otherCombat.NetworkObjectId, clashPoint);

                lastClashTime = Time.time;
            }
        }

        // ���� Ÿ�� ó��
        private void ProcessVehicleHit(Collider vehicleCollider)
        {
            // �ڽ��� �ӵ� üũ
            if (Mathf.Abs(vehicleController.CurrentSpeed) < minSpeedForDamage) return;

            var targetHealth = vehicleCollider.GetComponent<VehicleHealthSystem>();
            if (targetHealth == null || targetHealth == healthSystem) return;

            // ������ ���
            float damage = CalculateDamage(
                Mathf.Abs(vehicleController.CurrentSpeed),
                Mathf.Abs(vehicleController.AngularVelocity)
            );

            // ������ ������ ��û
            ProcessBladeHitServerRpc(
                NetworkObjectId,
                targetHealth.NetworkObjectId,
                damage,
                targetHealth.CurrentHealth == targetHealth.MaxHealth
            );
        }

        // ICombatSystem ����
        public float CalculateDamage(float speed, float angularVelocity)
        {
            // �⺻ ������
            float damage = baseDamage;

            // �ӵ� ����
            float speedRatio = speed / vehicleController.VehicleData.maxSpeed;
            damage *= (1f + speedRatio * speedDamageMultiplier);

            // ���ӵ� ���� (�帮��Ʈ ���ʽ�)
            float angularRatio = angularVelocity / vehicleController.VehicleData.maxAngularVelocity;
            damage *= (1f + angularRatio * angularDamageMultiplier);

            // ���� Ÿ�Կ� ���� ������ ����
            damage *= vehicleController.VehicleData.collisionDamageMultiplier;

            return Mathf.Round(damage);
        }

        public void ProcessBladeHit(int attackerId, int targetId)
        {
            // �������� ó��
            if (!IsServer) return;

            var attacker = NetworkManager.Singleton.ConnectedClients[((uint)attackerId)].PlayerObject;
            var target = NetworkManager.Singleton.ConnectedClients[((uint)targetId)].PlayerObject;

            if (attacker == null || target == null) return;

            OnBladeHit?.Invoke(attackerId, targetId, 0);
        }

        public void ProcessBladeClash(int player1Id, int player2Id)
        {
            // �������� ó��
            if (!IsServer) return;

            Debug.Log($"Blade Clash between Player {player1Id} and Player {player2Id}!");
        }

        // ��Ʈ��ũ RPC
        [ServerRpc]
        private void ProcessBladeHitServerRpc(ulong attackerNetId, ulong targetNetId, float damage, bool isPotentialOneShot)
        {
            var attackerObj = NetworkManager.SpawnManager.SpawnedObjects[attackerNetId];
            var targetObj = NetworkManager.SpawnManager.SpawnedObjects[targetNetId];

            if (attackerObj == null || targetObj == null) return;

            var targetHealth = targetObj.GetComponent<VehicleHealthSystem>();
            var attackerCombat = attackerObj.GetComponent<BladeCombatSystem>();

            if (targetHealth == null || attackerCombat == null) return;

            // ������ ����
            bool wasDestroyed = targetHealth.TakeDamage((int)damage, attackerNetId);

            // ���� �ý��� ������Ʈ
            if (scoreSystem != null)
            {
                int attackerId = (int)attackerObj.GetComponent<NetworkObject>().OwnerClientId;
                int targetId = (int)targetObj.GetComponent<NetworkObject>().OwnerClientId;

                // ���� ���
                scoreSystem.RecordDamage(attackerId, targetId, damage);

                // óġ ó��
                if (wasDestroyed)
                {
                    bool isOneShot = isPotentialOneShot && damage >= targetHealth.MaxHealth;
                    scoreSystem.ProcessKill(attackerId, targetId, isOneShot);

                    // ���� ų�� ��� �ν��� ��� ����
                    if (isOneShot)
                    {
                        var attackerBooster = attackerObj.GetComponent<BoosterSystem>();
                        attackerBooster?.InstantChargeBoost();
                    }
                }
            }

            // Ŭ���̾�Ʈ�� �˸�
            NotifyBladeHitClientRpc(attackerNetId, targetNetId, damage, wasDestroyed);
        }

        [ServerRpc]
        private void ProcessBladeClashServerRpc(ulong player1NetId, ulong player2NetId, Vector3 clashPoint)
        {
            var player1Obj = NetworkManager.SpawnManager.SpawnedObjects[player1NetId];
            var player2Obj = NetworkManager.SpawnManager.SpawnedObjects[player2NetId];

            if (player1Obj == null || player2Obj == null) return;

            // ���� �÷��̾ �о
            var rb1 = player1Obj.GetComponent<Rigidbody>();
            var rb2 = player2Obj.GetComponent<Rigidbody>();

            if (rb1 != null && rb2 != null)
            {
                Vector3 pushDir1 = (player1Obj.transform.position - player2Obj.transform.position).normalized;
                Vector3 pushDir2 = -pushDir1;

                rb1.AddForce(pushDir1 * clashKnockbackForce, ForceMode.Impulse);
                rb2.AddForce(pushDir2 * clashKnockbackForce, ForceMode.Impulse);
            }

            // Ŭ���� ����Ʈ
            NotifyBladeClashClientRpc(clashPoint);
        }

        [ClientRpc]
        private void NotifyBladeHitClientRpc(ulong attackerNetId, ulong targetNetId, float damage, bool wasDestroyed)
        {
            // UI ������Ʈ, ���� ��� ��
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

            // VFX/Audio ������ ����Ʈ ���
            Debug.Log($"Blade Clash at {clashPoint}!");
        }

        // ����� ǥ��
        private void OnDrawGizmos()
        {
            if (bladeTransform == null) return;

            // ���̵� ���� ǥ��
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