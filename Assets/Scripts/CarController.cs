using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour
{
    [Header("차량 기본 설정")]
    public float motorForce = 1500f;
    public float brakeForce = 3000f;
    public float maxSteerAngle = 30f;

    [Header("차량 안정성")]
    public Vector3 centerOfMassOffset = new Vector3(0, -0.75f, 0);

    [Header("휠 콜라이더")]
    public WheelCollider frontLeftWheelCollider;
    public WheelCollider frontRightWheelCollider;
    public WheelCollider rearLeftWheelCollider;
    public WheelCollider rearRightWheelCollider;

    [Header("휠 메쉬")]
    public Transform frontLeftWheelMesh;
    public Transform frontRightWheelMesh;
    public Transform rearLeftWheelMesh;
    public Transform rearRightWheelMesh;

    [Header("속도 (디버그용)")]
    public float currentSpeedKPH;
    public float currentSpeedMPS;

    // --- 부스터 설정 ---
    [Header("부스터 설정")]
    public KeyCode boosterKey = KeyCode.LeftShift;
    public float maxBoosterAmount = 100f;
    public float currentBoosterAmount = 0f;
    [Tooltip("부스터 사용 시 차량에 가해지는 추가 가속력 (m/s^2)")]
    public float boosterAcceleration = 8f;
    [Tooltip("초당 부스터 게이지 소모량")]
    public float boosterConsumeRate = 25f; // 예: 100 게이지를 4초만에 소모
    public float highSpeedThresholdKPH = 60f;
    public float highSpeedChargeRate = 5f;
    private bool isBoosterEffectActive = false; // 부스터 효과가 현재 활성화 상태인지
    private const float BOOSTER_FULL_THRESHOLD = 0.99f; // 부동소수점 비교를 위한 100% 근사치


    // --- 드리프트 설정 ---
    [Header("드리프트 설정")]
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
    // private bool isBoosterActiveInput; // 이 변수는 이제 isBoosterEffectActive로 대체됨

    private UIManager uiManager;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass += centerOfMassOffset;

        uiManager = FindFirstObjectByType<UIManager>();
        if (uiManager == null)
        {
            Debug.LogError("UIManager를 씬에서 찾을 수 없습니다!");
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

        HandleBoosterCharging(); // 부스터 충전 로직은 Update에서 시간 기반으로 처리
        HandleDriftingVisualsAndState(); // 드리프트 상태 및 시각 효과 (필요 시)
    }

    void FixedUpdate()
    {
        HandleMotor(); // 일반 주행 로직
        ProcessBoosterEffect(); // 부스터 물리 효과 적용
        HandleSteering();
        ApplyDriftPhysics(); // 드리프트 물리 효과 적용
        UpdateWheels();
    }

    void GetInput()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        isBrakingInput = Input.GetKey(KeyCode.Space);

        // 부스터 발동 조건: 키 누름, 게이지 100%, 현재 부스터 효과 비활성 상태
        if (Input.GetKeyDown(boosterKey) &&
            currentBoosterAmount >= maxBoosterAmount * BOOSTER_FULL_THRESHOLD &&
            !isBoosterEffectActive)
        {
            isBoosterEffectActive = true;
            Debug.Log("부스터 발동!");
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
        // 부스터 효과가 활성화된 동안에는 일반 모터 힘을 약간 줄이거나, 그대로 둘 수 있습니다.
        // 여기서는 일반 모터 힘은 그대로 유지하고, 부스터는 추가 가속으로만 작용합니다.
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
                // 차량의 전방으로 직접 가속력 적용
                rb.AddForce(transform.forward * boosterAcceleration, ForceMode.Acceleration);
                currentBoosterAmount -= boosterConsumeRate * Time.fixedDeltaTime;
                currentBoosterAmount = Mathf.Max(currentBoosterAmount, 0);
            }
            else // currentBoosterAmount <= 0
            {
                isBoosterEffectActive = false;
                currentBoosterAmount = 0; // 확실하게 0으로 설정
                Debug.Log("부스터 종료.");
            }
        }
    }

    void HandleBoosterCharging()
    {
        // 부스터 효과가 활성화 중이 아닐 때만 충전
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

    // 드리프트 상태 관리 및 시각적 효과 (필요 시)
    void HandleDriftingVisualsAndState()
    {
        // 이 함수는 Update에서 호출되어 드리프트 상태를 관리합니다.
        // 물리적 마찰력 변경은 FixedUpdate의 ApplyDriftPhysics에서 처리됩니다.
    }

    // 드리프트 물리 효과 적용
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
