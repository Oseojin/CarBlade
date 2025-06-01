using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;

namespace CarBlade.Core
{
    public class ScoreSystem : IScoreSystem
    {
        private GameManager _gameManager;
        private List<DamageRecord> _damageHistory = new List<DamageRecord>();

        // ���� ����
        private const int KILL_SCORE = 100;
        private const int ASSIST_SCORE = 25;
        private const int ONESHOT_BONUS = 100;
        private const float ASSIST_TIME_WINDOW = 5f; // 5��

        // �̺�Ʈ
        public event Action<int, int, int> OnScoreUpdated; // playerId, scoreAdded, totalScore
        public event Action<int, int> OnPlayerKill; // killerId, victimId
        public event Action<int> OnOneShotKill; // killerId

        public ScoreSystem(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        // ų ���� �߰�
        public void AddKillScore(int playerId)
        {
            if (!_gameManager.PlayerScores.ContainsKey(playerId))
                return;

            var playerScore = _gameManager.PlayerScores[playerId];
            playerScore.score += KILL_SCORE;
            playerScore.kills++;

            OnScoreUpdated?.Invoke(playerId, KILL_SCORE, playerScore.score);
            Debug.Log($"Player {playerScore.playerName} scored a kill! (+{KILL_SCORE} points)");
        }

        // ��ý�Ʈ ���� �߰�
        public void AddAssistScore(int playerId)
        {
            if (!_gameManager.PlayerScores.ContainsKey(playerId))
                return;

            var playerScore = _gameManager.PlayerScores[playerId];
            playerScore.score += ASSIST_SCORE;
            playerScore.assists++;

            OnScoreUpdated?.Invoke(playerId, ASSIST_SCORE, playerScore.score);
            Debug.Log($"Player {playerScore.playerName} got an assist! (+{ASSIST_SCORE} points)");
        }

        // ���� ���ʽ� �߰�
        public void AddOneShotBonus(int playerId)
        {
            if (!_gameManager.PlayerScores.ContainsKey(playerId))
                return;

            var playerScore = _gameManager.PlayerScores[playerId];
            playerScore.score += ONESHOT_BONUS;

            OnScoreUpdated?.Invoke(playerId, ONESHOT_BONUS, playerScore.score);
            OnOneShotKill?.Invoke(playerId);

            Debug.Log($"Player {playerScore.playerName} scored a ONE-SHOT KILL! (+{ONESHOT_BONUS} bonus points)");
        }

        // ���� ���
        public void RecordDamage(int attackerId, int targetId, float damage)
        {
            if (attackerId == targetId) // ���ش� ������� ����
                return;

            _damageHistory.Add(new DamageRecord(attackerId, targetId, damage));

            // ������ ��� ���� (10�� �̻�)
            CleanupOldDamageRecords();
        }

        // ų ó�� (��ý�Ʈ ���� ����)
        public void ProcessKill(int killerId, int victimId, bool isOneShot)
        {
            // �������� death ī��Ʈ ����
            if (_gameManager.PlayerScores.ContainsKey(victimId))
            {
                _gameManager.PlayerScores[victimId].deaths++;
            }

            // ų������ ���� �ο�
            AddKillScore(killerId);

            // ���� ų�� ��� ���ʽ�
            if (isOneShot)
            {
                AddOneShotBonus(killerId);
            }

            // ��ý�Ʈ ����
            ProcessAssists(killerId, victimId);

            // ų �̺�Ʈ �߻�
            OnPlayerKill?.Invoke(killerId, victimId);

            // �ش� �����ڿ� ���� ���� ��� ����
            _damageHistory.RemoveAll(record => record.targetId == victimId);
        }

        // ��ý�Ʈ ó��
        private void ProcessAssists(int killerId, int victimId)
        {
            float currentTime = Time.time;

            // �ֱ� 5�� �̳��� ���ظ� ���� �÷��̾�� ã��
            var assistCandidates = _damageHistory
                .Where(record =>
                    record.targetId == victimId &&
                    record.attackerId != killerId &&
                    (currentTime - record.timestamp) <= ASSIST_TIME_WINDOW)
                .Select(record => record.attackerId)
                .Distinct()
                .ToList();

            // ��ý�Ʈ ���� �ο�
            foreach (int assisterId in assistCandidates)
            {
                AddAssistScore(assisterId);
            }
        }

        // ������ ���� ��� ����
        private void CleanupOldDamageRecords()
        {
            float currentTime = Time.time;
            _damageHistory.RemoveAll(record => (currentTime - record.timestamp) > 10f);
        }

        // ��� ��ȸ
        public int GetPlayerKills(int playerId)
        {
            return _gameManager.PlayerScores.ContainsKey(playerId)
                ? _gameManager.PlayerScores[playerId].kills : 0;
        }

        public int GetPlayerAssists(int playerId)
        {
            return _gameManager.PlayerScores.ContainsKey(playerId)
                ? _gameManager.PlayerScores[playerId].assists : 0;
        }

        public int GetPlayerDeaths(int playerId)
        {
            return _gameManager.PlayerScores.ContainsKey(playerId)
                ? _gameManager.PlayerScores[playerId].deaths : 0;
        }

        public float GetPlayerKDA(int playerId)
        {
            if (!_gameManager.PlayerScores.ContainsKey(playerId))
                return 0f;

            var stats = _gameManager.PlayerScores[playerId];
            float deaths = Mathf.Max(stats.deaths, 1f); // 0���� ������ ����
            return (stats.kills + stats.assists * 0.5f) / deaths;
        }
    }
}