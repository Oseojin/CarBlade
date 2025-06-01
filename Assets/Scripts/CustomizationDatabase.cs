
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

namespace CarBlade.Customization
{
    [CreateAssetMenu(fileName = "CustomizationDatabase", menuName = "CarBlade/Customization Database", order = 3)]
    public class CustomizationDatabase : ScriptableObject
    {
        [SerializeField] private List<CustomizationItem> items = new List<CustomizationItem>();

        public CustomizationItem GetItem(int itemId)
        {
            return items.FirstOrDefault(item => item.itemId == itemId);
        }

        public List<CustomizationItem> GetAllItems()
        {
            return new List<CustomizationItem>(items);
        }

        // 에디터용 헬퍼 메서드
#if UNITY_EDITOR
        public void AddItem(CustomizationItem item)
        {
            if (item != null && !items.Any(i => i.itemId == item.itemId))
            {
                items.Add(item);
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }

        public void RemoveItem(int itemId)
        {
            items.RemoveAll(item => item.itemId == itemId);
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}