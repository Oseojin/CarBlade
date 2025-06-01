namespace CarBlade.Physics
{
    // �ν��� �ý��� �������̽�
    public interface IBoosterSystem
    {
        float CurrentBoost { get; }
        void ChargeBoost(float amount);
        void ConsumeBoost();
    }
}