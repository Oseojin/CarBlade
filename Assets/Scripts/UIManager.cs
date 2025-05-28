using UnityEngine;
using UnityEngine.UI; // Slider 사용을 위해 필요
using TMPro;

// 이 스크립트는 Canvas 오브젝트에 연결합니다.
public class UIManager : MonoBehaviour
{
    [Header("UI 요소")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI speedText;
    public Slider boosterGauge; // --- 추가된 부분 --- (Inspector에서 BoosterGauge 슬라이더 할당)

    // 점수를 업데이트하는 함수
    public void UpdateScore(int newScore)
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + newScore;
        }
    }

    // 속도를 업데이트하는 함수
    public void UpdateSpeed(float speed)
    {
        if (speedText != null)
        {
            speedText.text = "Speed: " + Mathf.FloorToInt(speed) + " km/h";
        }
    }

    // --- 추가된 부분 ---
    // 부스터 게이지를 업데이트하는 함수
    public void UpdateBoosterGauge(float currentBoosterAmount, float maxBoosterAmount)
    {
        if (boosterGauge != null)
        {
            boosterGauge.value = currentBoosterAmount / maxBoosterAmount; // 0과 1 사이의 값으로 정규화
        }
    }
    // --- 여기까지 ---
}