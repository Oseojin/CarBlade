using UnityEngine;

namespace CarBlade.Environment
{
    // 인터랙터블 오브젝트 인터페이스
    public interface IInteractable
    {
        void OnVehicleEnter(GameObject vehicle);
        void OnVehicleExit(GameObject vehicle);
        bool IsActive { get; }
    }
}
