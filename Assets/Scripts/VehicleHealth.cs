using UnityEngine;

// �� ��ũ��Ʈ�� �÷��̾�, AI �� ��� ������ ��Ʈ ������Ʈ�� �����մϴ�.
public class VehicleHealth : MonoBehaviour
{
    [Header("���� ü��")]
    public float maxHealth = 100f;
    private float currentHealth; // ���� private���� ����

    // �� ������ �÷��̾� �������� �ĺ��ϴ� �÷���
    public bool isPlayer = false;

    void Start()
    {
        ResetHealth(); // ���� �� ü�� �ʱ�ȭ
    }

    public void TakeDamage(float damageAmount)
    {
        if (currentHealth <= 0 && gameObject.activeInHierarchy) return; // �̹� ó�� ���̰ų� �ı��� ���¸� �ߺ� ȣ�� ����

        currentHealth -= damageAmount;
        Debug.Log(gameObject.name + "�� " + damageAmount + "�� �������� �Ծ����ϴ�. ���� ü��: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log(gameObject.name + "�� �ı��Ǿ����ϴ�!");
        gameObject.SetActive(false); // ���� ��Ȱ��ȭ

        // GameManager�� �˸�
        if (GameManager.Instance != null)
        {
            GameManager.Instance.VehicleDestroyed(this);
        }
        else
        {
            Debug.LogWarning("GameManager �ν��Ͻ��� ã�� �� �����ϴ�.");
        }
    }

    // �ܺο��� ���� ü���� Ȯ���ؾ� �� ��츦 ���� �Լ�
    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    // ü���� �ִ�� �����ϴ� �Լ� (������ �� ���)
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        // �ʿ��ϴٸ� ���⿡ ������ �ٸ� ����(��: ���� �ļ� ����) �ʱ�ȭ ���� �߰�
    }

    // ������Ʈ�� �ٽ� Ȱ��ȭ�� �� ü���� ���� (������ �� Ȱ��)
    void OnEnable()
    {
        ResetHealth();
        // ���� Rigidbody�� �ִٸ� �ӵ��� �ʱ�ȭ�ϴ� ���� ����
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
