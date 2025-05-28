using UnityEngine;
using UnityEngine.UI; // Slider ����� ���� �ʿ�
using TMPro;

// �� ��ũ��Ʈ�� Canvas ������Ʈ�� �����մϴ�.
public class UIManager : MonoBehaviour
{
    [Header("UI ���")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI speedText;
    public Slider boosterGauge; // --- �߰��� �κ� --- (Inspector���� BoosterGauge �����̴� �Ҵ�)

    // ������ ������Ʈ�ϴ� �Լ�
    public void UpdateScore(int newScore)
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + newScore;
        }
    }

    // �ӵ��� ������Ʈ�ϴ� �Լ�
    public void UpdateSpeed(float speed)
    {
        if (speedText != null)
        {
            speedText.text = "Speed: " + Mathf.FloorToInt(speed) + " km/h";
        }
    }

    // --- �߰��� �κ� ---
    // �ν��� �������� ������Ʈ�ϴ� �Լ�
    public void UpdateBoosterGauge(float currentBoosterAmount, float maxBoosterAmount)
    {
        if (boosterGauge != null)
        {
            boosterGauge.value = currentBoosterAmount / maxBoosterAmount; // 0�� 1 ������ ������ ����ȭ
        }
    }
    // --- ������� ---
}