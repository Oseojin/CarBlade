using System;

namespace CarBlade.Core
{
    // 게임 상태를 정의하는 열거형
    public enum GameState
    {
        Lobby,
        Starting,
        InProgress,
        Ending,
        PostMatch
    }

    // GameManager 인터페이스
    public interface IGameManager
    {
        void StartMatch();
        void EndMatch();
        void UpdateScore(int playerId, int points);
        GameState GetCurrentState();
        event Action<GameState> OnGameStateChanged;
    }
}