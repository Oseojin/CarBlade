namespace CarBlade.Customization
{
    [System.Serializable]
    public class SkinData
    {
        public int paintId;
        public int decalId;
        public int wheelId;
        public int boosterEffectId;
        public int bladeTrailId;
        public int hornId;

        public SkinData()
        {
            // 기본값 설정
            paintId = 0;
            decalId = 0;
            wheelId = 0;
            boosterEffectId = 0;
            bladeTrailId = 0;
            hornId = 0;
        }
    }
}