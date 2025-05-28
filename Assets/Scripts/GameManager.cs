using UnityEngine;
using System.Collections; // 코루틴 사용을 위해 필요
using System.Collections.Generic; // 리스트 사용을 위해 필요

// 이 스크립트는 Hierarchy에 빈 오브젝트를 만들고 (예: "GameManager") 연결합니다.
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; } // 싱글턴 패턴

    [Header("점수 설정")]
    private int currentScore = 0;
    public int scorePerKill = 100;

    [Header("리스폰 설정")]
    public float respawnDelay = 5f; // 차량 파괴 후 리스폰까지의 시간
    public List<Transform> spawnPoints; // AI 차량이 리스폰될 위치들
    public GameObject aiCarPrefab; // AI 차량 프리팹 (Inspector에서 할당)
    public Transform playerSpawnPoint; // 플레이어 리스폰 위치

    [Header("플레이어 전복 감지")]
    public float overturnCheckDelay = 1f; // 전복 상태를 체크하는 주기
    public float maxOverturnTime = 5f; // 이 시간 이상 뒤집혀 있으면 자동 파괴
    private float currentOverturnTime = 0f;
    private Coroutine overturnCoroutine;

    private UIManager uiManager;
    private VehicleHealth playerVehicleHealth; // 플레이어 차량의 VehicleHealth 참조

    void Awake()
    {
        // 싱글턴 인스턴스 설정
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 유지하려면 주석 해제
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        uiManager = FindFirstObjectByType<UIManager>(); // 씬에서 UIManager를 찾음
        if (uiManager == null)
        {
            Debug.LogError("UIManager를 씬에서 찾을 수 없습니다!");
        }
        uiManager.UpdateScore(currentScore); // 초기 점수 UI 업데이트

        // 플레이어 차량 찾기 및 전복 감지 시작
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player"); // 플레이어 차량에 "Player" 태그 설정 필요
        if (playerObject != null)
        {
            playerVehicleHealth = playerObject.GetComponent<VehicleHealth>();
            if (playerVehicleHealth != null)
            {
                overturnCoroutine = StartCoroutine(CheckPlayerOverturnRoutine(playerObject.transform));
            }
            else
            {
                Debug.LogError("플레이어 차량에서 VehicleHealth 컴포넌트를 찾을 수 없습니다.");
            }
        }
        else
        {
            Debug.LogError("플레이어 태그를 가진 차량을 찾을 수 없습니다. 플레이어 차량의 태그를 'Player'로 설정해주세요.");
        }
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
        if (uiManager != null)
        {
            uiManager.UpdateScore(currentScore);
        }
        Debug.Log("점수 획득! 현재 점수: " + currentScore);
    }

    // 차량이 파괴되었을 때 VehicleHealth에서 호출될 함수
    public void VehicleDestroyed(VehicleHealth destroyedVehicle)
    {
        if (destroyedVehicle == null) return;

        if (destroyedVehicle.isPlayer) // 파괴된 차량이 플레이어라면
        {
            Debug.Log("플레이어 차량 파괴됨. 리스폰 준비 중...");
            if (overturnCoroutine != null) StopCoroutine(overturnCoroutine); // 전복 체크 중단
            StartCoroutine(RespawnPlayerRoutine(destroyedVehicle.gameObject));
        }
        else // AI 차량이라면
        {
            AddScore(scorePerKill); // AI를 파괴했으니 점수 추가
            Debug.Log("AI 차량 파괴됨. 리스폰 준비 중...");
            StartCoroutine(RespawnAICarRoutine(destroyedVehicle.gameObject));
        }
    }

    IEnumerator RespawnAICarRoutine(GameObject carToRespawn)
    {
        // 파괴된 AI 차량 오브젝트를 즉시 파괴 (또는 풀링 시스템에서는 반환)
        // Destroy(carToRespawn); // 만약 VehicleHealth에서 SetActive(false)만 했다면 이 라인은 필요 없음.
        // SetActive(false) 후 리스폰 시 SetActive(true)로 재활용하는 것이 더 효율적일 수 있음.
        // 여기서는 간단하게 새 AI를 생성하는 것으로 가정합니다.

        yield return new WaitForSeconds(respawnDelay);

        if (aiCarPrefab != null && spawnPoints != null && spawnPoints.Count > 0)
        {
            int spawnIndex = Random.Range(0, spawnPoints.Count);
            Transform spawnPoint = spawnPoints[spawnIndex];
            GameObject newAICar = Instantiate(aiCarPrefab, spawnPoint.position, spawnPoint.rotation);
            // 새로 생성된 AI 차량에 필요한 초기화 작업이 있다면 여기서 수행
            Debug.Log("새로운 AI 차량 리스폰: " + newAICar.name);
        }
        else
        {
            Debug.LogError("AI 차량 프리팹 또는 스폰 포인트가 GameManager에 설정되지 않았습니다.");
        }
    }

    IEnumerator RespawnPlayerRoutine(GameObject playerObject)
    {
        // 플레이어 오브젝트를 비활성화 (VehicleHealth에서 이미 처리했다면 생략 가능)
        // playerObject.SetActive(false); 

        yield return new WaitForSeconds(respawnDelay);

        if (playerSpawnPoint != null && playerVehicleHealth != null)
        {
            playerObject.transform.position = playerSpawnPoint.position;
            playerObject.transform.rotation = playerSpawnPoint.rotation;
            playerObject.SetActive(true); // 플레이어 오브젝트 다시 활성화

            // Rigidbody 속도 초기화
            Rigidbody rb = playerObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // 체력 초기화 (VehicleHealth에 초기화 함수가 있다면 호출, 없다면 직접 설정)
            // playerVehicleHealth.ResetHealth(); // 예시: 이런 함수가 VehicleHealth에 있다고 가정
            // 임시로 직접 접근 (VehicleHealth 수정 필요)
            // playerVehicleHealth.currentHealth = playerVehicleHealth.maxHealth; // 직접 접근은 좋지 않음

            Debug.Log("플레이어 차량 리스폰 완료.");
            // 플레이어 리스폰 후 전복 감지 다시 시작
            overturnCoroutine = StartCoroutine(CheckPlayerOverturnRoutine(playerObject.transform));
        }
        else
        {
            Debug.LogError("플레이어 스폰 포인트 또는 플레이어 VehicleHealth 참조가 없습니다.");
        }
    }

    IEnumerator CheckPlayerOverturnRoutine(Transform playerTransform)
    {
        while (true)
        {
            yield return new WaitForSeconds(overturnCheckDelay);

            if (playerTransform == null || !playerTransform.gameObject.activeInHierarchy)
            {
                // 플레이어가 파괴되거나 비활성화되면 코루틴 중단
                yield break;
            }

            // 차량이 얼마나 뒤집혔는지 확인 (위쪽 방향 벡터와 월드 아래쪽 방향 벡터의 내적)
            // transform.up은 차량의 로컬 위쪽 방향
            // Vector3.down은 월드의 아래쪽 방향
            // 두 벡터가 거의 같은 방향을 가리키면 (내적이 1에 가까우면) 완전히 뒤집힌 것
            if (Vector3.Dot(playerTransform.up, Vector3.down) > 0.8f) // 0.8f는 임계값, 조절 가능
            {
                currentOverturnTime += overturnCheckDelay;
                if (currentOverturnTime >= maxOverturnTime)
                {
                    Debug.Log("플레이어 차량이 너무 오래 뒤집혀 있어 자동 파괴됩니다.");
                    if (playerVehicleHealth != null)
                    {
                        // TakeDamage 대신 즉시 파괴 로직을 호출하거나, 매우 큰 데미지를 줘서 파괴
                        playerVehicleHealth.TakeDamage(playerVehicleHealth.maxHealth * 2); // 확실히 파괴
                    }
                    currentOverturnTime = 0f;
                    yield break; // 파괴 후 이 코루틴은 종료 (리스폰 후 새로 시작됨)
                }
            }
            else
            {
                currentOverturnTime = 0f; // 정상 상태면 타이머 초기화
            }
        }
    }
}
