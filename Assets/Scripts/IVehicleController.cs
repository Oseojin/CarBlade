using UnityEngine;
using Unity.Netcode;
using System;

namespace CarBlade.Physics
{
    // 차량 컨트롤러 인터페이스
    public interface IVehicleController
    {
        float CurrentSpeed { get; }
        float AngularVelocity { get; }
        void Accelerate(float input);
        void Steer(float input);
        void Drift(bool isDrifting);
        void ActivateBooster();
    }
}