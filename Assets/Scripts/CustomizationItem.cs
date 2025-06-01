using UnityEngine;

namespace CarBlade.Customization
{
    public enum CustomizationType
    {
        VehiclePaint,      // 차량 도색
        Decal,            // 데칼
        Wheel,            // 휠 디자인
        BoosterEffect,    // 부스터 이펙트
        BladeTrail,       // 블레이드 트레일
        Horn              // 경적 소리
    }

    [System.Serializable]
    public class CustomizationItem
    {
        public int itemId;
        public string itemName;
        public string description;
        public CustomizationType type;
        public int gpCost;
        public Sprite preview;
        public bool isUnlocked;

        // 아이템별 데이터
        public Color primaryColor;
        public Color secondaryColor;
        public Material material;
        public GameObject prefab;
        public AudioClip audioClip;
    }
}