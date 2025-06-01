using UnityEngine;

namespace CarBlade.InputEvent
{
    public class CameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private float followDistance = 10f;
        [SerializeField] private float followHeight = 5f;
        [SerializeField] private float lookAheadDistance = 5f;
        [SerializeField] private float smoothTime = 0.3f;

        [Header("Camera Modes")]
        [SerializeField]
        private Vector3[] cameraOffsets = new Vector3[]
        {
            new Vector3(0, 5, -10),    // �⺻ �Ĺ� ��
            new Vector3(0, 8, -15),    // ���Ÿ� ��
            new Vector3(0, 2, -6),     // ���� ��
            new Vector3(0, 15, -20)    // ž�ٿ� ��
        };

        private Transform target;
        private Camera mainCamera;
        private int currentCameraMode = 0;

        private Vector3 currentVelocity;
        private Vector3 targetPosition;
        private Quaternion targetRotation;

        private void Awake()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = new GameObject("Main Camera").AddComponent<Camera>();
                mainCamera.tag = "MainCamera";
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            if (target == null || mainCamera == null) return;

            UpdateCameraPosition();
        }

        private void UpdateCameraPosition()
        {
            // ���� ī�޶� ������
            Vector3 offset = cameraOffsets[currentCameraMode];

            // Ÿ�� ��ġ ���
            targetPosition = target.position + target.TransformDirection(offset);

            // ī�޶� ��ġ ������
            mainCamera.transform.position = Vector3.SmoothDamp(
                mainCamera.transform.position,
                targetPosition,
                ref currentVelocity,
                smoothTime
            );

            // ī�޶� Ÿ���� �ٶ󺸵��� ����
            Vector3 lookPosition = target.position + target.forward * lookAheadDistance;
            mainCamera.transform.LookAt(lookPosition);
        }

        public void ToggleCamera()
        {
            currentCameraMode = (currentCameraMode + 1) % cameraOffsets.Length;
        }

        public void SetCameraMode(int mode)
        {
            if (mode >= 0 && mode < cameraOffsets.Length)
            {
                currentCameraMode = mode;
            }
        }
    }
}