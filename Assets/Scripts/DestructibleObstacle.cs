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
                        // 장애물 파괴
                        DestroyObstacle();

                        // 차량에 데미지
                        healthSystem.TakeDamage((int)damageToVehicle);
                    }
                }
                else
                {
                    // 속도가 부족하면 차량만 데미지
                    if (IsServer)
                    {
                        healthSystem.TakeDamage((int)(damageToVehicle * 0.5f));
                    }
                }
            }
        }

        public void OnVehicleExit(GameObject vehicle)
        {
            // 장애물은 Exit 이벤트를 사용하지 않음
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
                // 파괴 효과
                if (destructionEffect != null)
                {
                    Instantiate(destructionEffect, transform.position, transform.rotation);
                }

                // 잔해 생성
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

                        // 10초 후 잔해 제거
                        Destroy(debrisObj, 10f);
                    }
                }

                // 장애물 숨기기
                if (meshRenderer != null) meshRenderer.enabled = false;
                if (obstacleCollider != null) obstacleCollider.enabled = false;
            }
            else
            {
                // 장애물 복구
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