using UnityEngine;
using Unity.Netcode;
using System.Collections;
using CarBlade.Physics;

namespace CarBlade.Environment
{
    public class DestructibleObstacle : NetworkBehaviour, IInteractable
    {
        [Header("Obstacle Settings")]
        [SerializeField] private int health = 50;
        [SerializeField] private float respawnTime = 30f;
        [SerializeField] private GameObject destructionEffect;
        [SerializeField] private GameObject[] debrisPrefabs;

        [Header("Collision")]
        [SerializeField] private float minSpeedToDestroy = 10f;
        [SerializeField] private float damageToVehicle = 20f;

        private NetworkVariable<bool> isDestroyed = new NetworkVariable<bool>(false);
        private MeshRenderer meshRenderer;
        private Collider obstacleCollider;

        public bool IsActive => !isDestroyed.Value;

        private void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            obstacleCollider = GetComponent<Collider>();
        }

        private void Start()
        {
            MapManager.Instance?.RegisterInteractableObject(this);
            isDestroyed.OnValueChanged += OnDestroyedStateChanged;
        }

        public void OnVehicleEnter(GameObject vehicle)
        {
            if (isDestroyed.Value) return;

            var vehicleController = vehicle.GetComponent<VehicleController>();
            var healthSystem = vehicle.GetComponent<Combat.VehicleHealthSystem>();

            if (vehicleController != null && healthSystem != null)
            {
                float speed = Mathf.Abs(vehicleController.CurrentSpeed);

                if (speed >= minSpeedToDestroy)
                {
                    if (IsServer)
                    {
                        // ��ֹ� �ı�
                        DestroyObstacle();

                        // ������ ������
                        healthSystem.TakeDamage((int)damageToVehicle);
                    }
                }
                else
                {
                    // �ӵ��� �����ϸ� ������ ������
                    if (IsServer)
                    {
                        healthSystem.TakeDamage((int)(damageToVehicle * 0.5f));
                    }
                }
            }
        }

        public void OnVehicleExit(GameObject vehicle)
        {
            // ��ֹ��� Exit �̺�Ʈ�� ������� ����
        }

        private void DestroyObstacle()
        {
            isDestroyed.Value = true;
            StartCoroutine(RespawnCoroutine());
        }

        private IEnumerator RespawnCoroutine()
        {
            yield return new WaitForSeconds(respawnTime);

            if (IsServer)
            {
                isDestroyed.Value = false;
            }
        }

        private void OnDestroyedStateChanged(bool oldValue, bool newValue)
        {
            if (newValue)
            {
                // �ı� ȿ��
                if (destructionEffect != null)
                {
                    Instantiate(destructionEffect, transform.position, transform.rotation);
                }

                // ���� ����
                if (debrisPrefabs != null && debrisPrefabs.Length > 0)
                {
                    foreach (var debris in debrisPrefabs)
                    {
                        GameObject debrisObj = Instantiate(debris, transform.position, Random.rotation);
                        Rigidbody debrisRb = debrisObj.GetComponent<Rigidbody>();
                        if (debrisRb != null)
                        {
                            Vector3 randomForce = Random.insideUnitSphere * 10f;
                            randomForce.y = Mathf.Abs(randomForce.y);
                            debrisRb.AddForce(randomForce, ForceMode.Impulse);
                        }

                        // 10�� �� ���� ����
                        Destroy(debrisObj, 10f);
                    }
                }

                // ��ֹ� �����
                if (meshRenderer != null) meshRenderer.enabled = false;
                if (obstacleCollider != null) obstacleCollider.enabled = false;
            }
            else
            {
                // ��ֹ� ����
                if (meshRenderer != null) meshRenderer.enabled = true;
                if (obstacleCollider != null) obstacleCollider.enabled = true;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Vehicle"))
            {
                OnVehicleEnter(collision.gameObject);
            }
        }
    }
}