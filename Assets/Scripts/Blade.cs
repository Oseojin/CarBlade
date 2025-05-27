using UnityEngine;

public class Blade : MonoBehaviour
{
    [Header("���̵� ����")]
    public float baseDamage = 10f; // ���̵��� �⺻ ������ (�ӵ�/���ӵ� ������)
    public string targetTag = "PlayerVehicle"; // ������ ����� �±� (���� �±�)

    private Rigidbody vehicleRb; // ���̵带 ������ ������ Rigidbody
    private VehicleController vehicleController; // ���� ��Ʈ�ѷ� (�ӵ�, ���ӵ� ���ٿ�)
    private VehicleHealth myVehicleHealth; // �ڽ��� ���� ü�� (���� ������)

    void Start()
    {
        // �θ� ������Ʈ���� �ʿ��� ������Ʈ���� ã���ϴ�.
        // �� ���̵�� ���� ������Ʈ�� �ڽ����� �����ؾ� �մϴ�.
        Transform parentVehicle = transform.root; // �ֻ��� �θ� �������� ����
        if (parentVehicle != null)
        {
            vehicleRb = parentVehicle.GetComponent<Rigidbody>();
            vehicleController = parentVehicle.GetComponent<VehicleController>();
            myVehicleHealth = parentVehicle.GetComponent<VehicleHealth>();
        }

        if (vehicleRb == null)
        {
            Debug.LogError(gameObject.name + ": ���̵带 ������ ������ Rigidbody�� ã�� �� �����ϴ�!", this.gameObject);
        }
        if (vehicleController == null)
        {
            Debug.LogError(gameObject.name + ": ���̵带 ������ ������ VehicleController�� ã�� �� �����ϴ�!", this.gameObject);
        }
        if (myVehicleHealth == null)
        {
            Debug.LogError(gameObject.name + ": ���̵带 ������ ������ VehicleHealth�� ã�� �� �����ϴ�!", this.gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // --- ����� �α� �߰� ---
        Debug.Log(transform.root.name + "�� ���̵尡 " + collision.gameObject.name + "��(��) OnCollisionEnter �߻�!");
        // -----------------------

        // �浹�� ������Ʈ�� ���� ��� �±׸� ������ �ִ��� Ȯ��
        if (collision.gameObject.CompareTag(targetTag))
        {
            // --- ����� �α� �߰� ---
            Debug.Log(collision.gameObject.name + "�� �±װ� " + targetTag + "�� ��ġ�մϴ�.");
            // -----------------------

            VehicleHealth targetHealth = collision.gameObject.GetComponent<VehicleHealth>();

            // ������ VehicleHealth ������Ʈ�� ������ �ְ�, �ڱ� �ڽ��� �ƴ϶��
            if (targetHealth != null && targetHealth != myVehicleHealth)
            {
                // --- ����� �α� �߰� ---
                Debug.Log(collision.gameObject.name + "�� VehicleHealth�� ������ �ְ�, �� �ڽ��� �ƴմϴ�. ������ ��� ����.");
                // -----------------------

                // --- ������ ��� ���� (��ȹ ���� ���) ---
                float currentSpeed = 0f;
                float currentAngularVelocity = 0f; // ����� ���ӵ� �̱���, ���� VehicleController���� �����;� ��

                if (vehicleRb != null)
                {
                    currentSpeed = vehicleRb.linearVelocity.magnitude; // ���� ������ �ӵ�
                    // TODO: ���ӵ� �������� (VehicleController�� �帮��Ʈ ���� �Ǵ� ���ӵ� ��ȯ �Լ� �ʿ�)
                    // currentAngularVelocity = vehicleController.GetCurrentAngularVelocity();
                }

                // ��ȹ ������ ������ ���� ���� (�ִ� �ӵ�/���ӵ��� �ӽð�, Ʃ�� �ʿ�)
                float maxSpeed = 50f; // ���� �ִ� �ӵ� (m/s)
                // float maxAngularVelocity = 10f; // ���� �ִ� ���ӵ� (rad/s)

                float speedFactor = 1 + (currentSpeed / maxSpeed);
                // float angularVelocityFactor = 1 + (currentAngularVelocity / maxAngularVelocity); // ���ӵ� ���� �� Ȱ��ȭ

                float calculatedDamage = baseDamage * speedFactor; // * angularVelocityFactor; // ���ӵ� ���� �� Ȱ��ȭ

                Debug.Log(transform.root.name + "�� ���̵尡 " + collision.gameObject.name + "���� �浹! �ӵ�: " + currentSpeed.ToString("F2") + ", ���� ������: " + calculatedDamage.ToString("F2"));

                // ���濡�� ������ ���� (�����ڴ� �� ���̵��� ����)
                targetHealth.TakeDamage(calculatedDamage, transform.root.gameObject);
            }
            else if (targetHealth == null)
            {
                Debug.LogWarning(collision.gameObject.name + "���� VehicleHealth ������Ʈ�� �����ϴ�.");
            }
            else if (targetHealth == myVehicleHealth)
            {
                Debug.LogWarning("�ڽ��� ����(" + collision.gameObject.name + ")�� �浹�߽��ϴ�. (���� ����)");
            }
        }
        else
        {
            Debug.Log(collision.gameObject.name + "�� �±�(" + collision.gameObject.tag + ")�� " + targetTag + "�� ��ġ���� �ʽ��ϴ�.");
        }
        // TODO: ���̵� Ŭ���� ���� (���浵 ���̵��̰�, ���� ���� �浹 ��)
    }
}
