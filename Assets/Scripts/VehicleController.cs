using UnityEngine;
using UnityEngine.InputSystem;

public class VehicleController : MonoBehaviour
{
    // --- 기존 변수들 ---
    [Header("차량 설정")]
    public float motorForce = 2000f; // 차량의 기본 가속력
    public float brakeForce = 3000f; // 브레이크 힘
    public float maxSteerAngle = 30f; // 최대 조향 각도
    public bool isFourWheelDrive = true; // 4륜 구동 여부

    [Header("안정성 설정")]
    public float centerOfMassYOffset = -0.5f; // 무게 중심의 Y축 오프셋
    public float maxAngularVelocity = 5f; // Rigidbody의 최대 각속도

    [Header("전투 설정")]
    public Collider bladeCollider; // 차량에 부착된 블레이드의 Collider
    public float baseDamage = 10f; // 블레이드의 기본 공격력
    public string targetTag = "PlayerVehicle"; // 공격 대상으로 인식할 태그
    [Tooltip("데미지 계산 시 사용될 최대 유효 상대 속도. 이 값을 넘는 상대 속도에서는 데미지 증가폭이 줄어들거나 고정될 수 있습니다.")]
    public float maxEffectiveRelativeSpeed = 70f; // 데미지 계산용 최대 유효 상대 속도

    [Header("뒤집힘 자폭 설정")]
    [Tooltip("차량의 transform.up.y 값이 이 값 미만이면 뒤집힌 것으로 간주합니다.")]
    public float flipDetectionThreshold = 0.3f; // 뒤집힘 감지를 위한 Y축 방향 임계값
    [Tooltip("뒤집힌 상태가 이 시간(초) 이상 지속되면 차량이 자폭합니다.")]
    public float maxFlipDuration = 10f; // 최대 뒤집힘 지속 시간 (이후 자폭)
    private float currentFlipTime = 0f; // 현재 뒤집힌 상태로 지속된 시간

    [Header("휠 콜라이더 참조")]
    public WheelCollider frontLeftWheelCollider;
    public WheelCollider frontRightWheelCollider;
    public WheelCollider rearLeftWheelCollider;
    public WheelCollider rearRightWheelCollider;

    [Header("휠 트랜스폼 참조 (시각적 업데이트용)")]
    public Transform frontLeftWheelTransform;
    public Transform frontRightWheelTransform;
    public Transform rearLeftWheelTransform;
    public Transform rearRightWheelTransform;

    // --- 새로운 설정 변수들 ---
    [Header("부스터 설정")]
    public float maxBoostGauge = 100f; // 최대 부스터 게이지
    public float boostForce = 4000f; // 부스터 사용 시 추가되는 힘
    public float boostConsumptionRate = 25f; // 초당 부스터 게이지 소모량
    public float highSpeedThreshold = 40f; // 고속 주행으로 인정될 속도 (m/s)
    public float highSpeedChargeRate = 2f; // 고속 주행 시 초당 게이지 충전량

    [Header("드리프트 설정")]
    public float driftChargeRate = 30f; // 드리프트 시 초당 게이지 충전량
    [Tooltip("드리프트 시 적용될 뒷바퀴의 측면 마찰력 계수(Stiffness). 낮을수록 잘 미끄러집니다.")]
    public float driftSidewaysStiffness = 0.5f; // 드리프트 시 측면 마찰 계수

    // --- 내부 변수들 ---
    private Rigidbody rb; // 차량의 Rigidbody 컴포넌트
    private VehicleHealth myVehicleHealth; // 차량 자신의 VehicleHealth 컴포넌트
    private float horizontalInput; // 좌우 입력 값
    private float verticalInput; // 앞뒤 입력 값
    private bool isBraking; // 브레이크 입력 여부

    // --- 새로운 내부 변수들 ---
    private float currentBoostGauge; // 현재 부스터 게이지
    private bool isDrifting; // 현재 드리프트 중인지 여부
    private bool isBoosting; // 현재 부스터 사용 중인지 여부
    private WheelFrictionCurve defaultRearSidewaysFriction; // 뒷바퀴의 기본 측면 마찰력 저장용

    // --- Input System 관련 변수 ---
    public InputActionAsset inputActions; // 연결할 Input Actions Asset
    private InputAction moveAction; // 이동 액션 (Vector2)
    private InputAction brakeAction; // 브레이크 액션 (Button)
    private InputAction driftAction; // 드리프트 액션 (Button)
    private InputAction boostAction; // 부스터 액션 (Button)

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        myVehicleHealth = GetComponent<VehicleHealth>();

        if (myVehicleHealth == null) Debug.LogError("VehicleHealth 컴포넌트를 찾을 수 없습니다!", gameObject);
        if (rb == null) Debug.LogError("Rigidbody 컴포넌트를 찾을 수 없습니다!", gameObject);
        else
        {
            rb.maxAngularVelocity = maxAngularVelocity;
            rb.centerOfMass += new Vector3(0, centerOfMassYOffset, 0);
        }

        // 드리프트 종료 시 복원을 위해 뒷바퀴의 기본 측면 마찰력 값을 저장
        // WheelCollider가 null이 아닐 경우에만 접근
        if (rearLeftWheelCollider != null)
        {
            defaultRearSidewaysFriction = rearLeftWheelCollider.sidewaysFriction;
        }
        else
        {
            Debug.LogWarning("Rear Left Wheel Collider가 설정되지 않았습니다. 드리프트 기능이 정상 작동하지 않을 수 있습니다.", gameObject);
        }
        // 오른쪽 뒷바퀴도 동일하게 처리할 수 있으나, 보통 좌우 대칭이므로 하나만 저장해도 무방할 수 있음.
        // 필요시 rearRightWheelCollider에 대해서도 default 값을 저장.

        // Input Action 초기화
        if (inputActions != null)
        {
            var playerActionMap = inputActions.FindActionMap("Player"); // "Player"는 사용자가 설정한 액션 맵 이름
            if (playerActionMap != null)
            {
                moveAction = playerActionMap.FindAction("Move");
                brakeAction = playerActionMap.FindAction("Brake");
                driftAction = playerActionMap.FindAction("Drift"); // "Drift" 액션 찾기
                boostAction = playerActionMap.FindAction("Boost"); // "Boost" 액션 찾기

                EnableAction(moveAction, "Move");
                EnableAction(brakeAction, "Brake");
                EnableAction(driftAction, "Drift");
                EnableAction(boostAction, "Boost");
            }
            else
            {
                Debug.LogError("'Player' 액션 맵을 찾을 수 없습니다. Input Actions Asset 설정을 확인해주세요.", gameObject);
            }
        }
        else
        {
            Debug.LogError("Input Actions Asset이 연결되지 않았습니다.", gameObject);
        }
    }

    // 액션 활성화 보조 함수
    void EnableAction(InputAction action, string actionName)
    {
        if (action != null)
        {
            action.Enable();
        }
        else
        {
            Debug.LogError($"'{actionName}' 액션을 찾을 수 없습니다. Input Actions Asset에서 해당 이름의 액션이 존재하는지 확인해주세요.", gameObject);
        }
    }

    void Update()
    {
        GetInput(); // 입력 처리
        CheckIfFlipped(); // 매 프레임 뒤집힘 상태 체크
    }

    void FixedUpdate()
    {
        // 물리 업데이트는 FixedUpdate에서 처리
        HandleMotor();
        HandleSteering();
        HandleDrift(); // 드리프트 물리 및 게이지 충전 처리
        HandleBoost(); // 부스터 물리 및 게이지 소모/충전 처리
        UpdateWheels();
    }

    // --- 주요 로직 함수들 ---

    // 입력 값 받아오기
    void GetInput()
    {
        // 액션이 null일 경우를 대비한 방어 코드
        if (moveAction == null || brakeAction == null || driftAction == null || boostAction == null) return;

        Vector2 moveVector = moveAction.ReadValue<Vector2>();
        horizontalInput = moveVector.x; // 좌/우 입력 (-1 ~ 1)
        verticalInput = moveVector.y;   // 앞/뒤 입력 (-1 ~ 1)
        isBraking = brakeAction.IsPressed(); // 브레이크 버튼 눌림 여부
        isDrifting = driftAction.IsPressed(); // 드리프트 버튼 입력
        isBoosting = boostAction.IsPressed(); // 부스터 버튼 입력
    }

    // 드리프트 처리
    void HandleDrift()
    {
        if (rearLeftWheelCollider == null || rearRightWheelCollider == null) return; // 뒷바퀴 콜라이더 없으면 실행 안함

        if (isDrifting)
        {
            // 드리프트 중일 때 뒷바퀴의 측면 마찰력을 낮춰 미끄러지게 함
            WheelFrictionCurve driftFriction = defaultRearSidewaysFriction; // 기본값 복사
            driftFriction.stiffness = driftSidewaysStiffness; // 설정된 값으로 마찰 계수(Stiffness) 변경

            rearLeftWheelCollider.sidewaysFriction = driftFriction;
            rearRightWheelCollider.sidewaysFriction = driftFriction; // 양쪽 뒷바퀴 모두 적용

            // 부스터 게이지 충전
            AddToBoostGauge(driftChargeRate * Time.fixedDeltaTime);
        }
        else
        {
            // 드리프트가 아닐 때 원래 마찰력으로 복원
            rearLeftWheelCollider.sidewaysFriction = defaultRearSidewaysFriction;
            rearRightWheelCollider.sidewaysFriction = defaultRearSidewaysFriction;
        }
    }

    // 부스터 처리
    void HandleBoost()
    {
        // 부스터 사용 로직
        if (isBoosting && currentBoostGauge > 0)
        {
            if (rb != null)
            {
                // 차량의 전방으로 부스터 힘을 가함 (ForceMode.Acceleration은 질량에 관계없이 동일한 가속도)
                rb.AddForce(transform.forward * boostForce, ForceMode.Acceleration);
            }
            // 부스터 게이지 소모
            currentBoostGauge -= boostConsumptionRate * Time.fixedDeltaTime;
        }
        // 고속 주행 시 게이지 자동 충전 로직 (부스터 사용 중이 아닐 때만)
        else if (rb != null && rb.linearVelocity.magnitude > highSpeedThreshold)
        {
            AddToBoostGauge(highSpeedChargeRate * Time.fixedDeltaTime);
        }

        // 게이지가 0 미만으로 내려가지 않도록 함
        if (currentBoostGauge < 0) currentBoostGauge = 0;
    }

    // --- 유틸리티 및 기존 함수들 ---

    // 부스터 게이지 추가 함수 (외부에서도 호출 가능, 예: 아이템 획득)
    public void AddToBoostGauge(float amount)
    {
        currentBoostGauge += amount;
        if (currentBoostGauge > maxBoostGauge) // 최대 게이지를 넘지 않도록
        {
            currentBoostGauge = maxBoostGauge;
        }
    }

    // 원샷 처치 시 부스터를 즉시 채우기 위한 함수
    public void InstantFillBoostGauge()
    {
        currentBoostGauge = maxBoostGauge;
        Debug.Log("원샷 처치! 부스터가 즉시 충전됩니다!");
    }

    // UI 표시용 현재 부스터 게이지 비율 반환 함수
    public float GetBoostGaugeRatio()
    {
        if (maxBoostGauge <= 0) return 0; // 0으로 나누기 방지
        return currentBoostGauge / maxBoostGauge;
    }

    // 차량 뒤집힘 감지 및 자폭 로직
    void CheckIfFlipped()
    {
        if (myVehicleHealth == null || myVehicleHealth.IsDead())
        {
            currentFlipTime = 0f;
            return;
        }
        if (transform.up.y < flipDetectionThreshold)
        {
            currentFlipTime += Time.deltaTime;
            if (currentFlipTime >= maxFlipDuration)
            {
                myVehicleHealth.TakeDamage(myVehicleHealth.GetMaxHealth() + 1, gameObject);
                currentFlipTime = 0f;
            }
        }
        else
        {
            currentFlipTime = 0f;
        }
    }

    // 충돌 감지 로직
    void OnCollisionEnter(Collision collision)
    {
        if (myVehicleHealth == null || myVehicleHealth.IsDead()) return;
        if (!collision.gameObject.CompareTag(targetTag)) { return; }
        VehicleHealth targetHealth = collision.gameObject.GetComponent<VehicleHealth>();
        Rigidbody targetRb = collision.gameObject.GetComponent<Rigidbody>();
        if (targetHealth == null || targetRb == null || targetHealth == myVehicleHealth) { return; }
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.thisCollider == bladeCollider)
            {
                CalculateAndDealDamage(targetHealth, targetRb);
                break;
            }
        }
    }

    // 데미지 계산 및 상대방에게 전달하는 함수
    void CalculateAndDealDamage(VehicleHealth target, Rigidbody targetRb)
    {
        if (rb == null) return; // 자신의 Rigidbody가 없으면 계산 불가
        Vector3 relativeVelocity = rb.linearVelocity - targetRb.linearVelocity;
        float impactSpeed = relativeVelocity.magnitude;
        float relativeSpeedFactor = 1 + (Mathf.Clamp(impactSpeed, 0, maxEffectiveRelativeSpeed) / maxEffectiveRelativeSpeed);
        float calculatedDamage = baseDamage * relativeSpeedFactor;
        Debug.Log(gameObject.name + "가 블레이드로 " + target.gameObject.name + "를 공격! 상대 속도: " + impactSpeed.ToString("F2") + ", 데미지: " + calculatedDamage.ToString("F2"));
        target.TakeDamage(calculatedDamage, this.gameObject);
    }

    // 모터 힘 처리 (가속/감속)
    void HandleMotor()
    {
        if (frontLeftWheelCollider == null || rearLeftWheelCollider == null || rearRightWheelCollider == null) return; // 필수 휠 콜라이더 확인
        float currentMotorForce = verticalInput * motorForce;
        if (isFourWheelDrive && frontRightWheelCollider != null)
        {
            frontLeftWheelCollider.motorTorque = currentMotorForce;
            frontRightWheelCollider.motorTorque = currentMotorForce;
            rearLeftWheelCollider.motorTorque = currentMotorForce;
            rearRightWheelCollider.motorTorque = currentMotorForce;
        }
        else // 후륜 구동 기본
        {
            rearLeftWheelCollider.motorTorque = currentMotorForce;
            rearRightWheelCollider.motorTorque = currentMotorForce;
            // 전륜 구동이 아닐 때 앞바퀴 모터 토크는 0으로 설정 (선택적)
            if (frontLeftWheelCollider != null) frontLeftWheelCollider.motorTorque = 0;
            if (frontRightWheelCollider != null) frontRightWheelCollider.motorTorque = 0;
        }
        float currentBrakeForce = isBraking ? brakeForce : 0f;
        ApplyBraking(currentBrakeForce);
    }

    // 브레이크 적용 함수
    void ApplyBraking(float force)
    {
        if (frontLeftWheelCollider == null || rearLeftWheelCollider == null || rearRightWheelCollider == null) return;
        frontLeftWheelCollider.brakeTorque = force;
        if (frontRightWheelCollider != null) frontRightWheelCollider.brakeTorque = force;
        rearLeftWheelCollider.brakeTorque = force;
        rearRightWheelCollider.brakeTorque = force;
    }

    // 조향 처리
    void HandleSteering()
    {
        if (frontLeftWheelCollider == null || frontRightWheelCollider == null) return;
        float steerAngle = maxSteerAngle * horizontalInput;
        frontLeftWheelCollider.steerAngle = steerAngle;
        frontRightWheelCollider.steerAngle = steerAngle;
    }

    // 휠 콜라이더의 상태를 실제 휠 모델의 트랜스폼에 업데이트
    void UpdateWheels()
    {
        UpdateSingleWheel(frontLeftWheelCollider, frontLeftWheelTransform);
        UpdateSingleWheel(frontRightWheelCollider, frontRightWheelTransform);
        UpdateSingleWheel(rearLeftWheelCollider, rearLeftWheelTransform);
        UpdateSingleWheel(rearRightWheelCollider, rearRightWheelTransform);
    }

    // 개별 휠 업데이트 함수
    void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform)
    {
        if (wheelCollider == null || wheelTransform == null) return;
        Vector3 pos;
        Quaternion rot;
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelTransform.rotation = rot;
        wheelTransform.position = pos;
    }

    // 무게 중심 시각화를 위한 기즈모 (에디터에서만 보임)
    void OnDrawGizmosSelected()
    {
        if (rb == null && GetComponent<Rigidbody>() != null) rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.TransformPoint(rb.centerOfMass), 0.1f);
        }
    }

    // 스크립트 활성화/비활성화 시 Input Action 활성화/비활성화
    void OnEnable()
    {
        EnableAction(moveAction, "Move");
        EnableAction(brakeAction, "Brake");
        EnableAction(driftAction, "Drift");
        EnableAction(boostAction, "Boost");
    }

    void OnDisable()
    {
        if (moveAction != null) moveAction.Disable();
        if (brakeAction != null) brakeAction.Disable();
        if (driftAction != null) driftAction.Disable();
        if (boostAction != null) boostAction.Disable();
    }
}