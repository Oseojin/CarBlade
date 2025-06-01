using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CarBlade.Core
{
    // ��ġ ���� �ý���
    public class MatchManager
    {
        private GameManager _gameManager;
        private float _matchTimer;
        private bool _isMatchActive;
        private Coroutine _matchTimerCoroutine;

        // �̺�Ʈ
        public event Action<float> OnMatchTimerUpdated;
        public event Action<List<PlayerScore>> OnMatchEnded;
        public event Action OnMatchStarted;

        public float MatchTimer => _matchTimer;
        public bool IsMatchActive => _isMatchActive;

        public MatchManager(GameManager gameManager)
        {
            _gameManager = gameManager;
            _matchTimer = 0f;
            _isMatchActive = false;
        }

        // ��ġ �غ�
        public void PrepareMatch()
        {
            _matchTimer = _gameManager.MatchDuration;
            _isMatchActive = false;

            // ��� �÷��̾� ���� �ʱ�ȭ
            foreach (var playerScore in _gameManager.PlayerScores.Values)
            {
                playerScore.score = 0;
                playerScore.kills = 0;
                playerScore.assists = 0;
                playerScore.deaths = 0;
            }

            Debug.Log("Match prepared. Duration: " + _matchTimer + " seconds");
        }

        // ��ġ ����
        public void StartMatch()
        {
            _isMatchActive = true;
            OnMatchStarted?.Invoke();

            // Ÿ�̸� �ڷ�ƾ ����
            _matchTimerCoroutine = _gameManager.StartCoroutine(MatchTimerCoroutine());

            Debug.Log("Match started!");
        }

        // ��ġ ����
        public void EndMatch()
        {
            _isMatchActive = false;

            // Ÿ�̸� ����
            if (_matchTimerCoroutine != null)
            {
                _gameManager.StopCoroutine(_matchTimerCoroutine);
                _matchTimerCoroutine = null;
            }

            // ���� ���� ���
            var finalScores = GetRankedScores();

            // GP ���� ����
            AwardGearPoints(finalScores);

            // ��ġ ���� �̺�Ʈ �߻�
            OnMatchEnded?.Invoke(finalScores);

            Debug.Log("Match ended! Winner: " + (finalScores.Count > 0 ? finalScores[0].playerName : "None"));
        }

        // ��ġ ����
        public void ResetMatch()
        {
            _matchTimer = 0f;
            _isMatchActive = false;

            if (_matchTimerCoroutine != null)
            {
                _gameManager.StopCoroutine(_matchTimerCoroutine);
                _matchTimerCoroutine = null;
            }
        }

        // ��ġ Ÿ�̸� �ڷ�ƾ
        private IEnumerator MatchTimerCoroutine()
        {
            while (_matchTimer > 0 && _isMatchActive)
            {
                _matchTimer -= Time.deltaTime;
                OnMatchTimerUpdated?.Invoke(_matchTimer);

                // �ð��� �� �Ǹ� ��ġ ����
                if (_matchTimer <= 0)
                {
                    _matchTimer = 0;
                    _gameManager.EndMatch();
                }

                yield return null;
            }
        }

        // �������� ���ĵ� ���� ��ȯ
        public List<PlayerScore> GetRankedScores()
        {
            return _gameManager.PlayerScores.Values
                .OrderByDescending(p => p.score)
                .ThenByDescending(p => p.kills)
                .ThenBy(p => p.deaths)
                .ToList();
        }

        // GP(Gear Point) ���� ����
        private void AwardGearPoints(List<PlayerScore> rankedScores)
        {
            // ������ GP ���� (����)
            int[] gpRewards = { 1000, 750, 500, 400, 300, 250, 200, 150, 100, 50 };

            for (int i = 0; i < rankedScores.Count && i < gpRewards.Length; i++)
            {
                int playerId = rankedScores[i].playerId;
                int gpAmount = gpRewards[i];

                // �߰� ���ʽ�: 10ų �̻� �� +200 GP
                if (rankedScores[i].kills >= 10)
                {
                    gpAmount += 200;
                }

                // ���⼭ ���� GP ���� ���� ȣ��
                // CustomizationSystem���� ó��
                Debug.Log($"Awarding {gpAmount} GP to player {rankedScores[i].playerName} (Rank {i + 1})");
            }

            // ���� ����: ������ ���� �÷��̾�鿡�� 25 GP
            for (int i = gpRewards.Length; i < rankedScores.Count; i++)
            {
                Debug.Log($"Awarding 25 GP (participation) to player {rankedScores[i].playerName}");
            }
        }

        // ���� ���� ��ȯ
        public PlayerScore GetCurrentLeader()
        {
            var ranked = GetRankedScores();
            return ranked.Count > 0 ? ranked[0] : null;
        }

        // ��ġ �ð� ������ (MM:SS)
        public string GetFormattedMatchTime()
        {
            int minutes = Mathf.FloorToInt(_matchTimer / 60);
            int seconds = Mathf.FloorToInt(_matchTimer % 60);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}