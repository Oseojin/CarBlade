using System.Collections.Generic;
using UnityEngine;

namespace CarBlade.Environment
{
    public interface IMapManager
    {
        Vector3 GetRandomSpawnPoint();
        List<Transform> GetJumpRamps();
        void RegisterInteractableObject(IInteractable obj);
    }
}