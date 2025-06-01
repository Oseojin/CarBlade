using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using CarBlade.Core;
using CarBlade.Physics;
using CarBlade.Combat;

namespace CarBlade.UI
{
    public class ScoreboardItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI positionText;
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI killsText;
        [SerializeField] private TextMeshProUGUI assistsText;
        [SerializeField] private TextMeshProUGUI deathsText;
        [SerializeField] private TextMeshProUGUI kdaText;

        [Header("Highlight")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Color normalColor = new Color(0, 0, 0, 0.5f);
        [SerializeField] private Color localPlayerColor = new Color(0.2f, 0.5f, 1f, 0.7f);
        [SerializeField] private Color topThreeColor = new Color(1f, 0.8f, 0f, 0.5f);

        private int playerId;
        private bool isLocalPlayer;

        public void UpdateScore(PlayerScore score)
        {
            playerId = score.playerId;

            // 로컬 플레이어 확인
            isLocalPlayer = Unity.Netcode.NetworkManager.Singleton != null &&
                           score.playerId == (int)Unity.Netcode.NetworkManager.Singleton.LocalClientId;

            // 텍스트 업데이트
            if (playerNameText != null) playerNameText.text = score.playerName;
            if (scoreText != null) scoreText.text = score.score.ToString();
            if (killsText != null) killsText.text = score.kills.ToString();
            if (assistsText != null) assistsText.text = score.assists.ToString();
            if (deathsText != null) deathsText.text = score.deaths.ToString();

            // KDA 계산
            if (kdaText != null)
            {
                float kda = score.deaths == 0 ?
                    score.kills + score.assists * 0.5f :
                    (score.kills + score.assists * 0.5f) / score.deaths;
                kdaText.text = kda.ToString("F2");
            }

            // 배경색 설정
            UpdateBackgroundColor();
        }

        public void SetPosition(int position)
        {
            if (positionText != null)
            {
                positionText.text = position.ToString();

                // 상위 3명 특별 표시
                if (position <= 3)
                {
                    positionText.color = GetPositionColor(position);
                    positionText.fontSize = 24;
                }
                else
                {
                    positionText.color = Color.white;
                    positionText.fontSize = 20;
                }
            }

            UpdateBackgroundColor();
        }

        private Color GetPositionColor(int position)
        {
            switch (position)
            {
                case 1: return new Color(1f, 0.84f, 0f); // Gold
                case 2: return new Color(0.75f, 0.75f, 0.75f); // Silver
                case 3: return new Color(0.8f, 0.5f, 0.2f); // Bronze
                default: return Color.white;
            }
        }

        private void UpdateBackgroundColor()
        {
            if (backgroundImage == null) return;

            if (isLocalPlayer)
            {
                backgroundImage.color = localPlayerColor;
            }
            else if (positionText != null && int.TryParse(positionText.text, out int pos) && pos <= 3)
            {
                backgroundImage.color = topThreeColor;
            }
            else
            {
                backgroundImage.color = normalColor;
            }
        }
    }
}