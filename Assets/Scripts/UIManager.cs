using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using CarBlade.Core;
using CarBlade.Physics;
using CarBlade.Combat;

namespace CarBlade.UI
{
    public class UIManager : MonoBehaviour, IUIManager
    {
        private static UIManager _instance;
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<UIManager>();
                }
                return _instance;
            }
        }

        [Header("HUD Elements")]
        [SerializeField] private GameObject hudPanel;

        [Header("Speedometer")]
        [SerializeField] private TextMeshProUGUI speedText;
        [SerializeField] private Image speedNeedle;
        [SerializeField] private float maxNeedleAngle = 270f;
        [SerializeField] private Gradient speedColorGradient;

        [Header("Booster Gauge")]
        [SerializeField] private Image boosterFillBar;
        [SerializeField] private TextMeshProUGUI boosterPercentText;
        [SerializeField] private GameObject boosterActiveEffect;
        [SerializeField] private Color normalBoostColor = Color.cyan;
        [SerializeField] private Color activeBoostColor = Color.yellow;

        [Header("Health Bar")]
        [SerializeField] private Image healthFillBar;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private GameObject lowHealthWarning;
        [SerializeField] private float lowHealthThreshold = 0.3f;

        [Header("Kill Feed")]
        [SerializeField] private Transform killFeedContainer;
        [SerializeField] private GameObject killFeedItemPrefab;
        [SerializeField] private int maxKillFeedItems = 5;
        [SerializeField] private float killFeedItemDuration = 5f;
        private Queue<GameObject> killFeedItems = new Queue<GameObject>();

        [Header("Scoreboard")]
        [SerializeField] private GameObject scoreboardPanel;
        [SerializeField] private Transform scoreboardContent;
        [SerializeField] private GameObject scoreboardItemPrefab;
        [SerializeField] private KeyCode scoreboardKey = KeyCode.Tab;
        private Dictionary<int, ScoreboardItem> scoreboardItems = new Dictionary<int, ScoreboardItem>();

        [Header("Match Timer")]
        [SerializeField] private TextMeshProUGUI matchTimerText;
        [SerializeField] private Image timerBackground;
        [SerializeField] private Color normalTimerColor = Color.white;
        [SerializeField] private Color urgentTimerColor = Color.red;
        [SerializeField] private float urgentTimeThreshold = 60f;

        [Header("Position Indicator")]
        [SerializeField] private TextMeshProUGUI positionText;
        [SerializeField] private GameObject positionChangeEffect;

        [Header("Notifications")]
        [SerializeField] private GameObject notificationPanel;
        [SerializeField] private TextMeshProUGUI notificationText;
        [SerializeField] private float notificationDuration = 3f;

        // References
        private GameManager gameManager;
        private MatchManager matchManager;
        private VehicleController localVehicle;
        private VehicleHealthSystem localHealth;
        private BoosterSystem localBooster;

        // State
        private int currentPosition = 0;
        private bool isScoreboardVisible = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void Start()
        {
            gameManager = GameManager.Instance;
            matchManager = gameManager?.GetMatchManager();

            // 이벤트 구독
            if (matchManager != null)
            {
                matchManager.OnMatchTimerUpdated += UpdateMatchTimer;
                matchManager.OnMatchEnded += OnMatchEnded;
            }

            var scoreSystem = gameManager?.GetScoreSystem() as ScoreSystem;
            if (scoreSystem != null)
            {
                scoreSystem.OnPlayerKill += OnPlayerKill;
                scoreSystem.OnOneShotKill += OnOneShotKill;
            }

            // 초기 UI 상태
            scoreboardPanel.SetActive(false);
            notificationPanel.SetActive(false);
        }

        private void Update()
        {
            // 스코어보드 토글
            if (Input.GetKeyDown(scoreboardKey))
            {
                ToggleScoreboard(true);
            }
            else if (Input.GetKeyUp(scoreboardKey))
            {
                ToggleScoreboard(false);
            }

            // 로컬 플레이어 업데이트
            UpdateLocalPlayerHUD();
        }

        // 로컬 플레이어 설정
        public void SetLocalPlayer(GameObject player)
        {
            localVehicle = player.GetComponent<VehicleController>();
            localHealth = player.GetComponent<VehicleHealthSystem>();
            localBooster = player.GetComponent<BoosterSystem>();

            // 이벤트 구독
            if (localVehicle != null)
            {
                localVehicle.OnSpeedChanged += UpdateSpeedometer;
            }

            if (localHealth != null)
            {
                localHealth.OnHealthChanged += UpdateHealthBar;
                localHealth.OnVehicleDestroyed += OnLocalPlayerDestroyed;
                localHealth.OnVehicleRespawn += OnLocalPlayerRespawn;
            }

            if (localBooster != null)
            {
                localBooster.OnBoostChanged += UpdateBoosterGauge;
                localBooster.OnBoosterStateChanged += UpdateBoosterState;
            }
        }

        // IUIManager 구현
        public void UpdateSpeedometer(float speed)
        {
            if (speedText != null)
            {
                int displaySpeed = Mathf.RoundToInt(Mathf.Abs(speed) * 3.6f); // m/s to km/h
                speedText.text = $"{displaySpeed} <size=20>KM/H</size>";
            }

            if (speedNeedle != null && localVehicle != null)
            {
                float speedPercent = Mathf.Abs(speed) / localVehicle.VehicleData.maxSpeed;
                float needleAngle = Mathf.Lerp(0, -maxNeedleAngle, speedPercent);
                speedNeedle.transform.localRotation = Quaternion.Euler(0, 0, needleAngle);

                if (speedColorGradient != null)
                {
                    speedNeedle.color = speedColorGradient.Evaluate(speedPercent);
                }
            }
        }

        public void UpdateBoosterGauge(float boost)
        {
            if (boosterFillBar != null)
            {
                boosterFillBar.fillAmount = boost / 100f;
            }

            if (boosterPercentText != null)
            {
                boosterPercentText.text = $"{Mathf.RoundToInt(boost)}%";
            }
        }

        public void ShowKillFeed(string killer, string victim)
        {
            if (killFeedItemPrefab == null || killFeedContainer == null) return;

            // 최대 개수 초과 시 가장 오래된 항목 제거
            if (killFeedItems.Count >= maxKillFeedItems)
            {
                GameObject oldestItem = killFeedItems.Dequeue();
                Destroy(oldestItem);
            }

            // 새 킬 피드 항목 생성
            GameObject newItem = Instantiate(killFeedItemPrefab, killFeedContainer);
            KillFeedItem feedItem = newItem.GetComponent<KillFeedItem>();

            if (feedItem != null)
            {
                feedItem.SetKillInfo(killer, victim);
                killFeedItems.Enqueue(newItem);

                // 자동 제거
                Destroy(newItem, killFeedItemDuration);
            }
        }

        public void UpdateScoreboard(List<PlayerScore> scores)
        {
            if (scoreboardContent == null || scoreboardItemPrefab == null) return;

            // 기존 항목 업데이트 또는 새로 생성
            foreach (var score in scores)
            {
                if (!scoreboardItems.ContainsKey(score.playerId))
                {
                    GameObject newItem = Instantiate(scoreboardItemPrefab, scoreboardContent);
                    ScoreboardItem item = newItem.GetComponent<ScoreboardItem>();
                    if (item != null)
                    {
                        scoreboardItems[score.playerId] = item;
                    }
                }

                if (scoreboardItems.TryGetValue(score.playerId, out ScoreboardItem scoreItem))
                {
                    scoreItem.UpdateScore(score);
                }
            }

            // 순위별로 정렬
            int position = 1;
            foreach (var score in scores.OrderByDescending(s => s.score))
            {
                if (scoreboardItems.TryGetValue(score.playerId, out ScoreboardItem item))
                {
                    item.transform.SetSiblingIndex(position - 1);
                    item.SetPosition(position);

                    // 로컬 플레이어 위치 업데이트
                    if (Unity.Netcode.NetworkManager.Singleton != null &&
                        score.playerId == (int)Unity.Netcode.NetworkManager.Singleton.LocalClientId)
                    {
                        UpdatePosition(position);
                    }
                }
                position++;
            }
        }

        // UI 업데이트 메서드들
        private void UpdateLocalPlayerHUD()
        {
            if (gameManager == null) return;

            // 매치 시간은 MatchManager 이벤트로 업데이트됨

            // 순위 업데이트
            if (matchManager != null && matchManager.IsMatchActive)
            {
                var scores = matchManager.GetRankedScores();
                UpdateScoreboard(scores);
            }
        }

        private void UpdateHealthBar(int current, int max)
        {
            if (healthFillBar != null)
            {
                float healthPercent = (float)current / max;
                healthFillBar.fillAmount = healthPercent;

                // 색상 변경
                if (healthPercent <= lowHealthThreshold)
                {
                    healthFillBar.color = Color.red;
                    if (lowHealthWarning != null)
                    {
                        lowHealthWarning.SetActive(true);
                    }
                }
                else
                {
                    healthFillBar.color = Color.green;
                    if (lowHealthWarning != null)
                    {
                        lowHealthWarning.SetActive(false);
                    }
                }
            }

            if (healthText != null)
            {
                healthText.text = $"{current}/{max}";
            }
        }

        private void UpdateBoosterState(bool isActive)
        {
            if (boosterActiveEffect != null)
            {
                boosterActiveEffect.SetActive(isActive);
            }

            if (boosterFillBar != null)
            {
                boosterFillBar.color = isActive ? activeBoostColor : normalBoostColor;
            }
        }

        private void UpdateMatchTimer(float timeRemaining)
        {
            if (matchTimerText != null)
            {
                int minutes = Mathf.FloorToInt(timeRemaining / 60);
                int seconds = Mathf.FloorToInt(timeRemaining % 60);
                matchTimerText.text = $"{minutes:00}:{seconds:00}";

                // 긴급 시간 표시
                if (timeRemaining <= urgentTimeThreshold)
                {
                    matchTimerText.color = urgentTimerColor;
                    if (timerBackground != null)
                    {
                        timerBackground.color = new Color(1, 0, 0, 0.3f);
                    }
                }
            }
        }

        private void UpdatePosition(int newPosition)
        {
            if (currentPosition != newPosition)
            {
                currentPosition = newPosition;

                if (positionText != null)
                {
                    positionText.text = GetPositionString(newPosition);
                }

                if (positionChangeEffect != null)
                {
                    // 위치 변경 애니메이션
                    positionChangeEffect.SetActive(true);
                    Invoke(nameof(HidePositionEffect), 1f);
                }
            }
        }

        private string GetPositionString(int position)
        {
            string suffix = "th";
            if (position == 1) suffix = "st";
            else if (position == 2) suffix = "nd";
            else if (position == 3) suffix = "rd";

            return $"{position}{suffix}";
        }

        private void HidePositionEffect()
        {
            if (positionChangeEffect != null)
            {
                positionChangeEffect.SetActive(false);
            }
        }

        // 스코어보드 토글
        private void ToggleScoreboard(bool show)
        {
            isScoreboardVisible = show;
            scoreboardPanel.SetActive(show);

            // 게임 중에는 커서 표시
            if (show)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        // 알림 표시
        public void ShowNotification(string message, float duration = 0)
        {
            if (notificationPanel != null && notificationText != null)
            {
                notificationText.text = message;
                notificationPanel.SetActive(true);

                float displayDuration = duration > 0 ? duration : notificationDuration;
                CancelInvoke(nameof(HideNotification));
                Invoke(nameof(HideNotification), displayDuration);
            }
        }

        private void HideNotification()
        {
            if (notificationPanel != null)
            {
                notificationPanel.SetActive(false);
            }
        }

        // 이벤트 핸들러
        private void OnPlayerKill(int killerId, int victimId)
        {
            string killerName = gameManager.PlayerScores.ContainsKey(killerId)
                ? gameManager.PlayerScores[killerId].playerName
                : $"Player {killerId}";

            string victimName = gameManager.PlayerScores.ContainsKey(victimId)
                ? gameManager.PlayerScores[victimId].playerName
                : $"Player {victimId}";

            ShowKillFeed(killerName, victimName);
        }

        private void OnOneShotKill(int killerId)
        {
            if (Unity.Netcode.NetworkManager.Singleton != null &&
                killerId == (int)Unity.Netcode.NetworkManager.Singleton.LocalClientId)
            {
                ShowNotification("ONE-SHOT KILL!", 2f);
            }
        }

        private void OnLocalPlayerDestroyed()
        {
            ShowNotification("DESTROYED!", 2f);
            hudPanel.SetActive(false);
        }

        private void OnLocalPlayerRespawn()
        {
            ShowNotification("RESPAWNED", 1f);
            hudPanel.SetActive(true);
        }

        private void OnMatchEnded(List<PlayerScore> finalScores)
        {
            // 최종 스코어보드 표시
            UpdateScoreboard(finalScores);
            ToggleScoreboard(true);

            // 승자 발표
            if (finalScores.Count > 0)
            {
                string winner = finalScores[0].playerName;
                ShowNotification($"{winner} WINS!", 5f);
            }
        }

        private void OnDestroy()
        {
            // 이벤트 구독 해제
            if (matchManager != null)
            {
                matchManager.OnMatchTimerUpdated -= UpdateMatchTimer;
                matchManager.OnMatchEnded -= OnMatchEnded;
            }

            if (localVehicle != null)
            {
                localVehicle.OnSpeedChanged -= UpdateSpeedometer;
            }

            if (localHealth != null)
            {
                localHealth.OnHealthChanged -= UpdateHealthBar;
                localHealth.OnVehicleDestroyed -= OnLocalPlayerDestroyed;
                localHealth.OnVehicleRespawn -= OnLocalPlayerRespawn;
            }

            if (localBooster != null)
            {
                localBooster.OnBoostChanged -= UpdateBoosterGauge;
                localBooster.OnBoosterStateChanged -= UpdateBoosterState;
            }
        }
    }
}