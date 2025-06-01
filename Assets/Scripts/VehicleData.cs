using UnityEngine;

namespace CarBlade.Physics
{
    // 차량 타입 열거형
    public enum VehicleType
    {
        Light,      // 경량형
        Heavy,      // 중량형
        Balanced    // 밸런스형
    }

    // 차량 데이터 ScriptableObject
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
        public float acceleration = 25f;  // m/s²

        [Range(5f, 20f)]
        public float brakeForce = 15f;

        [Range(0.5f, 2f)]
        public float handling = 1f;  // 핸들링 배율

        [Header("Drift Settings")]
        [Range(0.1f, 1f)]
        public float driftFactor = 0.5f;  // 드리프트 중 미끄러짐 정도

        [Range(1f, 3f)]
        public float driftBoostChargeRate = 2f;  // 초당 부스터 충전량

        [Range(10f, 30f)]
        public float maxAngularVelocity = 20f;  // 최대 회전 속도

        [Header("Booster Settings")]
        [Range(1.2f, 2f)]
        public float boosterSpeedMultiplier = 1.5f;  // 부스터 사용 시 속도 배율

        [Range(2f, 5f)]
        public float boosterDuration = 3f;  // 부스터 지속 시간

        [Range(20f, 50f)]
        public float boosterConsumptionRate = 33.3f;  // 초당 부스터 소비량

        [Header("Physics Settings")]
        [Range(1000f, 3000f)]
        public float vehicleMass = 1500f;  // kg

        [Range(0.5f, 2f)]
        public float downForce = 1f;  // 다운포스 (지면 압착력)

        [Range(0.1f, 0.5f)]
        public float airDrag = 0.3f;  // 공기 저항

        [Header("Collision Settings")]
        public Vector3 centerOfMass = new Vector3(0, -0.5f, 0);
        public float collisionDamageMultiplier = 1f;

        // 차량 타입별 프리셋 생성 헬퍼
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