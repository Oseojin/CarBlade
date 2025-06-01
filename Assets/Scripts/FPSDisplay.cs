using Unity.Netcode;
using UnityEngine;

namespace CarBlade.Integration
{
    public class FPSDisplay : MonoBehaviour
    {
        private float deltaTime = 0.0f;
        private GUIStyle style;

        private void Start()
        {
            style = new GUIStyle();
            style.alignment = TextAnchor.UpperRight;
            style.fontSize = 24;
            style.normal.textColor = Color.white;
        }

        private void Update()
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        }

        private void OnGUI()
        {
            float fps = 1.0f / deltaTime;
            string text = $"{fps:0.} FPS";

            // 배경
            GUI.Box(new Rect(Screen.width - 110, 10, 100, 30), "");

            // FPS 텍스트
            GUI.Label(new Rect(Screen.width - 105, 15, 90, 20), text, style);

            // 네트워크 상태
            if (NetworkManager.Singleton != null)
            {
                string netStatus = "Offline";
                if (NetworkManager.Singleton.IsHost)
                    netStatus = "Host";
                else if (NetworkManager.Singleton.IsClient)
                    netStatus = "Client";

                GUI.Label(new Rect(Screen.width - 105, 35, 90, 20), netStatus, style);
            }
        }
    }
}