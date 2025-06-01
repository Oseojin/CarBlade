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

        // 점수 설정
        private const int KILL_SCORE = 100;
        private const int ASSIST_SCORE = 25;
        private const int ONESHOT_BONUS = 100;
        private const float ASSIST_TIME_WINDOW = 5f; // 5초

        // 이벤트
        public event Action<int, int, int> OnScoreUpdated; // playerId, scoreAdded, totalScore
        public event Action<int, int> OnPlayerKill; // killerId, victimId
        public event Action<int> OnOneShotKill; // killerId

        public ScoreSystem(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        // 킬 점수 추가
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

        // 어시스트 점수 추가
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

        // 원샷 보너스 추가
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

        // 피해 기록
        public void RecordDamage(int attackerId, int targetId, float damage)
        {
            if (attackerId == targetId) // 자해는 기록하지 않음
                return;

            _damageHistory.Add(new DamageRecord(attackerId, targetId, damage));

            // 오래된 기록 제거 (10초 이상)
            CleanupOldDamageRecords();
        }

        // 킬 처리 (어시스트 판정 포함)
        public void ProcessKill(int killerId, int victimId, bool isOneShot)
        {
            // 피해자의 death 카운트 증가
            if (_gameManager.PlayerScores.ContainsKey(victimId))
            {
                _gameManager.PlayerScores[victimId].deaths++;
            }

            // 킬러에게 점수 부여
            AddKillScore(killerId);

            // 원샷 킬인 경우 보너스
            if (isOneShot)
            {
                AddOneShotBonus(killerId);
            }

            // 어시스트 판정
            ProcessAssists(killerId, victimId);

            // 킬 이벤트 발생
            OnPlayerKill?.Invoke(killerId, victimId);

            // 해당 피해자에 대한 피해 기록 제거
            _damageHistory.RemoveAll(record => record.targetId == victimId);
        }

        // 어시스트 처리
        private void ProcessAssists(int killerId, int victimId)
        {
            float currentTime = Time.time;

            // 최근 5초 이내에 피해를 입힌 플레이어들 찾기
            var assistCandidates = _damageHistory
                .Where(record =>
                    record.targetId == victimId &&
                    record.attackerId != killerId &&
                    (currentTime - record.timestamp) <= ASSIST_TIME_WINDOW)
                .Select(record => record.attackerId)
                .Distinct()
                .ToList();

            // 어시스트 점수 부여
            foreach (int assisterId in assistCandidates)
            {
                AddAssistScore(assisterId);
            }
        }

        // 오래된 피해 기록 정리
        private void CleanupOldDamageRecords()
        {
            float currentTime = Time.time;
            _damageHistory.RemoveAll(record => (currentTime - record.timestamp) > 10f);
        }

        // 통계 조회
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
            float deaths = Mathf.Max(stats.deaths, 1f); // 0으로 나누기 방지
            return (stats.kills + stats.assists * 0.5f) / deaths;
        }
    }
}