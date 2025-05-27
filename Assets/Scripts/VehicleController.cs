using UnityEngine;
using UnityEngine.InputSystem;

public class VehicleController : MonoBehaviour
{
    // --- ���� ������ ---
    [Header("���� ����")]
    public float motorForce = 2000f; // ������ �⺻ ���ӷ�
    public float brakeForce = 3000f; // �극��ũ ��
    public float maxSteerAngle = 30f; // �ִ� ���� ����
    public bool isFourWheelDrive = true; // 4�� ���� ����

    [Header("������ ����")]
    public float centerOfMassYOffset = -0.5f; // ���� �߽��� Y�� ������
    public float maxAngularVelocity = 5f; // Rigidbody�� �ִ� ���ӵ�

    [Header("���� ����")]
    public Collider bladeCollider; // ������ ������ ���̵��� Collider
    public float baseDamage = 10f; // ���̵��� �⺻ ���ݷ�
    public string targetTag = "PlayerVehicle"; // ���� ������� �ν��� �±�
    [Tooltip("������ ��� �� ���� �ִ� ��ȿ ��� �ӵ�. �� ���� �Ѵ� ��� �ӵ������� ������ �������� �پ��ų� ������ �� �ֽ��ϴ�.")]
    public float maxEffectiveRelativeSpeed = 70f; // ������ ���� �ִ� ��ȿ ��� �ӵ�

    [Header("������ ���� ����")]
    [Tooltip("������ transform.up.y ���� �� �� �̸��̸� ������ ������ �����մϴ�.")]
    public float flipDetectionThreshold = 0.3f; // ������ ������ ���� Y�� ���� �Ӱ谪
    [Tooltip("������ ���°� �� �ð�(��) �̻� ���ӵǸ� ������ �����մϴ�.")]
    public float maxFlipDuration = 10f; // �ִ� ������ ���� �ð� (���� ����)
    private float currentFlipTime = 0f; // ���� ������ ���·� ���ӵ� �ð�

    [Header("�� �ݶ��̴� ����")]
    public WheelCollider frontLeftWheelCollider;
    public WheelCollider frontRightWheelCollider;
    public WheelCollider rearLeftWheelCollider;
    public WheelCollider rearRightWheelCollider;

    [Header("�� Ʈ������ ���� (�ð��� ������Ʈ��)")]
    public Transform frontLeftWheelTransform;
    public Transform frontRightWheelTransform;
    public Transform rearLeftWheelTransform;
    public Transform rearRightWheelTransform;

    // --- ���ο� ���� ������ ---
    [Header("�ν��� ����")]
    public float maxBoostGauge = 100f; // �ִ� �ν��� ������
    public float boostForce = 4000f; // �ν��� ��� �� �߰��Ǵ� ��
    public float boostConsumptionRate = 25f; // �ʴ� �ν��� ������ �Ҹ�
    public float highSpeedThreshold = 40f; // ��� �������� ������ �ӵ� (m/s)
    public float highSpeedChargeRate = 2f; // ��� ���� �� �ʴ� ������ ������

    [Header("�帮��Ʈ ����")]
    public float driftChargeRate = 30f; // �帮��Ʈ �� �ʴ� ������ ������
    [Tooltip("�帮��Ʈ �� ����� �޹����� ���� ������ ���(Stiffness). �������� �� �̲������ϴ�.")]
    public float driftSidewaysStiffness = 0.5f; // �帮��Ʈ �� ���� ���� ���

    // --- ���� ������ ---
    private Rigidbody rb; // ������ Rigidbody ������Ʈ
    private VehicleHealth myVehicleHealth; // ���� �ڽ��� VehicleHealth ������Ʈ
    private float horizontalInput; // �¿� �Է� ��
    private float verticalInput; // �յ� �Է� ��
    private bool isBraking; // �극��ũ �Է� ����

    // --- ���ο� ���� ������ ---
    private float currentBoostGauge; // ���� �ν��� ������
    private bool isDrifting; // ���� �帮��Ʈ ������ ����
    private bool isBoosting; // ���� �ν��� ��� ������ ����
    private WheelFrictionCurve defaultRearSidewaysFriction; // �޹����� �⺻ ���� ������ �����

    // --- Input System ���� ���� ---
    public InputActionAsset inputActions; // ������ Input Actions Asset
    private InputAction moveAction; // �̵� �׼� (Vector2)
    private InputAction brakeAction; // �극��ũ �׼� (Button)
    private InputAction driftAction; // �帮��Ʈ �׼� (Button)
    private InputAction boostAction; // �ν��� �׼� (Button)

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        myVehicleHealth = GetComponent<VehicleHealth>();

        if (myVehicleHealth == null) Debug.LogError("VehicleHealth ������Ʈ�� ã�� �� �����ϴ�!", gameObject);
        if (rb == null) Debug.LogError("Rigidbody ������Ʈ�� ã�� �� �����ϴ�!", gameObject);
        else
        {
            rb.maxAngularVelocity = maxAngularVelocity;
            rb.centerOfMass += new Vector3(0, centerOfMassYOffset, 0);
        }

        // �帮��Ʈ ���� �� ������ ���� �޹����� �⺻ ���� ������ ���� ����
        // WheelCollider�� null�� �ƴ� ��쿡�� ����
        if (rearLeftWheelCollider != null)
        {
            defaultRearSidewaysFriction = rearLeftWheelCollider.sidewaysFriction;
        }
        else
        {
            Debug.LogWarning("Rear Left Wheel Collider�� �������� �ʾҽ��ϴ�. �帮��Ʈ ����� ���� �۵����� ���� �� �ֽ��ϴ�.", gameObject);
        }
        // ������ �޹����� �����ϰ� ó���� �� ������, ���� �¿� ��Ī�̹Ƿ� �ϳ��� �����ص� ������ �� ����.
        // �ʿ�� rearRightWheelCollider�� ���ؼ��� default ���� ����.

        // Input Action �ʱ�ȭ
        if (inputActions != null)
        {
            var playerActionMap = inputActions.FindActionMap("Player"); // "Player"�� ����ڰ� ������ �׼� �� �̸�
            if (playerActionMap != null)
            {
                moveAction = playerActionMap.FindAction("Move");
                brakeAction = playerActionMap.FindAction("Brake");
                driftAction = playerActionMap.FindAction("Drift"); // "Drift" �׼� ã��
                boostAction = playerActionMap.FindAction("Boost"); // "Boost" �׼� ã��

                EnableAction(moveAction, "Move");
                EnableAction(brakeAction, "Brake");
                EnableAction(driftAction, "Drift");
                EnableAction(boostAction, "Boost");
            }
            else
            {
                Debug.LogError("'Player' �׼� ���� ã�� �� �����ϴ�. Input Actions Asset ������ Ȯ�����ּ���.", gameObject);
            }
        }
        else
        {
            Debug.LogError("Input Actions Asset�� ������� �ʾҽ��ϴ�.", gameObject);
        }
    }

    // �׼� Ȱ��ȭ ���� �Լ�
    void EnableAction(InputAction action, string actionName)
    {
        if (action != null)
        {
            action.Enable();
        }
        else
        {
            Debug.LogError($"'{actionName}' �׼��� ã�� �� �����ϴ�. Input Actions Asset���� �ش� �̸��� �׼��� �����ϴ��� Ȯ�����ּ���.", gameObject);
        }
    }

    void Update()
    {
        GetInput(); // �Է� ó��
        CheckIfFlipped(); // �� ������ ������ ���� üũ
    }

    void FixedUpdate()
    {
        // ���� ������Ʈ�� FixedUpdate���� ó��
        HandleMotor();
        HandleSteering();
        HandleDrift(); // �帮��Ʈ ���� �� ������ ���� ó��
        HandleBoost(); // �ν��� ���� �� ������ �Ҹ�/���� ó��
        UpdateWheels();
    }

    // --- �ֿ� ���� �Լ��� ---

    // �Է� �� �޾ƿ���
    void GetInput()
    {
        // �׼��� null�� ��츦 ����� ��� �ڵ�
        if (moveAction == null || brakeAction == null || driftAction == null || boostAction == null) return;

        Vector2 moveVector = moveAction.ReadValue<Vector2>();
        horizontalInput = moveVector.x; // ��/�� �Է� (-1 ~ 1)
        verticalInput = moveVector.y;   // ��/�� �Է� (-1 ~ 1)
        isBraking = brakeAction.IsPressed(); // �극��ũ ��ư ���� ����
        isDrifting = driftAction.IsPressed(); // �帮��Ʈ ��ư �Է�
        isBoosting = boostAction.IsPressed(); // �ν��� ��ư �Է�
    }

    // �帮��Ʈ ó��
    void HandleDrift()
    {
        if (rearLeftWheelCollider == null || rearRightWheelCollider == null) return; // �޹��� �ݶ��̴� ������ ���� ����

        if (isDrifting)
        {
            // �帮��Ʈ ���� �� �޹����� ���� �������� ���� �̲������� ��
            WheelFrictionCurve driftFriction = defaultRearSidewaysFriction; // �⺻�� ����
            driftFriction.stiffness = driftSidewaysStiffness; // ������ ������ ���� ���(Stiffness) ����

            rearLeftWheelCollider.sidewaysFriction = driftFriction;
            rearRightWheelCollider.sidewaysFriction = driftFriction; // ���� �޹��� ��� ����

            // �ν��� ������ ����
            AddToBoostGauge(driftChargeRate * Time.fixedDeltaTime);
        }
        else
        {
            // �帮��Ʈ�� �ƴ� �� ���� ���������� ����
            rearLeftWheelCollider.sidewaysFriction = defaultRearSidewaysFriction;
            rearRightWheelCollider.sidewaysFriction = defaultRearSidewaysFriction;
        }
    }

    // �ν��� ó��
    void HandleBoost()
    {
        // �ν��� ��� ����
        if (isBoosting && currentBoostGauge > 0)
        {
            if (rb != null)
            {
                // ������ �������� �ν��� ���� ���� (ForceMode.Acceleration�� ������ ������� ������ ���ӵ�)
                rb.AddForce(transform.forward * boostForce, ForceMode.Acceleration);
            }
            // �ν��� ������ �Ҹ�
            currentBoostGauge -= boostConsumptionRate * Time.fixedDeltaTime;
        }
        // ��� ���� �� ������ �ڵ� ���� ���� (�ν��� ��� ���� �ƴ� ����)
        else if (rb != null && rb.linearVelocity.magnitude > highSpeedThreshold)
        {
            AddToBoostGauge(highSpeedChargeRate * Time.fixedDeltaTime);
        }

        // �������� 0 �̸����� �������� �ʵ��� ��
        if (currentBoostGauge < 0) currentBoostGauge = 0;
    }

    // --- ��ƿ��Ƽ �� ���� �Լ��� ---

    // �ν��� ������ �߰� �Լ� (�ܺο����� ȣ�� ����, ��: ������ ȹ��)
    public void AddToBoostGauge(float amount)
    {
        currentBoostGauge += amount;
        if (currentBoostGauge > maxBoostGauge) // �ִ� �������� ���� �ʵ���
        {
            currentBoostGauge = maxBoostGauge;
        }
    }

    // ���� óġ �� �ν��͸� ��� ä��� ���� �Լ�
    public void InstantFillBoostGauge()
    {
        currentBoostGauge = maxBoostGauge;
        Debug.Log("���� óġ! �ν��Ͱ� ��� �����˴ϴ�!");
    }

    // UI ǥ�ÿ� ���� �ν��� ������ ���� ��ȯ �Լ�
    public float GetBoostGaugeRatio()
    {
        if (maxBoostGauge <= 0) return 0; // 0���� ������ ����
        return currentBoostGauge / maxBoostGauge;
    }

    // ���� ������ ���� �� ���� ����
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

    // �浹 ���� ����
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

    // ������ ��� �� ���濡�� �����ϴ� �Լ�
    void CalculateAndDealDamage(VehicleHealth target, Rigidbody targetRb)
    {
        if (rb == null) return; // �ڽ��� Rigidbody�� ������ ��� �Ұ�
        Vector3 relativeVelocity = rb.linearVelocity - targetRb.linearVelocity;
        float impactSpeed = relativeVelocity.magnitude;
        float relativeSpeedFactor = 1 + (Mathf.Clamp(impactSpeed, 0, maxEffectiveRelativeSpeed) / maxEffectiveRelativeSpeed);
        float calculatedDamage = baseDamage * relativeSpeedFactor;
        Debug.Log(gameObject.name + "�� ���̵�� " + target.gameObject.name + "�� ����! ��� �ӵ�: " + impactSpeed.ToString("F2") + ", ������: " + calculatedDamage.ToString("F2"));
        target.TakeDamage(calculatedDamage, this.gameObject);
    }

    // ���� �� ó�� (����/����)
    void HandleMotor()
    {
        if (frontLeftWheelCollider == null || rearLeftWheelCollider == null || rearRightWheelCollider == null) return; // �ʼ� �� �ݶ��̴� Ȯ��
        float currentMotorForce = verticalInput * motorForce;
        if (isFourWheelDrive && frontRightWheelCollider != null)
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
            // ���� ������ �ƴ� �� �չ��� ���� ��ũ�� 0���� ���� (������)
            if (frontLeftWheelCollider != null) frontLeftWheelCollider.motorTorque = 0;
            if (frontRightWheelCollider != null) frontRightWheelCollider.motorTorque = 0;
        }
        float currentBrakeForce = isBraking ? brakeForce : 0f;
        ApplyBraking(currentBrakeForce);
    }

    // �극��ũ ���� �Լ�
    void ApplyBraking(float force)
    {
        if (frontLeftWheelCollider == null || rearLeftWheelCollider == null || rearRightWheelCollider == null) return;
        frontLeftWheelCollider.brakeTorque = force;
        if (frontRightWheelCollider != null) frontRightWheelCollider.brakeTorque = force;
        rearLeftWheelCollider.brakeTorque = force;
        rearRightWheelCollider.brakeTorque = force;
    }

    // ���� ó��
    void HandleSteering()
    {
        if (frontLeftWheelCollider == null || frontRightWheelCollider == null) return;
        float steerAngle = maxSteerAngle * horizontalInput;
        frontLeftWheelCollider.steerAngle = steerAngle;
        frontRightWheelCollider.steerAngle = steerAngle;
    }

    // �� �ݶ��̴��� ���¸� ���� �� ���� Ʈ�������� ������Ʈ
    void UpdateWheels()
    {
        UpdateSingleWheel(frontLeftWheelCollider, frontLeftWheelTransform);
        UpdateSingleWheel(frontRightWheelCollider, frontRightWheelTransform);
        UpdateSingleWheel(rearLeftWheelCollider, rearLeftWheelTransform);
        UpdateSingleWheel(rearRightWheelCollider, rearRightWheelTransform);
    }

    // ���� �� ������Ʈ �Լ�
    void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform)
    {
        if (wheelCollider == null || wheelTransform == null) return;
        Vector3 pos;
        Quaternion rot;
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelTransform.rotation = rot;
        wheelTransform.position = pos;
    }

    // ���� �߽� �ð�ȭ�� ���� ����� (�����Ϳ����� ����)
    void OnDrawGizmosSelected()
    {
        if (rb == null && GetComponent<Rigidbody>() != null) rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.TransformPoint(rb.centerOfMass), 0.1f);
        }
    }

    // ��ũ��Ʈ Ȱ��ȭ/��Ȱ��ȭ �� Input Action Ȱ��ȭ/��Ȱ��ȭ
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