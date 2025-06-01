using System.Collections.Generic;
using System;
using UnityEngine;

namespace CarBlade.Core
{
    public class GameManager : MonoBehaviour, IGameManager
    {
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<GameManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // ���� ���� ����
        private GameState _currentState = GameState.Lobby;
        public GameState CurrentState => _currentState;
        public event Action<GameState> OnGameStateChanged;

        // ��ġ ����
        private MatchManager _matchManager;
        private ScoreSystem _scoreSystem;

        // �÷��̾� ����
        private Dictionary<int, PlayerScore> _playerScores = new Dictionary<int, PlayerScore>();
        public Dictionary<int, PlayerScore> PlayerScores => _playerScores;

        // ���� ����
        [Header("Game Settings")]
        [SerializeField] private float matchDuration = 300f; // 5��
        [SerializeField] private int maxPlayers = 20;
        [SerializeField] private float respawnDelay = 5f;
        [SerializeField] private float respawnInvulnerabilityDuration = 2f;

        public float MatchDuration => matchDuration;
        public int MaxPlayers => maxPlayers;
        public float RespawnDelay => respawnDelay;
        public float RespawnInvulnerabilityDuration => respawnInvulnerabilityDuration;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // ������Ʈ �ʱ�ȭ
            _matchManager = new MatchManager(this);
            _scoreSystem = new ScoreSystem(this);
        }

        // IGameManager ����
        public void StartMatch()
        {
            if (_currentState != GameState.Lobby)
            {
                Debug.LogWarning("Can only start match from Lobby state");
                return;
            }

            SetGameState(GameState.Starting);
            _matchManager.PrepareMatch();

            // 3�� �� ��ġ ����
            Invoke(nameof(BeginMatch), 3f);
        }

        private void BeginMatch()
        {
            SetGameState(GameState.InProgress);
            _matchManager.StartMatch();
        }

        public void EndMatch()
        {
            if (_currentState != GameState.InProgress)
            {
                Debug.LogWarning("Can only end match when in progress");
                return;
            }

            SetGameState(GameState.Ending);
            _matchManager.EndMatch();

            // ��� ȭ�� ǥ�� �� �κ��
            Invoke(nameof(ShowPostMatch), 2f);
        }

        private void ShowPostMatch()
        {
            SetGameState(GameState.PostMatch);
            // UI ������ ��� ȭ�� ǥ��

            // 10�� �� �κ�� ����
            Invoke(nameof(ReturnToLobby), 10f);
        }

        private void ReturnToLobby()
        {
            SetGameState(GameState.Lobby);
            ResetMatch();
        }

        public void UpdateScore(int playerId, int points)
        {
            if (_playerScores.ContainsKey(playerId))
            {
                _playerScores[playerId].score += points;
            }
        }

        public GameState GetCurrentState()
        {
            return _currentState;
        }

        private void SetGameState(GameState newState)
        {
            _currentState = newState;
            OnGameStateChanged?.Invoke(newState);
            Debug.Log($"Game State Changed: {newState}");
        }

        // �÷��̾� ����
        public void RegisterPlayer(int playerId, string playerName)
        {
            if (!_playerScores.ContainsKey(playerId))
            {
                _playerScores[playerId] = new PlayerScore(playerId, playerName);
                Debug.Log($"Player registered: {playerName} (ID: {playerId})");
            }
        }

        public void UnregisterPlayer(int playerId)
        {
            if (_playerScores.ContainsKey(playerId))
            {
                _playerScores.Remove(playerId);
                Debug.Log($"Player unregistered: ID {playerId}");
            }
        }

        // ��ġ ����
        private void ResetMatch()
        {
            _playerScores.Clear();
            _matchManager.ResetMatch();
        }

        // ScoreSystem ����
        public IScoreSystem GetScoreSystem()
        {
            return _scoreSystem;
        }

        // MatchManager ����
        public MatchManager GetMatchManager()
        {
            return _matchManager;
        }
    }
}