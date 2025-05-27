using UnityEngine;

public class Blade : MonoBehaviour
{
    [Header("블레이드 설정")]
    public float baseDamage = 10f; // 블레이드의 기본 데미지 (속도/각속도 미적용)
    public string targetTag = "PlayerVehicle"; // 공격할 대상의 태그 (차량 태그)

    private Rigidbody vehicleRb; // 블레이드를 장착한 차량의 Rigidbody
    private VehicleController vehicleController; // 차량 컨트롤러 (속도, 각속도 접근용)
    private VehicleHealth myVehicleHealth; // 자신의 차량 체력 (자해 방지용)

    void Start()
    {
        // 부모 오브젝트에서 필요한 컴포넌트들을 찾습니다.
        // 이 블레이드는 차량 오브젝트의 자식으로 존재해야 합니다.
        Transform parentVehicle = transform.root; // 최상위 부모를 차량으로 가정
        if (parentVehicle != null)
        {
            vehicleRb = parentVehicle.GetComponent<Rigidbody>();
            vehicleController = parentVehicle.GetComponent<VehicleController>();
            myVehicleHealth = parentVehicle.GetComponent<VehicleHealth>();
        }

        if (vehicleRb == null)
        {
            Debug.LogError(gameObject.name + ": 블레이드를 장착한 차량의 Rigidbody를 찾을 수 없습니다!", this.gameObject);
        }
        if (vehicleController == null)
        {
            Debug.LogError(gameObject.name + ": 블레이드를 장착한 차량의 VehicleController를 찾을 수 없습니다!", this.gameObject);
        }
        if (myVehicleHealth == null)
        {
            Debug.LogError(gameObject.name + ": 블레이드를 장착한 차량의 VehicleHealth를 찾을 수 없습니다!", this.gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // --- 디버깅 로그 추가 ---
        Debug.Log(transform.root.name + "의 블레이드가 " + collision.gameObject.name + "와(과) OnCollisionEnter 발생!");
        // -----------------------

        // 충돌한 오브젝트가 공격 대상 태그를 가지고 있는지 확인
        if (collision.gameObject.CompareTag(targetTag))
        {
            // --- 디버깅 로그 추가 ---
            Debug.Log(collision.gameObject.name + "의 태그가 " + targetTag + "와 일치합니다.");
            // -----------------------

            VehicleHealth targetHealth = collision.gameObject.GetComponent<VehicleHealth>();

            // 상대방이 VehicleHealth 컴포넌트를 가지고 있고, 자기 자신이 아니라면
            if (targetHealth != null && targetHealth != myVehicleHealth)
            {
                // --- 디버깅 로그 추가 ---
                Debug.Log(collision.gameObject.name + "는 VehicleHealth를 가지고 있고, 내 자신이 아닙니다. 데미지 계산 시작.");
                // -----------------------

                // --- 데미지 계산 로직 (기획 문서 기반) ---
                float currentSpeed = 0f;
                float currentAngularVelocity = 0f; // 현재는 각속도 미구현, 추후 VehicleController에서 가져와야 함

                if (vehicleRb != null)
                {
                    currentSpeed = vehicleRb.linearVelocity.magnitude; // 현재 차량의 속도
                    // TODO: 각속도 가져오기 (VehicleController에 드리프트 상태 또는 각속도 반환 함수 필요)
                    // currentAngularVelocity = vehicleController.GetCurrentAngularVelocity();
                }

                // 기획 문서의 데미지 공식 적용 (최대 속도/각속도는 임시값, 튜닝 필요)
                float maxSpeed = 50f; // 예시 최대 속도 (m/s)
                // float maxAngularVelocity = 10f; // 예시 최대 각속도 (rad/s)

                float speedFactor = 1 + (currentSpeed / maxSpeed);
                // float angularVelocityFactor = 1 + (currentAngularVelocity / maxAngularVelocity); // 각속도 구현 후 활성화

                float calculatedDamage = baseDamage * speedFactor; // * angularVelocityFactor; // 각속도 구현 후 활성화

                Debug.Log(transform.root.name + "의 블레이드가 " + collision.gameObject.name + "에게 충돌! 속도: " + currentSpeed.ToString("F2") + ", 계산된 데미지: " + calculatedDamage.ToString("F2"));

                // 상대방에게 데미지 전달 (공격자는 이 블레이드의 차량)
                targetHealth.TakeDamage(calculatedDamage, transform.root.gameObject);
            }
            else if (targetHealth == null)
            {
                Debug.LogWarning(collision.gameObject.name + "에는 VehicleHealth 컴포넌트가 없습니다.");
            }
            else if (targetHealth == myVehicleHealth)
            {
                Debug.LogWarning("자신의 차량(" + collision.gameObject.name + ")과 충돌했습니다. (자해 방지)");
            }
        }
        else
        {
            Debug.Log(collision.gameObject.name + "의 태그(" + collision.gameObject.tag + ")가 " + targetTag + "와 일치하지 않습니다.");
        }
        // TODO: 블레이드 클래시 로직 (상대방도 블레이드이고, 서로 정면 충돌 시)
    }
}
