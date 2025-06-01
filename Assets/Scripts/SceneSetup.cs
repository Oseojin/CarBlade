using CarBlade.Core;
using CarBlade.Environment;
using CarBlade.Networking;
using CarBlade.UI;
using UnityEngine;

namespace CarBlade.Integration
{
    [System.Serializable]
    public class SceneSetup
    {
        public string sceneName = "CarBladeArena";
        public GameObject[] requiredPrefabs;
        public bool validateOnStart = true;

        public bool Validate()
        {
            bool isValid = true;

            // 필수 컴포넌트 체크
            if (GameManager.Instance == null)
            {
                Debug.LogError("GameManager not found!");
                isValid = false;
            }

            if (CarBladeNetworkManager.Instance == null)
            {
                Debug.LogError("NetworkManager not found!");
                isValid = false;
            }

            if (MapManager.Instance == null)
            {
                Debug.LogError("MapManager not found!");
                isValid = false;
            }

            if (UIManager.Instance == null)
            {
                Debug.LogError("UIManager not found!");
                isValid = false;
            }

            // 필수 프리팹 체크
            foreach (var prefab in requiredPrefabs)
            {
                if (prefab == null)
                {
                    Debug.LogError($"Required prefab is missing!");
                    isValid = false;
                }
            }

            return isValid;
        }
    }
}