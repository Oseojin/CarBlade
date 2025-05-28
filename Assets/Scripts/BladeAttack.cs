using UnityEngine;

// �� ��ũ��Ʈ�� �÷��̾� ������ 'Blade' ������Ʈ�� �����մϴ�.
[RequireComponent(typeof(Collider))]
public class BladeAttack : MonoBehaviour
{
    [Header("���� �⺻ ����")]
    [Tooltip("������ ��ȿ�� �ּ� �ӵ� (m/s)")]
    public float minAttackSpeed = 10f;
    [Tooltip("�⺻ ������")]
    public float baseDamage = 20f;

    [Header("�ӵ� ��� ������ ����")]
    [Tooltip("������ ���Ŀ� ���� ���� �ְ� �ӵ� (m/s). ������ ���� �ְ� �ӵ�(m/s) ��ó�� ���� ����. ��: 100km/h�� �� 28m/s.")]
    public float maxSpeedForFormula_MPS = 30f;
    [Tooltip("�ӵ��� �������� �⿩�ϴ� ������ �����ϴ� ����. 1�̸� �⺻, �������� �ӵ� ���� Ŀ��.")]
    public float speedDamageFactor = 1.5f; // �⺻���� 1.5�� �ణ ����

    private Rigidbody carRigidbody;
    private VehicleHealth myVehicleHealth;

    void Start()
    {
        carRigidbody = GetComponentInParent<Rigidbody>();
        myVehicleHealth = GetComponentInParent<VehicleHealth>();

        if (carRigidbody == null || myVehicleHealth == null)
        {
            Debug.LogError("BladeAttack ��ũ��Ʈ�� �θ� ������Ʈ���� Rigidbody�� VehicleHealth�� ã�� �� �����ϴ�! ������ Ȯ�����ּ���.");
            enabled = false; // ��ũ��Ʈ ��Ȱ��ȭ
        }

        // maxSpeedForFormula_MPS�� �ʹ� ���� �����Ǵ� ���� ���� (0���� ������ ����)
        if (maxSpeedForFormula_MPS <= 0)
        {
            Debug.LogWarning("MaxSpeedForFormula_MPS�� 0���� Ŀ�� �մϴ�. �⺻��(1.0f)���� �����մϴ�.");
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
            // Debug.Log("�ӵ��� �ʹ� ���� �������� ���� ���߽��ϴ�. ���� �ӵ�: " + currentSpeedMPS + " m/s");
            return;
        }

        // ������ ���
        // Damage = BaseDamage * (1 + (CurrentSpeed_MPS / MaxSpeedForFormula_MPS) * SpeedDamageFactor)
        float speedRatio = currentSpeedMPS / maxSpeedForFormula_MPS;
        float damage = baseDamage * (1 + (speedRatio * speedDamageFactor));

        Debug.Log($"������ ���: Base({baseDamage}) * (1 + (SpeedRatio({currentSpeedMPS}/{maxSpeedForFormula_MPS} = {speedRatio:F2}) * Factor({speedDamageFactor}))) = {damage:F2}");

        targetHealth.TakeDamage(damage);
    }
}
