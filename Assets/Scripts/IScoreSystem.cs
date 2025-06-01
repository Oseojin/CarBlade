namespace CarBlade.Core
{
    // ���� �ý��� �������̽�
    public interface IScoreSystem
    {
        void AddKillScore(int playerId);
        void AddAssistScore(int playerId);
        void AddOneShotBonus(int playerId);
        void RecordDamage(int attackerId, int targetId, float damage);
        void ProcessKill(int killerId, int victimId, bool isOneShot);
    }
}