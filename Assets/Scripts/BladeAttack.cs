using UnityEngine;

// 이 스크립트는 플레이어 차량의 'Blade' 오브젝트에 연결합니다.
[RequireComponent(typeof(Collider))]
public class BladeAttack : MonoBehaviour
{
    [Header("공격 기본 설정")]
    [Tooltip("공격이 유효한 최소 속도 (m/s)")]
    public float minAttackSpeed = 10f;
    [Tooltip("기본 데미지")]
    public float baseDamage = 20f;

    [Header("속도 기반 데미지 설정")]
    [Tooltip("데미지 공식에 사용될 기준 최고 속도 (m/s). 차량의 실제 최고 속도(m/s) 근처로 설정 권장. 예: 100km/h는 약 28m/s.")]
    public float maxSpeedForFormula_MPS = 30f;
    [Tooltip("속도가 데미지에 기여하는 정도를 조절하는 배율. 1이면 기본, 높을수록 속도 영향 커짐.")]
    public float speedDamageFactor = 1.5f; // 기본값을 1.5로 약간 상향

    private Rigidbody carRigidbody;
    private VehicleHealth myVehicleHealth;

    void Start()
    {
        carRigidbody = GetComponentInParent<Rigidbody>();
        myVehicleHealth = GetComponentInParent<VehicleHealth>();

        if (carRigidbody == null || myVehicleHealth == null)
        {
            Debug.LogError("BladeAttack 스크립트가 부모 오브젝트에서 Rigidbody나 VehicleHealth를 찾을 수 없습니다! 구조를 확인해주세요.");
            enabled = false; // 스크립트 비활성화
        }

        // maxSpeedForFormula_MPS가 너무 낮게 설정되는 것을 방지 (0으로 나누기 방지)
        if (maxSpeedForFormula_MPS <= 0)
        {
            Debug.LogWarning("MaxSpeedForFormula_MPS는 0보다 커야 합니다. 기본값(1.0f)으로 설정합니다.");
            maxSpeedForFormula_MPS = 1.0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled) return;

        VehicleHealth targetHealth = other.GetComponentInParent<VehicleHealth>();

        if (targetHealth == null || targetHealth == myVehicleHealth)
        {
            return;
        }

        float currentSpeedMPS = carRigidbody.linearVelocity.magnitude;

        if (currentSpeedMPS < minAttackSpeed)
        {
            // Debug.Log("속도가 너무 낮아 데미지를 주지 못했습니다. 현재 속도: " + currentSpeedMPS + " m/s");
            return;
        }

        // 데미지 계산
        // Damage = BaseDamage * (1 + (CurrentSpeed_MPS / MaxSpeedForFormula_MPS) * SpeedDamageFactor)
        float speedRatio = currentSpeedMPS / maxSpeedForFormula_MPS;
        float damage = baseDamage * (1 + (speedRatio * speedDamageFactor));

        Debug.Log($"데미지 계산: Base({baseDamage}) * (1 + (SpeedRatio({currentSpeedMPS}/{maxSpeedForFormula_MPS} = {speedRatio:F2}) * Factor({speedDamageFactor}))) = {damage:F2}");

        targetHealth.TakeDamage(damage);
    }
}
