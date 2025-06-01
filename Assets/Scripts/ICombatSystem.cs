namespace CarBlade.Combat
{
    // 전투 시스템 인터페이스
    public interface ICombatSystem
    {
        float CalculateDamage(float speed, float angularVelocity);
        void ProcessBladeHit(int attackerId, int targetId);
        void ProcessBladeClash(int player1Id, int player2Id);
    }
}