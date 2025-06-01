using UnityEngine;

namespace CarBlade.Physics
{
    // ���� Ÿ�� ������
    public enum VehicleType
    {
        Light,      // �淮��
        Heavy,      // �߷���
        Balanced    // �뷱����
    }

    // ���� ������ ScriptableObject
    [CreateAssetMenu(fileName = "VehicleData", menuName = "CarBlade/Vehicle Data", order = 1)]
    public class VehicleData : ScriptableObject
    {
        [Header("Vehicle Info")]
        public string vehicleName = "Default Vehicle";
        public VehicleType vehicleType = VehicleType.Balanced;
        public GameObject vehiclePrefab;

        [Header("Health Stats")]
        [Range(50, 200)]
        public int maxHealth = 100;

        [Header("Movement Stats")]
        [Range(5f, 30f)]
        public float maxSpeed = 20f;  // m/s

        [Range(10f, 50f)]
        public float acceleration = 25f;  // m/s��

        [Range(5f, 20f)]
        public float brakeForce = 15f;

        [Range(0.5f, 2f)]
        public float handling = 1f;  // �ڵ鸵 ����

        [Header("Drift Settings")]
        [Range(0.1f, 1f)]
        public float driftFactor = 0.5f;  // �帮��Ʈ �� �̲����� ����

        [Range(1f, 3f)]
        public float driftBoostChargeRate = 2f;  // �ʴ� �ν��� ������

        [Range(10f, 30f)]
        public float maxAngularVelocity = 20f;  // �ִ� ȸ�� �ӵ�

        [Header("Booster Settings")]
        [Range(1.2f, 2f)]
        public float boosterSpeedMultiplier = 1.5f;  // �ν��� ��� �� �ӵ� ����

        [Range(2f, 5f)]
        public float boosterDuration = 3f;  // �ν��� ���� �ð�

        [Range(20f, 50f)]
        public float boosterConsumptionRate = 33.3f;  // �ʴ� �ν��� �Һ�

        [Header("Physics Settings")]
        [Range(1000f, 3000f)]
        public float vehicleMass = 1500f;  // kg

        [Range(0.5f, 2f)]
        public float downForce = 1f;  // �ٿ����� (���� ������)

        [Range(0.1f, 0.5f)]
        public float airDrag = 0.3f;  // ���� ����

        [Header("Collision Settings")]
        public Vector3 centerOfMass = new Vector3(0, -0.5f, 0);
        public float collisionDamageMultiplier = 1f;

        // ���� Ÿ�Ժ� ������ ���� ����
        public static VehicleData CreateLightPreset()
        {
            VehicleData data = CreateInstance<VehicleData>();
            data.vehicleName = "Light Racer";
            data.vehicleType = VehicleType.Light;
            data.maxHealth = 75;
            data.maxSpeed = 25f;
            data.acceleration = 40f;
            data.handling = 1.5f;
            data.vehicleMass = 1000f;
            data.driftFactor = 0.7f;
            data.maxAngularVelocity = 25f;
            return data;
        }

        public static VehicleData CreateHeavyPreset()
        {
            VehicleData data = CreateInstance<VehicleData>();
            data.vehicleName = "Heavy Crusher";
            data.vehicleType = VehicleType.Heavy;
            data.maxHealth = 150;
            data.maxSpeed = 30f;
            data.acceleration = 15f;
            data.handling = 0.7f;
            data.vehicleMass = 2500f;
            data.driftFactor = 0.3f;
            data.maxAngularVelocity = 15f;
            return data;
        }

        public static VehicleData CreateBalancedPreset()
        {
            VehicleData data = CreateInstance<VehicleData>();
            data.vehicleName = "Balanced Fighter";
            data.vehicleType = VehicleType.Balanced;
            data.maxHealth = 100;
            data.maxSpeed = 22f;
            data.acceleration = 25f;
            data.handling = 1f;
            data.vehicleMass = 1500f;
            data.driftFactor = 0.5f;
            data.maxAngularVelocity = 20f;
            return data;
        }
    }
}