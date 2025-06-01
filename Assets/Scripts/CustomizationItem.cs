using UnityEngine;

namespace CarBlade.Customization
{
    public enum CustomizationType
    {
        VehiclePaint,      // ���� ����
        Decal,            // ��Į
        Wheel,            // �� ������
        BoosterEffect,    // �ν��� ����Ʈ
        BladeTrail,       // ���̵� Ʈ����
        Horn              // ���� �Ҹ�
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

        // �����ۺ� ������
        public Color primaryColor;
        public Color secondaryColor;
        public Material material;
        public GameObject prefab;
        public AudioClip audioClip;
    }
}