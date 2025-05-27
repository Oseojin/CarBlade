using UnityEngine;

public class SimpleCameraFollow : MonoBehaviour
{
    [Header("ī�޶� ����")]
    public Transform target; // ī�޶� ���� ��� (����)
    public Vector3 offset = new Vector3(0f, 5f, -10f); // ������κ����� ī�޶� ������
    public float smoothSpeed = 0.125f; // ī�޶� �̵��� �ε巯�� ����

    private Vector3 desiredPosition;

    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("ī�޶��� Target�� �������� �ʾҽ��ϴ�.");
            return;
        }

        // target�� ��ġ�� ȸ���� ������� desiredPosition ���
        // target�� ����, �ణ ������ �ٶ󺸴� ���·� ������ ����
        desiredPosition = target.position + target.TransformDirection(offset);

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;

        // ī�޶� �׻� target�� �ٶ󺸵��� ����
        transform.LookAt(target.position + Vector3.up * 2f); // �ణ ���� �ٶ󺸵��� ���� (������ �߾� �κ�)
    }
}
