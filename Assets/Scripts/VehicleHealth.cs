using UnityEngine;
using UnityEngine.Events; // UnityEvent ���
using System.Collections; // �ڷ�ƾ ���

public class VehicleHealth : MonoBehaviour
{
    [Header("ü�� ����")]
    public float maxHealth = 100f; // �ִ� ü��
    private float currentHealth; // ���� ü��
    private bool isDead = false; // ��� ���� �÷��� (�ߺ� ��� ó�� ������)

    [Header("�ı� ȿ��")]
    public GameObject destructionEffectPrefab; // �ı� �� ������ ��ƼŬ ȿ�� ������
    public float effectDuration = 2f; // �ı� ȿ���� ���ӵ� �ð� (���� �ڵ� �ı�)

    [Header("������ ����")]
    public float respawnDelay = 5f; // ���������� �ɸ��� �ð� (��)
    public float invulnerabilityDuration = 2f; // ������ �� ���� ���� �ð� (��)
    [Tooltip("������ �������� �� �ִ� ��ġ���� �迭�Դϴ�. Inspector���� �������ּ���.")]
    public Transform[] spawnPoints; // ���� ������ (Transform �迭)
    private bool isInvulnerable = false; // ���� ���� �������� ����

    [Header("�̺�Ʈ")]
    public UnityEvent OnTakeDamage; // �������� �Ծ��� �� ȣ��� UnityEvent
    public UnityEvent OnDie; // ������ �ı��Ǿ��� �� ȣ��� UnityEvent
    public UnityEvent OnRespawn; // ������ �������Ǿ��� �� ȣ��� UnityEvent

    // ������ �ð���/������ ��Ҹ� �����ϱ� ���� ������Ʈ ����
    private Renderer[] renderers; // ���� �� �ڽ� ������Ʈ�� ��� Renderer ������Ʈ
    private Collider[] colliders; // ���� �� �ڽ� ������Ʈ�� ��� Collider ������Ʈ
    private VehicleController vehicleController; // ���� ��Ʈ�ѷ� ��ũ��Ʈ ����
    private Rigidbody rb; // ������ Rigidbody ������Ʈ


    void Awake()
    {
        currentHealth = maxHealth; // ���� �� ü���� �ִ�� ����

        // ������ ��� �������� �ݶ��̴��� ������ (��Ȱ��ȭ�� �ڽ� �����Ͽ� �˻�)
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
        vehicleController = GetComponent<VehicleController>();
        rb = GetComponent<Rigidbody>();

        // �ʼ� ������Ʈ Ȯ��
        if (vehicleController == null) Debug.LogError("VehicleController ������Ʈ�� ã�� �� �����ϴ�.", gameObject);
        if (rb == null) Debug.LogError("Rigidbody ������Ʈ�� ã�� �� �����ϴ�.", gameObject);
        if (renderers.Length == 0) Debug.LogWarning("������ Renderer ������Ʈ�� �����ϴ�. ����/ǥ�� ����� �۵����� ���� �� �ֽ��ϴ�.", gameObject);
        if (colliders.Length == 0) Debug.LogWarning("������ Collider ������Ʈ�� �����ϴ�. �浹 ���� ����� �۵����� ���� �� �ֽ��ϴ�.", gameObject);

    }

    // ���� ü�� ��ȯ
    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    // �ִ� ü�� ��ȯ
    public float GetMaxHealth()
    {
        return maxHealth;
    }

    // ��� ���� ��ȯ
    public bool IsDead()
    {
        return isDead;
    }

    // �������� �޴� �Լ�
    public void TakeDamage(float amount, GameObject attacker)
    {
        // �̹� �׾��ų� ���� ���¸� �������� ���� ����
        if (isDead || isInvulnerable) return;

        currentHealth -= amount;
        string attackerName = (attacker != null && attacker != this.gameObject) ? attacker.name : "�ڱ� �ڽ� �Ǵ� ȯ��"; // ������ �̸� ����
        Debug.Log(gameObject.name + "��(��) " + attackerName + "�κ��� " + amount + "�� �������� �Ծ����ϴ�. ���� ü��: " + currentHealth.ToString("F0"));

        OnTakeDamage.Invoke(); // ������ �̺�Ʈ ȣ��

        // ü���� 0 ���ϰ� �Ǹ� �ı� ó��
        if (currentHealth <= 0)
        {
            currentHealth = 0; // ü���� ������ ���� �ʵ���
            Die(attacker);
        }
    }

    // ���� �ı� ó�� �Լ�
    void Die(GameObject attacker)
    {
        if (isDead) return; // �̹� ��� ó�� ���̸� �ߺ� ���� ����
        isDead = true; // ��� ���·� ����

        string attackerName = (attacker != null && attacker != this.gameObject) ? attacker.name : "���� �Ǵ� ȯ��";
        Debug.Log(gameObject.name + "��(��) " + attackerName + "�� ���� �ı��Ǿ����ϴ�!");

        // �ı� ȿ�� ���� (�������� �����Ǿ� �ִٸ�)
        if (destructionEffectPrefab != null)
        {
            GameObject effectInstance = Instantiate(destructionEffectPrefab, transform.position, transform.rotation);
            Destroy(effectInstance, effectDuration); // ���� �ð� �� �ı� ȿ�� �ڵ� ����
        }

        OnDie.Invoke(); // ��� �̺�Ʈ ȣ��

        // ���� ��Ʈ�� �� ���� ��Ȱ��ȭ
        if (vehicleController != null) vehicleController.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero; // �ӵ� �ʱ�ȭ
            rb.angularVelocity = Vector3.zero; // ���ӵ� �ʱ�ȭ
            rb.isKinematic = true; // ������ ������ �ߴ� (�ٸ� ������Ʈ�� ��ȣ�ۿ� ����)
        }

        // ���� ����� (������ �� �ݶ��̴� ��Ȱ��ȭ)
        SetVehicleVisualAndCollisionActive(false);


        // TODO: GameManager�� ��� �˸� (���ھ� ó��, ������ ���� �� ����)
        // ����: if (GameManager.Instance != null) GameManager.Instance.PlayerDied(this.gameObject, attacker);

        // ������ �ڷ�ƾ ����
        StartCoroutine(RespawnCoroutine());
    }

    // ������ ó�� �ڷ�ƾ
    IEnumerator RespawnCoroutine()
    {
        // ������ �ð���ŭ ���
        yield return new WaitForSeconds(respawnDelay);

        // ������ ��ġ ����
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            // ��ϵ� ���� ���� �� �������� �ϳ� ����
            Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            transform.position = randomSpawnPoint.position;
            transform.rotation = randomSpawnPoint.rotation;
        }
        else
        {
            // ���� ������ ������ ��� �α� ��� �� ���� ����(0,0,0)���� ������
            Debug.LogWarning("������ ����(Spawn Points)�� �������� �ʾҽ��ϴ�. ���� �������� �������մϴ�.", gameObject);
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }

        // ���� �ʱ�ȭ
        currentHealth = maxHealth;
        isDead = false; // ��� ���� ����

        // ���� �ٽ� ���̰� �ϰ� ���� �� ��Ʈ�� Ȱ��ȭ
        SetVehicleVisualAndCollisionActive(true);
        if (rb != null) rb.isKinematic = false; // ���� �ٽ� Ȱ��ȭ
        if (vehicleController != null) vehicleController.enabled = true; // ��Ʈ�ѷ� �ٽ� Ȱ��ȭ


        Debug.Log(gameObject.name + " ������ �Ϸ�!");
        OnRespawn.Invoke(); // ������ �̺�Ʈ ȣ��

        // ���� ���� ����
        StartCoroutine(InvulnerabilityCoroutine());
    }

    // ���� ���� ó�� �ڷ�ƾ
    IEnumerator InvulnerabilityCoroutine()
    {
        isInvulnerable = true; // ���� ���·� ����
        Debug.Log(gameObject.name + " ���� ���� ���� (" + invulnerabilityDuration + "��)");

        // ���� �ð� ���� ������ �����̴� �ð��� ȿ�� (���� ����)
        float endTime = Time.time + invulnerabilityDuration;
        bool rendererCurrentlyEnabled = true; // ������ ����/���� ���¸� ����ϱ� ���� ����
        float blinkInterval = 0.15f; // �����̴� ����

        while (Time.time < endTime)
        {
            if (renderers != null)
            {
                foreach (var r in renderers)
                {
                    if (r != null) r.enabled = rendererCurrentlyEnabled;
                }
            }
            rendererCurrentlyEnabled = !rendererCurrentlyEnabled; // ���� ����
            yield return new WaitForSeconds(blinkInterval);
        }

        // ���� ���� �� �������� Ȯ���� ���� ���·� ����
        SetVehicleVisualAndCollisionActive(true, true); // forceRenderersOn �÷��� ���

        isInvulnerable = false; // ���� ���� ����
        Debug.Log(gameObject.name + " ���� ���� ����");
    }

    // ���� ������ �� �ݶ��̴� Ȱ��/��Ȱ�� ��ƿ��Ƽ �Լ�
    // isActive: �������� Ȱ��/��Ȱ�� ����
    // forceRenderersOnAfterBlink: ���� ������ ���� �� �������� Ȯ���� �ѱ� ���� �÷���
    void SetVehicleVisualAndCollisionActive(bool isActive, bool forceRenderersOnAfterBlink = false)
    {
        if (renderers != null)
        {
            foreach (Renderer r in renderers)
            {
                if (r != null)
                {
                    // isActive�� true�� ���� ������ ���¸� ���
                    // forceRenderersOnAfterBlink�� true�̸� isActive�� true�� �� ������ �������� �� (������ ���� ��)
                    // �� �ܿ��� isActive ���¸� ����
                    r.enabled = isActive ? (forceRenderersOnAfterBlink || isActive) : false;
                }
            }
        }

        // �ݶ��̴��� ���/������ �ÿ��� ���� ���� (�����Ӱ� �����ϰ� isActive ���¸� ����)
        if (colliders != null && !forceRenderersOnAfterBlink) // ������ ���� �ÿ��� �ݶ��̴� ���� ���� ����
        {
            foreach (Collider c in colliders)
            {
                if (c != null) c.enabled = isActive;
            }
        }
    }

    // �׽�Ʈ�� ü�� ȸ�� �Լ� (���߿� ������ ������ Ȱ�� ����)
    public void Heal(float amount)
    {
        if (isDead) return; // ���� ���¸� ȸ�� �Ұ�
        currentHealth += amount;
        if (currentHealth > maxHealth) // �ִ� ü���� ���� �ʵ���
        {
            currentHealth = maxHealth;
        }
        Debug.Log(gameObject.name + "��(��) " + amount + "��ŭ ü���� ȸ���߽��ϴ�. ���� ü��: " + currentHealth.ToString("F0"));
    }
}