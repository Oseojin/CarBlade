using UnityEngine;

public class SimpleCameraFollow : MonoBehaviour
{
    [Header("카메라 설정")]
    public Transform target; // 카메라가 따라갈 대상 (차량)
    public Vector3 offset = new Vector3(0f, 5f, -10f); // 대상으로부터의 카메라 오프셋
    public float smoothSpeed = 0.125f; // 카메라 이동의 부드러움 정도

    private Vector3 desiredPosition;

    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("카메라의 Target이 설정되지 않았습니다.");
            return;
        }

        // target의 위치와 회전을 기반으로 desiredPosition 계산
        // target의 뒤쪽, 약간 위에서 바라보는 형태로 오프셋 적용
        desiredPosition = target.position + target.TransformDirection(offset);

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        // 카메라가 항상 target을 바라보도록 설정
        transform.LookAt(target.position + Vector3.up * 2f); // 약간 위를 바라보도록 조정 (차량의 중앙 부분)
    }
}
