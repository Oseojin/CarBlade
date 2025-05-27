using UnityEngine;
using UnityEngine.InputSystem; // 새로운 Input System 사용

public class VehicleController : MonoBehaviour
{
    [Header("차량 설정")]
    public float motorForce = 2000f; // 차량의 기본 가속력
    public float brakeForce = 3000f; // 브레이크 힘
    public float maxSteerAngle = 30f; // 최대 조향 각도
    public bool isFourWheelDrive = true; // 4륜 구동 여부

    [Header("휠 콜라이더")]
    public WheelCollider frontLeftWheelCollider;
    public WheelCollider frontRightWheelCollider;
    public WheelCollider rearLeftWheelCollider;
    public WheelCollider rearRightWheelCollider;

    [Header("휠 트랜스폼 (시각적 업데이트용)")]
    public Transform frontLeftWheelTransform;
    public Transform frontRightWheelTransform;
    public Transform rearLeftWheelTransform;
    public Transform rearRightWheelTransform;

    private Rigidbody rb;
    private float horizontalInput;
    private float verticalInput;
    private bool isBraking;

    // 새로운 Input System 액션 (Unity 에디터에서 설정 필요)
    public InputActionAsset inputActions;
    private InputAction moveAction;
    private InputAction brakeAction;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            Debug.LogWarning("Rigidbody가 없어 자동으로 추가되었습니다. 질량(mass)을 적절히 조절해주세요.");
            rb.mass = 1500; // 예시 질량
        }

        // Input Action 초기화 (Player Input 컴포넌트 또는 직접 바인딩 필요)
        // 예시: "Player" 액션 맵의 "Move" 와 "Brake" 액션을 사용한다고 가정
        var playerActionMap = inputActions.FindActionMap("Player"); // 액션 맵 이름은 실제 설정에 맞게 변경
        if (playerActionMap != null)
        {
            moveAction = playerActionMap.FindAction("Move"); // "Move" 액션은 Vector2 (좌/우, 앞/뒤)
            brakeAction = playerActionMap.FindAction("Brake"); // "Brake" 액션은 Button

            if (moveAction != null) moveAction.Enable();
            if (brakeAction != null) brakeAction.Enable();
        }
        else
        {
            Debug.LogError("지정된 Input Action Map을 찾을 수 없습니다. Input Actions Asset을 확인해주세요.");
        }
    }

    void Update()
    {
        GetInput();
    }

    void FixedUpdate()
    {
        HandleMotor();
        HandleSteering();
        UpdateWheels();
    }

    void GetInput()
    {
        // 새로운 Input System 사용
        if (moveAction != null)
        {
            Vector2 moveVector = moveAction.ReadValue<Vector2>();
            horizontalInput = moveVector.x; // 좌/우 입력
            verticalInput = moveVector.y;   // 앞/뒤 입력
        }

        if (brakeAction != null)
        {
            isBraking = brakeAction.IsPressed();
        }
    }

    void HandleMotor()
    {
        float currentMotorForce = verticalInput * motorForce;

        // 전륜 또는 후륜 구동 설정
        if (isFourWheelDrive)
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
        }

        // 브레이크 적용
        float currentBrakeForce = isBraking ? brakeForce : 0f;
        ApplyBraking(currentBrakeForce);
    }

    void ApplyBraking(float force)
    {
        // 모든 바퀴에 브레이크 적용 또는 선택적 적용 가능
        frontLeftWheelCollider.brakeTorque = force;
        frontRightWheelCollider.brakeTorque = force;
        rearLeftWheelCollider.brakeTorque = force;
        rearRightWheelCollider.brakeTorque = force;
    }

    void HandleSteering()
    {
        float steerAngle = maxSteerAngle * horizontalInput;
        frontLeftWheelCollider.steerAngle = steerAngle;
        frontRightWheelCollider.steerAngle = steerAngle;
    }

    void UpdateWheels()
    {
        // 휠 콜라이더의 회전과 위치를 실제 휠 메시에 적용
        UpdateSingleWheel(frontLeftWheelCollider, frontLeftWheelTransform);
        UpdateSingleWheel(frontRightWheelCollider, frontRightWheelTransform);
        UpdateSingleWheel(rearLeftWheelCollider, rearLeftWheelTransform);
        UpdateSingleWheel(rearRightWheelCollider, rearRightWheelTransform);
    }

    void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform)
    {
        if (wheelTransform == null) return;

        Vector3 pos;
        Quaternion rot;
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelTransform.rotation = rot;
        wheelTransform.position = pos;
    }

    void OnEnable()
    {
        if (moveAction != null) moveAction.Enable();
        if (brakeAction != null) brakeAction.Enable();
    }

    void OnDisable()
    {
        if (moveAction != null) moveAction.Disable();
        if (brakeAction != null) brakeAction.Disable();
    }
}
