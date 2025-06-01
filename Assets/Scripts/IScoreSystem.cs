namespace CarBlade.Core
{
    // 점수 시스템 인터페이스
    public interface IScoreSystem
    {
        void AddKillScore(int playerId);
        void AddAssistScore(int playerId);
        void AddOneShotBonus(int playerId);
        void RecordDamage(int attackerId, int targetId, float damage);
        void ProcessKill(int killerId, int victimId, bool isOneShot);
    }
}