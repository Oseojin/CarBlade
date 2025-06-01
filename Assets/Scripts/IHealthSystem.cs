namespace CarBlade.Combat
{
    // 체력 시스템 인터페이스
    public interface IHealthSystem
    {
        int CurrentHealth { get; }
        void TakeDamage(int damage);
        bool IsDestroyed { get; }
    }
}