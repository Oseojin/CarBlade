using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CarBlade.Core
{
    // 매치 관리 시스템
    public class MatchManager
    {
        private GameManager _gameManager;
        private float _matchTimer;
        private bool _isMatchActive;
        private Coroutine _matchTimerCoroutine;

        // 이벤트
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

        // 매치 준비
        public void PrepareMatch()
        {
            _matchTimer = _gameManager.MatchDuration;
            _isMatchActive = false;

            // 모든 플레이어 점수 초기화
            foreach (var playerScore in _gameManager.PlayerScores.Values)
            {
                playerScore.score = 0;
                playerScore.kills = 0;
                playerScore.assists = 0;
                playerScore.deaths = 0;
            }

            Debug.Log("Match prepared. Duration: " + _matchTimer + " seconds");
        }

        // 매치 시작
        public void StartMatch()
        {
            _isMatchActive = true;
            OnMatchStarted?.Invoke();

            // 타이머 코루틴 시작
            _matchTimerCoroutine = _gameManager.StartCoroutine(MatchTimerCoroutine());

            Debug.Log("Match started!");
        }

        // 매치 종료
        public void EndMatch()
        {
            _isMatchActive = false;

            // 타이머 중지
            if (_matchTimerCoroutine != null)
            {
                _gameManager.StopCoroutine(_matchTimerCoroutine);
                _matchTimerCoroutine = null;
            }

            // 최종 순위 계산
            var finalScores = GetRankedScores();

            // GP 보상 지급
            AwardGearPoints(finalScores);

            // 매치 종료 이벤트 발생
            OnMatchEnded?.Invoke(finalScores);

            Debug.Log("Match ended! Winner: " + (finalScores.Count > 0 ? finalScores[0].playerName : "None"));
        }

        // 매치 리셋
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

        // 매치 타이머 코루틴
        private IEnumerator MatchTimerCoroutine()
        {
            while (_matchTimer > 0 && _isMatchActive)
            {
                _matchTimer -= Time.deltaTime;
                OnMatchTimerUpdated?.Invoke(_matchTimer);

                // 시간이 다 되면 매치 종료
                if (_matchTimer <= 0)
                {
                    _matchTimer = 0;
                    _gameManager.EndMatch();
                }

                yield return null;
            }
        }

        // 순위별로 정렬된 점수 반환
        public List<PlayerScore> GetRankedScores()
        {
            return _gameManager.PlayerScores.Values
                .OrderByDescending(p => p.score)
                .ThenByDescending(p => p.kills)
                .ThenBy(p => p.deaths)
                .ToList();
        }

        // GP(Gear Point) 보상 지급
        private void AwardGearPoints(List<PlayerScore> rankedScores)
        {
            // 순위별 GP 보상 (예시)
            int[] gpRewards = { 1000, 750, 500, 400, 300, 250, 200, 150, 100, 50 };

            for (int i = 0; i < rankedScores.Count && i < gpRewards.Length; i++)
            {
                int playerId = rankedScores[i].playerId;
                int gpAmount = gpRewards[i];

                // 추가 보너스: 10킬 이상 시 +200 GP
                if (rankedScores[i].kills >= 10)
                {
                    gpAmount += 200;
                }

                // 여기서 실제 GP 지급 로직 호출
                // CustomizationSystem에서 처리
                Debug.Log($"Awarding {gpAmount} GP to player {rankedScores[i].playerName} (Rank {i + 1})");
            }

            // 참가 보상: 순위권 밖의 플레이어들에게 25 GP
            for (int i = gpRewards.Length; i < rankedScores.Count; i++)
            {
                Debug.Log($"Awarding 25 GP (participation) to player {rankedScores[i].playerName}");
            }
        }

        // 현재 리더 반환
        public PlayerScore GetCurrentLeader()
        {
            var ranked = GetRankedScores();
            return ranked.Count > 0 ? ranked[0] : null;
        }

        // 매치 시간 포맷팅 (MM:SS)
        public string GetFormattedMatchTime()
        {
            int minutes = Mathf.FloorToInt(_matchTimer / 60);
            int seconds = Mathf.FloorToInt(_matchTimer % 60);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}