namespace CarBlade.Combat
{
    // ü�� �ý��� �������̽�
    public interface IHealthSystem
    {
        int CurrentHealth { get; }
        void TakeDamage(int damage);
        bool IsDestroyed { get; }
    }
}