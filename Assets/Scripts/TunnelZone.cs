using UnityEngine;
using Unity.Netcode;
using System.Collections;
using CarBlade.Physics;

namespace CarBlade.Environment
{
    public class TunnelZone : NetworkBehaviour, IInteractable
    {
        [Header("Tunnel Settings")]
        [SerializeField] private bool limitVisibility = true;
        [SerializeField] private float visibilityRange = 10f;
        [SerializeField] private bool applySpeedBoost = true;
        [SerializeField] private float speedBoostMultiplier = 1.2f;

        [Header("Ambush Mechanics")]
        [SerializeField] private Transform[] ambushPoints;
        [SerializeField] private bool notifyOnEntry = true;

        private bool isActive = true;
        public bool IsActive => isActive;

        // �ͳ� ���� ���� ����
        private System.Collections.Generic.List<GameObject> vehiclesInTunnel = new System.Collections.Generic.List<GameObject>();

        private void Start()
        {
            MapManager.Instance?.RegisterInteractableObject(this);
        }

        public void OnVehicleEnter(GameObject vehicle)
        {
            if (!vehiclesInTunnel.Contains(vehicle))
            {
                vehiclesInTunnel.Add(vehicle);

                if (IsServer)
                {
                    ApplyTunnelEffects(vehicle, true);

                    if (notifyOnEntry)
                    {
                        NotifyTunnelEntryClientRpc(vehicle.GetComponent<NetworkObject>().NetworkObjectId);
                    }
                }
            }
        }

        public void OnVehicleExit(GameObject vehicle)
        {
            if (vehiclesInTunnel.Contains(vehicle))
            {
                vehiclesInTunnel.Remove(vehicle);

                if (IsServer)
                {
                    ApplyTunnelEffects(vehicle, false);
                }
            }
        }

        private void ApplyTunnelEffects(GameObject vehicle, bool entering)
        {
            var vehicleController = vehicle.GetComponent<VehicleController>();
            if (vehicleController == null) return;

            // �ӵ� �ν�Ʈ ȿ��
            if (applySpeedBoost)
            {
                // VehicleController�� �ͳ� �ν�Ʈ �÷��� ����
                // ���� ���������� VehicleController�� �޼��� �߰� �ʿ�
            }

            // �þ� ���� ȿ��
            if (limitVisibility)
            {
                // UI ���� �����Ͽ� ����
            }
        }

        [ClientRpc]
        private void NotifyTunnelEntryClientRpc(ulong vehicleNetId)
        {
            // UI�� �ͳ� ���� �˸�
            Debug.Log($"Vehicle {vehicleNetId} entered tunnel!");
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Vehicle"))
            {
                OnVehicleEnter(other.gameObject);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Vehicle"))
            {
                OnVehicleExit(other.gameObject);
            }
        }

        // �ź� ����Ʈ ��ȯ
        public Transform GetRandomAmbushPoint()
        {
            if (ambushPoints != null && ambushPoints.Length > 0)
            {
                return ambushPoints[Random.Range(0, ambushPoints.Length)];
            }
            return null;
        }

        private void OnDrawGizmos()
        {
            // �ͳ� ���� ǥ��
            Gizmos.color = new Color(0.5f, 0, 0.5f, 0.3f);
            Gizmos.DrawCube(transform.position, transform.localScale);

            // �ź� ����Ʈ ǥ��
            if (ambushPoints != null)
            {
                Gizmos.color = Color.red;
                foreach (var point in ambushPoints)
                {
                    if (point != null)
                    {
                        Gizmos.DrawWireSphere(point.position, 1f);
                    }
                }
            }
        }
    }
}