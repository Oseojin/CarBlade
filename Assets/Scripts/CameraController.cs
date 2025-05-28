using UnityEngine;

// 이 스크립트는 메인 카메라에 연결합니다.
public class CameraController : MonoBehaviour
{
    [Header("카메라 설정")]
    public Transform target; // 카메라가 따라갈 대상 (플레이어 차량의 Transform)
    public Vector3 offset = new Vector3(0f, 5f, -10f); // 대상으로부터의 카메라 오프셋
    public float smoothSpeed = 0.125f; // 카메라 이동의 부드러움 정도

    void LateUpdate() // 모든 Update 호출이 끝난 후 실행되어, 타겟의 움직임을 정확히 반영
    {
        if (target == null)
        {
            Debug.LogWarning("카메라 타겟이 설정되지 않았습니다!");
            return;
        }

        // 타겟의 위치에 오프셋을 더해 원하는 카메라 위치 계산
        Vector3 desiredPosition = target.position + target.rotation * offset; // 타겟의 회전을 고려하여 오프셋 적용
        // 현재 카메라 위치에서 원하는 위치로 부드럽게 이동 (Lerp)
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime); // Time.deltaTime을 곱해 프레임률에 독립적으로 만듦
        transform.position = smoothedPosition;

        // 카메라가 항상 타겟을 바라보도록 설정
        transform.LookAt(target.position + Vector3.up * 2f); // 타겟의 약간 윗부분을 바라보도록 조정
    }
}
