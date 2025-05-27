using UnityEngine;
using UnityEngine.Events; // UnityEvent 사용
using System.Collections; // 코루틴 사용

public class VehicleHealth : MonoBehaviour
{
    [Header("체력 설정")]
    public float maxHealth = 100f; // 최대 체력
    private float currentHealth; // 현재 체력
    private bool isDead = false; // 사망 상태 플래그 (중복 사망 처리 방지용)

    [Header("파괴 효과")]
    public GameObject destructionEffectPrefab; // 파괴 시 생성될 파티클 효과 프리팹
    public float effectDuration = 2f; // 파괴 효과가 지속될 시간 (이후 자동 파괴)

    [Header("리스폰 설정")]
    public float respawnDelay = 5f; // 리스폰까지 걸리는 시간 (초)
    public float invulnerabilityDuration = 2f; // 리스폰 후 무적 지속 시간 (초)
    [Tooltip("차량이 리스폰될 수 있는 위치들의 배열입니다. Inspector에서 설정해주세요.")]
    public Transform[] spawnPoints; // 스폰 지점들 (Transform 배열)
    private bool isInvulnerable = false; // 현재 무적 상태인지 여부

    [Header("이벤트")]
    public UnityEvent OnTakeDamage; // 데미지를 입었을 때 호출될 UnityEvent
    public UnityEvent OnDie; // 차량이 파괴되었을 때 호출될 UnityEvent
    public UnityEvent OnRespawn; // 차량이 리스폰되었을 때 호출될 UnityEvent

    // 차량의 시각적/물리적 요소를 제어하기 위한 컴포넌트 참조
    private Renderer[] renderers; // 차량 및 자식 오브젝트의 모든 Renderer 컴포넌트
    private Collider[] colliders; // 차량 및 자식 오브젝트의 모든 Collider 컴포넌트
    private VehicleController vehicleController; // 차량 컨트롤러 스크립트 참조
    private Rigidbody rb; // 차량의 Rigidbody 컴포넌트


    void Awake()
    {
        currentHealth = maxHealth; // 시작 시 체력을 최대로 설정

        // 차량의 모든 렌더러와 콜라이더를 가져옴 (비활성화된 자식 포함하여 검색)
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
        vehicleController = GetComponent<VehicleController>();
        rb = GetComponent<Rigidbody>();

        // 필수 컴포넌트 확인
        if (vehicleController == null) Debug.LogError("VehicleController 컴포넌트를 찾을 수 없습니다.", gameObject);
        if (rb == null) Debug.LogError("Rigidbody 컴포넌트를 찾을 수 없습니다.", gameObject);
        if (renderers.Length == 0) Debug.LogWarning("차량에 Renderer 컴포넌트가 없습니다. 숨김/표시 기능이 작동하지 않을 수 있습니다.", gameObject);
        if (colliders.Length == 0) Debug.LogWarning("차량에 Collider 컴포넌트가 없습니다. 충돌 관련 기능이 작동하지 않을 수 있습니다.", gameObject);

    }

    // 현재 체력 반환
    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    // 최대 체력 반환
    public float GetMaxHealth()
    {
        return maxHealth;
    }

    // 사망 상태 반환
    public bool IsDead()
    {
        return isDead;
    }

    // 데미지를 받는 함수
    public void TakeDamage(float amount, GameObject attacker)
    {
        // 이미 죽었거나 무적 상태면 데미지를 받지 않음
        if (isDead || isInvulnerable) return;

        currentHealth -= amount;
        string attackerName = (attacker != null && attacker != this.gameObject) ? attacker.name : "자기 자신 또는 환경"; // 공격자 이름 설정
        Debug.Log(gameObject.name + "이(가) " + attackerName + "로부터 " + amount + "의 데미지를 입었습니다. 현재 체력: " + currentHealth.ToString("F0"));

        OnTakeDamage.Invoke(); // 데미지 이벤트 호출

        // 체력이 0 이하가 되면 파괴 처리
        if (currentHealth <= 0)
        {
            currentHealth = 0; // 체력이 음수가 되지 않도록
            Die(attacker);
        }
    }

    // 차량 파괴 처리 함수
    void Die(GameObject attacker)
    {
        if (isDead) return; // 이미 사망 처리 중이면 중복 실행 방지
        isDead = true; // 사망 상태로 변경

        string attackerName = (attacker != null && attacker != this.gameObject) ? attacker.name : "자폭 또는 환경";
        Debug.Log(gameObject.name + "이(가) " + attackerName + "에 의해 파괴되었습니다!");

        // 파괴 효과 생성 (프리팹이 지정되어 있다면)
        if (destructionEffectPrefab != null)
        {
            GameObject effectInstance = Instantiate(destructionEffectPrefab, transform.position, transform.rotation);
            Destroy(effectInstance, effectDuration); // 일정 시간 후 파괴 효과 자동 제거
        }

        OnDie.Invoke(); // 사망 이벤트 호출

        // 차량 컨트롤 및 물리 비활성화
        if (vehicleController != null) vehicleController.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero; // 속도 초기화
            rb.angularVelocity = Vector3.zero; // 각속도 초기화
            rb.isKinematic = true; // 물리적 움직임 중단 (다른 오브젝트와 상호작용 안함)
        }

        // 차량 숨기기 (렌더러 및 콜라이더 비활성화)
        SetVehicleVisualAndCollisionActive(false);


        // TODO: GameManager에 사망 알림 (스코어 처리, 리스폰 로직 등 연동)
        // 예시: if (GameManager.Instance != null) GameManager.Instance.PlayerDied(this.gameObject, attacker);

        // 리스폰 코루틴 시작
        StartCoroutine(RespawnCoroutine());
    }

    // 리스폰 처리 코루틴
    IEnumerator RespawnCoroutine()
    {
        // 설정된 시간만큼 대기
        yield return new WaitForSeconds(respawnDelay);

        // 리스폰 위치 설정
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            // 등록된 스폰 지점 중 무작위로 하나 선택
            Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            transform.position = randomSpawnPoint.position;
            transform.rotation = randomSpawnPoint.rotation;
        }
        else
        {
            // 스폰 지점이 없으면 경고 로그 출력 후 월드 원점(0,0,0)에서 리스폰
            Debug.LogWarning("리스폰 지점(Spawn Points)이 설정되지 않았습니다. 월드 원점에서 리스폰합니다.", gameObject);
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }

        // 상태 초기화
        currentHealth = maxHealth;
        isDead = false; // 사망 상태 해제

        // 차량 다시 보이게 하고 물리 및 컨트롤 활성화
        SetVehicleVisualAndCollisionActive(true);
        if (rb != null) rb.isKinematic = false; // 물리 다시 활성화
        if (vehicleController != null) vehicleController.enabled = true; // 컨트롤러 다시 활성화


        Debug.Log(gameObject.name + " 리스폰 완료!");
        OnRespawn.Invoke(); // 리스폰 이벤트 호출

        // 무적 상태 시작
        StartCoroutine(InvulnerabilityCoroutine());
    }

    // 무적 상태 처리 코루틴
    IEnumerator InvulnerabilityCoroutine()
    {
        isInvulnerable = true; // 무적 상태로 변경
        Debug.Log(gameObject.name + " 무적 상태 시작 (" + invulnerabilityDuration + "초)");

        // 무적 시간 동안 차량을 깜빡이는 시각적 효과 (선택 사항)
        float endTime = Time.time + invulnerabilityDuration;
        bool rendererCurrentlyEnabled = true; // 렌더러 켜짐/꺼짐 상태를 토글하기 위한 변수
        float blinkInterval = 0.15f; // 깜빡이는 간격

        while (Time.time < endTime)
        {
            if (renderers != null)
            {
                foreach (var r in renderers)
                {
                    if (r != null) r.enabled = rendererCurrentlyEnabled;
                }
            }
            rendererCurrentlyEnabled = !rendererCurrentlyEnabled; // 상태 반전
            yield return new WaitForSeconds(blinkInterval);
        }

        // 무적 종료 후 렌더러를 확실히 켜진 상태로 만듦
        SetVehicleVisualAndCollisionActive(true, true); // forceRenderersOn 플래그 사용

        isInvulnerable = false; // 무적 상태 해제
        Debug.Log(gameObject.name + " 무적 상태 종료");
    }

    // 차량 렌더러 및 콜라이더 활성/비활성 유틸리티 함수
    // isActive: 전반적인 활성/비활성 상태
    // forceRenderersOnAfterBlink: 무적 깜빡임 종료 시 렌더러만 확실히 켜기 위한 플래그
    void SetVehicleVisualAndCollisionActive(bool isActive, bool forceRenderersOnAfterBlink = false)
    {
        if (renderers != null)
        {
            foreach (Renderer r in renderers)
            {
                if (r != null)
                {
                    // isActive가 true일 때만 렌더러 상태를 고려
                    // forceRenderersOnAfterBlink가 true이면 isActive가 true일 때 무조건 렌더러를 켬 (깜빡임 종료 시)
                    // 그 외에는 isActive 상태를 따름
                    r.enabled = isActive ? (forceRenderersOnAfterBlink || isActive) : false;
                }
            }
        }

        // 콜라이더는 사망/리스폰 시에만 상태 변경 (깜빡임과 무관하게 isActive 상태를 따름)
        if (colliders != null && !forceRenderersOnAfterBlink) // 깜빡임 종료 시에는 콜라이더 상태 변경 안함
        {
            foreach (Collider c in colliders)
            {
                if (c != null) c.enabled = isActive;
            }
        }
    }

    // 테스트용 체력 회복 함수 (나중에 아이템 등으로 활용 가능)
    public void Heal(float amount)
    {
        if (isDead) return; // 죽은 상태면 회복 불가
        currentHealth += amount;
        if (currentHealth > maxHealth) // 최대 체력을 넘지 않도록
        {
            currentHealth = maxHealth;
        }
        Debug.Log(gameObject.name + "이(가) " + amount + "만큼 체력을 회복했습니다. 현재 체력: " + currentHealth.ToString("F0"));
    }
}