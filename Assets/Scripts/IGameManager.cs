using System;

namespace CarBlade.Core
{
    // ���� ���¸� �����ϴ� ������
    public enum GameState
    {
        Lobby,
        Starting,
        InProgress,
        Ending,
        PostMatch
    }

    // GameManager �������̽�
    public interface IGameManager
    {
        void StartMatch();
        void EndMatch();
        void UpdateScore(int playerId, int points);
        GameState GetCurrentState();
        event Action<GameState> OnGameStateChanged;
    }
}