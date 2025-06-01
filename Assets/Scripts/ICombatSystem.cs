namespace CarBlade.Combat
{
    // ���� �ý��� �������̽�
    public interface ICombatSystem
    {
        float CalculateDamage(float speed, float angularVelocity);
        void ProcessBladeHit(int attackerId, int targetId);
        void ProcessBladeClash(int player1Id, int player2Id);
    }
}