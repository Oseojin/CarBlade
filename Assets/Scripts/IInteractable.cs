using UnityEngine;

namespace CarBlade.Environment
{
    // ���ͷ��ͺ� ������Ʈ �������̽�
    public interface IInteractable
    {
        void OnVehicleEnter(GameObject vehicle);
        void OnVehicleExit(GameObject vehicle);
        bool IsActive { get; }
    }
}
