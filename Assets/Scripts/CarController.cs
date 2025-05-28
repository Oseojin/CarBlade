using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("���� �⺻ ����")]
    public float motorForce = 1500f;
    public float brakeForce = 3000f;
    public float maxSteerAngle = 30f;

    [Header("���� ������")]
    public Vector3 centerOfMassOffset = new Vector3(0, -0.75f, 0);

    [Header("�� �ݶ��̴�")]
    public WheelCollider frontLeftWheelCollider;
    public WheelCollider frontRightWheelCollider;
    public WheelCollider rearLeftWheelCollider;
    public WheelCollider rearRightWheelCollider;

    [Header("�� �޽�")]
    public Transform frontLeftWheelMesh;
    public Transform frontRightWheelMesh;
    public Transform rearLeftWheelMesh;
    public Transform rearRightWheelMesh;

    [Header("�ӵ� (����׿�)")]
    public float currentSpeedKPH;
    public float currentSpeedMPS;

    // --- �ν��� ���� ---
    [Header("�ν��� ����")]
    public KeyCode boosterKey = KeyCode.LeftShift;
    public float maxBoosterAmount = 100f;
    public float currentBoosterAmount = 0f;
    [Tooltip("�ν��� ��� �� ������ �������� �߰� ���ӷ� (m/s^2)")]
    public float boosterAcceleration = 8f;
    [Tooltip("�ʴ� �ν��� ������ �Ҹ�")]
    public float boosterConsumeRate = 25f; // ��: 100 �������� 4�ʸ��� �Ҹ�
    public float highSpeedThresholdKPH = 60f;
    public float highSpeedChargeRate = 5f;
    private bool isBoosterEffectActive = false; // �ν��� ȿ���� ���� Ȱ��ȭ ��������
    private const float BOOSTER_FULL_THRESHOLD = 0.99f; // �ε��Ҽ��� �񱳸� ���� 100% �ٻ�ġ


    // --- �帮��Ʈ ���� ---
    [Header("�帮��Ʈ ����")]
    public KeyCode driftKey = KeyCode.LeftControl;
    public bool isDrifting = false;
    public float driftSidewaysFrictionMultiplier = 0.6f;
    public float driftForwardFrictionMultiplier = 0.8f;
    public float driftChargeRate = 15f;
    private WheelFrictionCurve originalRearSidewaysFriction;
    private WheelFrictionCurve originalRearForwardFriction;
    private WheelFrictionCurve driftingRearSidewaysFriction;
    private WheelFrictionCurve driftingRearForwardFriction;

    private Rigidbody rb;
    private float horizontalInput;
    private float verticalInput;
    private bool isBrakingInput;
    // private bool isBoosterActiveInput; // �� ������ ���� isBoosterEffectActive�� ��ü��

    private UIManager uiManager;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass += centerOfMassOffset;

        uiManager = FindFirstObjectByType<UIManager>();
        if (uiManager == null)
        {
            Debug.LogError("UIManager�� ������ ã�� �� �����ϴ�!");
        }

        if (uiManager != null)
        {
            uiManager.UpdateBoosterGauge(currentBoosterAmount, maxBoosterAmount);
        }

        originalRearSidewaysFriction = rearLeftWheelCollider.sidewaysFriction;
        originalRearForwardFriction = rearLeftWheelCollider.forwardFriction;

        driftingRearSidewaysFriction = originalRearSidewaysFriction;
        driftingRearSidewaysFriction.stiffness *= driftSidewaysFrictionMultiplier;
        driftingRearForwardFriction = originalRearForwardFriction;
        driftingRearForwardFriction.stiffness *= driftForwardFrictionMultiplier;
    }

    void Update()
    {
        GetInput();

        currentSpeedMPS = rb.linearVelocity.magnitude;
        currentSpeedKPH = currentSpeedMPS * 3.6f;

        if (uiManager != null)
        {
            uiManager.UpdateSpeed(currentSpeedKPH);
            uiManager.UpdateBoosterGauge(currentBoosterAmount, maxBoosterAmount);
        }

        HandleBoosterCharging(); // �ν��� ���� ������ Update���� �ð� ������� ó��
        HandleDriftingVisualsAndState(); // �帮��Ʈ ���� �� �ð� ȿ�� (�ʿ� ��)
    }

    void FixedUpdate()
    {
        HandleMotor(); // �Ϲ� ���� ����
        ProcessBoosterEffect(); // �ν��� ���� ȿ�� ����
        HandleSteering();
        ApplyDriftPhysics(); // �帮��Ʈ ���� ȿ�� ����
        UpdateWheels();
    }

    void GetInput()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        isBrakingInput = Input.GetKey(KeyCode.Space);

        // �ν��� �ߵ� ����: Ű ����, ������ 100%, ���� �ν��� ȿ�� ��Ȱ�� ����
        if (Input.GetKeyDown(boosterKey) &&
            currentBoosterAmount >= maxBoosterAmount * BOOSTER_FULL_THRESHOLD &&
            !isBoosterEffectActive)
        {
            isBoosterEffectActive = true;
            Debug.Log("�ν��� �ߵ�!");
        }

        if (Input.GetKeyDown(driftKey))
        {
            StartDrift();
        }
        if (Input.GetKeyUp(driftKey))
        {
            StopDrift();
        }
    }

    void HandleMotor()
    {
        // �ν��� ȿ���� Ȱ��ȭ�� ���ȿ��� �Ϲ� ���� ���� �ణ ���̰ų�, �״�� �� �� �ֽ��ϴ�.
        // ���⼭�� �Ϲ� ���� ���� �״�� �����ϰ�, �ν��ʹ� �߰� �������θ� �ۿ��մϴ�.
        float currentMotorForce = motorForce;

        rearLeftWheelCollider.motorTorque = verticalInput * currentMotorForce;
        rearRightWheelCollider.motorTorque = verticalInput * currentMotorForce;

        float currentBrakeForceApplied = isBrakingInput ? brakeForce : 0f;
        ApplyBrakes(currentBrakeForceApplied);
    }

    void ProcessBoosterEffect()
    {
        if (isBoosterEffectActive)
        {
            if (currentBoosterAmount > 0)
            {
                // ������ �������� ���� ���ӷ� ����
                rb.AddForce(transform.forward * boosterAcceleration, ForceMode.Acceleration);
                currentBoosterAmount -= boosterConsumeRate * Time.fixedDeltaTime;
                currentBoosterAmount = Mathf.Max(currentBoosterAmount, 0);
            }
            else // currentBoosterAmount <= 0
            {
                isBoosterEffectActive = false;
                currentBoosterAmount = 0; // Ȯ���ϰ� 0���� ����
                Debug.Log("�ν��� ����.");
            }
        }
    }

    void HandleBoosterCharging()
    {
        // �ν��� ȿ���� Ȱ��ȭ ���� �ƴ� ���� ����
        if (!isBoosterEffectActive)
        {
            if (currentSpeedKPH >= highSpeedThresholdKPH && !isDrifting)
            {
                currentBoosterAmount += highSpeedChargeRate * Time.deltaTime;
            }

            if (isDrifting)
            {
                currentBoosterAmount += driftChargeRate * Time.deltaTime;
            }

            currentBoosterAmount = Mathf.Min(currentBoosterAmount, maxBoosterAmount);
        }
    }

    // �帮��Ʈ ���� ���� �� �ð��� ȿ�� (�ʿ� ��)
    void HandleDriftingVisualsAndState()
    {
        // �� �Լ��� Update���� ȣ��Ǿ� �帮��Ʈ ���¸� �����մϴ�.
        // ������ ������ ������ FixedUpdate�� ApplyDriftPhysics���� ó���˴ϴ�.
    }

    // �帮��Ʈ ���� ȿ�� ����
    void ApplyDriftPhysics()
    {
        if (isDrifting)
        {
            rearLeftWheelCollider.sidewaysFriction = driftingRearSidewaysFriction;
            rearLeftWheelCollider.forwardFriction = driftingRearForwardFriction;
            rearRightWheelCollider.sidewaysFriction = driftingRearSidewaysFriction;
            rearRightWheelCollider.forwardFriction = driftingRearForwardFriction;
        }
        else
        {
            rearLeftWheelCollider.sidewaysFriction = originalRearSidewaysFriction;
            rearLeftWheelCollider.forwardFriction = originalRearForwardFriction;
            rearRightWheelCollider.sidewaysFriction = originalRearSidewaysFriction;
            rearRightWheelCollider.forwardFriction = originalRearForwardFriction;
        }
    }


    void StartDrift()
    {
        isDrifting = true;
    }

    void StopDrift()
    {
        isDrifting = false;
    }

    void ApplyBrakes(float brakeForceToApply)
    {
        frontLeftWheelCollider.brakeTorque = brakeForceToApply;
        frontRightWheelCollider.brakeTorque = brakeForceToApply;
        rearLeftWheelCollider.brakeTorque = brakeForceToApply;
        rearRightWheelCollider.brakeTorque = brakeForceToApply;
    }

    void HandleSteering()
    {
        float currentSteerAngle = maxSteerAngle * horizontalInput;
        frontLeftWheelCollider.steerAngle = currentSteerAngle;
        frontRightWheelCollider.steerAngle = currentSteerAngle;
    }

    void UpdateWheels()
    {
        UpdateSingleWheel(frontLeftWheelCollider, frontLeftWheelMesh);
        UpdateSingleWheel(frontRightWheelCollider, frontRightWheelMesh);
        UpdateSingleWheel(rearLeftWheelCollider, rearLeftWheelMesh);
        UpdateSingleWheel(rearRightWheelCollider, rearRightWheelMesh);
    }

    void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelMesh)
    {
        if (wheelMesh == null) return;
        Vector3 pos;
        Quaternion rot;
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelMesh.transform.position = pos;
        wheelMesh.transform.rotation = rot;
    }
}
