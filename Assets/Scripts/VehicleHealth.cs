using UnityEngine;

// 이 스크립트는 플레이어, AI 등 모든 차량의 루트 오브젝트에 연결합니다.
public class VehicleHealth : MonoBehaviour
{
    [Header("차량 체력")]
    public float maxHealth = 100f;
    private float currentHealth; // 이제 private으로 변경

    // 이 차량이 플레이어 차량인지 식별하는 플래그
    public bool isPlayer = false;

    void Start()
    {
        ResetHealth(); // 시작 시 체력 초기화
    }

    public void TakeDamage(float damageAmount)
    {
        if (currentHealth <= 0 && gameObject.activeInHierarchy) return; // 이미 처리 중이거나 파괴된 상태면 중복 호출 방지

        currentHealth -= damageAmount;
        Debug.Log(gameObject.name + "가 " + damageAmount + "의 데미지를 입었습니다. 현재 체력: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log(gameObject.name + "가 파괴되었습니다!");
        gameObject.SetActive(false); // 먼저 비활성화

        // GameManager에 알림
        if (GameManager.Instance != null)
        {
            GameManager.Instance.VehicleDestroyed(this);
        }
        else
        {
            Debug.LogWarning("GameManager 인스턴스를 찾을 수 없습니다.");
        }
    }

    // 외부에서 현재 체력을 확인해야 할 경우를 위한 함수
    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    // 체력을 최대로 리셋하는 함수 (리스폰 시 사용)
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        // 필요하다면 여기에 차량의 다른 상태(예: 외형 파손 상태) 초기화 로직 추가
    }

    // 오브젝트가 다시 활성화될 때 체력을 리셋 (리스폰 시 활용)
    void OnEnable()
    {
        ResetHealth();
        // 만약 Rigidbody가 있다면 속도도 초기화하는 것이 좋음
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
