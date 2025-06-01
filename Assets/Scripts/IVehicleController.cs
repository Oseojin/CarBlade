using UnityEngine;
using Unity.Netcode;
using System;

namespace CarBlade.Physics
{
    // ���� ��Ʈ�ѷ� �������̽�
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