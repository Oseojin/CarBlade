using UnityEngine;
using UnityEngine.InputSystem; // ���ο� Input System ���

public class VehicleController : MonoBehaviour
{
    [Header("���� ����")]
    public float motorForce = 2000f; // ������ �⺻ ���ӷ�
    public float brakeForce = 3000f; // �극��ũ ��
    public float maxSteerAngle = 30f; // �ִ� ���� ����
    public bool isFourWheelDrive = true; // 4�� ���� ����

    [Header("�� �ݶ��̴�")]
    public WheelCollider frontLeftWheelCollider;
    public WheelCollider frontRightWheelCollider;
    public WheelCollider rearLeftWheelCollider;
    public WheelCollider rearRightWheelCollider;

    [Header("�� Ʈ������ (�ð��� ������Ʈ��)")]
    public Transform frontLeftWheelTransform;
    public Transform frontRightWheelTransform;
    public Transform rearLeftWheelTransform;
    public Transform rearRightWheelTransform;

    private Rigidbody rb;
    private float horizontalInput;
    private float verticalInput;
    private bool isBraking;

    // ���ο� Input System �׼� (Unity �����Ϳ��� ���� �ʿ�)
    public InputActionAsset inputActions;
    private InputAction moveAction;
    private InputAction brakeAction;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            Debug.LogWarning("Rigidbody�� ���� �ڵ����� �߰��Ǿ����ϴ�. ����(mass)�� ������ �������ּ���.");
            rb.mass = 1500; // ���� ����
        }

        // Input Action �ʱ�ȭ (Player Input ������Ʈ �Ǵ� ���� ���ε� �ʿ�)
        // ����: "Player" �׼� ���� "Move" �� "Brake" �׼��� ����Ѵٰ� ����
        var playerActionMap = inputActions.FindActionMap("Player"); // �׼� �� �̸��� ���� ������ �°� ����
        if (playerActionMap != null)
        {
            moveAction = playerActionMap.FindAction("Move"); // "Move" �׼��� Vector2 (��/��, ��/��)
            brakeAction = playerActionMap.FindAction("Brake"); // "Brake" �׼��� Button

            if (moveAction != null) moveAction.Enable();
            if (brakeAction != null) brakeAction.Enable();
        }
        else
        {
            Debug.LogError("������ Input Action Map�� ã�� �� �����ϴ�. Input Actions Asset�� Ȯ�����ּ���.");
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
        // ���ο� Input System ���
        if (moveAction != null)
        {
            Vector2 moveVector = moveAction.ReadValue<Vector2>();
            horizontalInput = moveVector.x; // ��/�� �Է�
            verticalInput = moveVector.y;   // ��/�� �Է�
        }

        if (brakeAction != null)
        {
            isBraking = brakeAction.IsPressed();
        }
    }

    void HandleMotor()
    {
        float currentMotorForce = verticalInput * motorForce;

        // ���� �Ǵ� �ķ� ���� ����
        if (isFourWheelDrive)
        {
            frontLeftWheelCollider.motorTorque = currentMotorForce;
            frontRightWheelCollider.motorTorque = currentMotorForce;
            rearLeftWheelCollider.motorTorque = currentMotorForce;
            rearRightWheelCollider.motorTorque = currentMotorForce;
        }
        else // �ķ� ���� �⺻
        {
            rearLeftWheelCollider.motorTorque = currentMotorForce;
            rearRightWheelCollider.motorTorque = currentMotorForce;
        }

        // �극��ũ ����
        float currentBrakeForce = isBraking ? brakeForce : 0f;
        ApplyBraking(currentBrakeForce);
    }

    void ApplyBraking(float force)
    {
        // ��� ������ �극��ũ ���� �Ǵ� ������ ���� ����
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
        // �� �ݶ��̴��� ȸ���� ��ġ�� ���� �� �޽ÿ� ����
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
