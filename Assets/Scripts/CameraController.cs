using UnityEngine;

// �� ��ũ��Ʈ�� ���� ī�޶� �����մϴ�.
public class CameraController : MonoBehaviour
{
    [Header("ī�޶� ����")]
    public Transform target; // ī�޶� ���� ��� (�÷��̾� ������ Transform)
    public Vector3 offset = new Vector3(0f, 5f, -10f); // ������κ����� ī�޶� ������
    public float smoothSpeed = 0.125f; // ī�޶� �̵��� �ε巯�� ����

    void LateUpdate() // ��� Update ȣ���� ���� �� ����Ǿ�, Ÿ���� �������� ��Ȯ�� �ݿ�
    {
        if (target == null)
        {
            Debug.LogWarning("ī�޶� Ÿ���� �������� �ʾҽ��ϴ�!");
            return;
        }

        // Ÿ���� ��ġ�� �������� ���� ���ϴ� ī�޶� ��ġ ���
        Vector3 desiredPosition = target.position + target.rotation * offset; // Ÿ���� ȸ���� ����Ͽ� ������ ����
        // ���� ī�޶� ��ġ���� ���ϴ� ��ġ�� �ε巴�� �̵� (Lerp)
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime); // Time.deltaTime�� ���� �����ӷ��� ���������� ����
        transform.position = smoothedPosition;

        // ī�޶� �׻� Ÿ���� �ٶ󺸵��� ����
        transform.LookAt(target.position + Vector3.up * 2f); // Ÿ���� �ణ ���κ��� �ٶ󺸵��� ����
    }
}
