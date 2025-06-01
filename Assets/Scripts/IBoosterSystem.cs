namespace CarBlade.Physics
{
    // 부스터 시스템 인터페이스
    public interface IBoosterSystem
    {
        float CurrentBoost { get; }
        void ChargeBoost(float amount);
        void ConsumeBoost();
    }
}