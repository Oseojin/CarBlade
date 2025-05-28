using UnityEngine;
using System.Collections; // �ڷ�ƾ ����� ���� �ʿ�
using System.Collections.Generic; // ����Ʈ ����� ���� �ʿ�

// �� ��ũ��Ʈ�� Hierarchy�� �� ������Ʈ�� ����� (��: "GameManager") �����մϴ�.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; } // �̱��� ����

    [Header("���� ����")]
    private int currentScore = 0;
    public int scorePerKill = 100;

    [Header("������ ����")]
    public float respawnDelay = 5f; // ���� �ı� �� ������������ �ð�
    public List<Transform> spawnPoints; // AI ������ �������� ��ġ��
    public GameObject aiCarPrefab; // AI ���� ������ (Inspector���� �Ҵ�)
    public Transform playerSpawnPoint; // �÷��̾� ������ ��ġ

    [Header("�÷��̾� ���� ����")]
    public float overturnCheckDelay = 1f; // ���� ���¸� üũ�ϴ� �ֱ�
    public float maxOverturnTime = 5f; // �� �ð� �̻� ������ ������ �ڵ� �ı�
    private float currentOverturnTime = 0f;
    private Coroutine overturnCoroutine;

    private UIManager uiManager;
    private VehicleHealth playerVehicleHealth; // �÷��̾� ������ VehicleHealth ����

    void Awake()
    {
        // �̱��� �ν��Ͻ� ����
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // ���� �ٲ� �����Ϸ��� �ּ� ����
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        uiManager = FindFirstObjectByType<UIManager>(); // ������ UIManager�� ã��
        if (uiManager == null)
        {
            Debug.LogError("UIManager�� ������ ã�� �� �����ϴ�!");
        }
        uiManager.UpdateScore(currentScore); // �ʱ� ���� UI ������Ʈ

        // �÷��̾� ���� ã�� �� ���� ���� ����
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player"); // �÷��̾� ������ "Player" �±� ���� �ʿ�
        if (playerObject != null)
        {
            playerVehicleHealth = playerObject.GetComponent<VehicleHealth>();
            if (playerVehicleHealth != null)
            {
                overturnCoroutine = StartCoroutine(CheckPlayerOverturnRoutine(playerObject.transform));
            }
            else
            {
                Debug.LogError("�÷��̾� �������� VehicleHealth ������Ʈ�� ã�� �� �����ϴ�.");
            }
        }
        else
        {
            Debug.LogError("�÷��̾� �±׸� ���� ������ ã�� �� �����ϴ�. �÷��̾� ������ �±׸� 'Player'�� �������ּ���.");
        }
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
        if (uiManager != null)
        {
            uiManager.UpdateScore(currentScore);
        }
        Debug.Log("���� ȹ��! ���� ����: " + currentScore);
    }

    // ������ �ı��Ǿ��� �� VehicleHealth���� ȣ��� �Լ�
    public void VehicleDestroyed(VehicleHealth destroyedVehicle)
    {
        if (destroyedVehicle == null) return;

        if (destroyedVehicle.isPlayer) // �ı��� ������ �÷��̾���
        {
            Debug.Log("�÷��̾� ���� �ı���. ������ �غ� ��...");
            if (overturnCoroutine != null) StopCoroutine(overturnCoroutine); // ���� üũ �ߴ�
            StartCoroutine(RespawnPlayerRoutine(destroyedVehicle.gameObject));
        }
        else // AI �����̶��
        {
            AddScore(scorePerKill); // AI�� �ı������� ���� �߰�
            Debug.Log("AI ���� �ı���. ������ �غ� ��...");
            StartCoroutine(RespawnAICarRoutine(destroyedVehicle.gameObject));
        }
    }

    IEnumerator RespawnAICarRoutine(GameObject carToRespawn)
    {
        // �ı��� AI ���� ������Ʈ�� ��� �ı� (�Ǵ� Ǯ�� �ý��ۿ����� ��ȯ)
        // Destroy(carToRespawn); // ���� VehicleHealth���� SetActive(false)�� �ߴٸ� �� ������ �ʿ� ����.
        // SetActive(false) �� ������ �� SetActive(true)�� ��Ȱ���ϴ� ���� �� ȿ������ �� ����.
        // ���⼭�� �����ϰ� �� AI�� �����ϴ� ������ �����մϴ�.

        yield return new WaitForSeconds(respawnDelay);

        if (aiCarPrefab != null && spawnPoints != null && spawnPoints.Count > 0)
        {
            int spawnIndex = Random.Range(0, spawnPoints.Count);
            Transform spawnPoint = spawnPoints[spawnIndex];
            GameObject newAICar = Instantiate(aiCarPrefab, spawnPoint.position, spawnPoint.rotation);
            // ���� ������ AI ������ �ʿ��� �ʱ�ȭ �۾��� �ִٸ� ���⼭ ����
            Debug.Log("���ο� AI ���� ������: " + newAICar.name);
        }
        else
        {
            Debug.LogError("AI ���� ������ �Ǵ� ���� ����Ʈ�� GameManager�� �������� �ʾҽ��ϴ�.");
        }
    }

    IEnumerator RespawnPlayerRoutine(GameObject playerObject)
    {
        // �÷��̾� ������Ʈ�� ��Ȱ��ȭ (VehicleHealth���� �̹� ó���ߴٸ� ���� ����)
        // playerObject.SetActive(false); 

        yield return new WaitForSeconds(respawnDelay);

        if (playerSpawnPoint != null && playerVehicleHealth != null)
        {
            playerObject.transform.position = playerSpawnPoint.position;
            playerObject.transform.rotation = playerSpawnPoint.rotation;
            playerObject.SetActive(true); // �÷��̾� ������Ʈ �ٽ� Ȱ��ȭ

            // Rigidbody �ӵ� �ʱ�ȭ
            Rigidbody rb = playerObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // ü�� �ʱ�ȭ (VehicleHealth�� �ʱ�ȭ �Լ��� �ִٸ� ȣ��, ���ٸ� ���� ����)
            // playerVehicleHealth.ResetHealth(); // ����: �̷� �Լ��� VehicleHealth�� �ִٰ� ����
            // �ӽ÷� ���� ���� (VehicleHealth ���� �ʿ�)
            // playerVehicleHealth.currentHealth = playerVehicleHealth.maxHealth; // ���� ������ ���� ����

            Debug.Log("�÷��̾� ���� ������ �Ϸ�.");
            // �÷��̾� ������ �� ���� ���� �ٽ� ����
            overturnCoroutine = StartCoroutine(CheckPlayerOverturnRoutine(playerObject.transform));
        }
        else
        {
            Debug.LogError("�÷��̾� ���� ����Ʈ �Ǵ� �÷��̾� VehicleHealth ������ �����ϴ�.");
        }
    }

    IEnumerator CheckPlayerOverturnRoutine(Transform playerTransform)
    {
        while (true)
        {
            yield return new WaitForSeconds(overturnCheckDelay);

            if (playerTransform == null || !playerTransform.gameObject.activeInHierarchy)
            {
                // �÷��̾ �ı��ǰų� ��Ȱ��ȭ�Ǹ� �ڷ�ƾ �ߴ�
                yield break;
            }

            // ������ �󸶳� ���������� Ȯ�� (���� ���� ���Ϳ� ���� �Ʒ��� ���� ������ ����)
            // transform.up�� ������ ���� ���� ����
            // Vector3.down�� ������ �Ʒ��� ����
            // �� ���Ͱ� ���� ���� ������ ����Ű�� (������ 1�� ������) ������ ������ ��
            if (Vector3.Dot(playerTransform.up, Vector3.down) > 0.8f) // 0.8f�� �Ӱ谪, ���� ����
            {
                currentOverturnTime += overturnCheckDelay;
                if (currentOverturnTime >= maxOverturnTime)
                {
                    Debug.Log("�÷��̾� ������ �ʹ� ���� ������ �־� �ڵ� �ı��˴ϴ�.");
                    if (playerVehicleHealth != null)
                    {
                        // TakeDamage ��� ��� �ı� ������ ȣ���ϰų�, �ſ� ū �������� �༭ �ı�
                        playerVehicleHealth.TakeDamage(playerVehicleHealth.maxHealth * 2); // Ȯ���� �ı�
                    }
                    currentOverturnTime = 0f;
                    yield break; // �ı� �� �� �ڷ�ƾ�� ���� (������ �� ���� ���۵�)
                }
            }
            else
            {
                currentOverturnTime = 0f; // ���� ���¸� Ÿ�̸� �ʱ�ȭ
            }
        }
    }
}
