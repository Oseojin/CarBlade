using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace CarBlade.Customization
{
    public class CustomizationSystem : MonoBehaviour, ICustomizationSystem
    {
        private static CustomizationSystem _instance;
        public static CustomizationSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CustomizationSystem>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("CustomizationSystem");
                        _instance = go.AddComponent<CustomizationSystem>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("Customization Database")]
        [SerializeField] private CustomizationDatabase database;

        [Header("Player Data")]
        [SerializeField] private int playerGP = 1000; // ���� GP
        private List<int> ownedItemIds = new List<int>();
        private Dictionary<int, SkinData> vehicleSkins = new Dictionary<int, SkinData>();

        // �̺�Ʈ
        public event System.Action<int> OnGPChanged;
        public event System.Action<CustomizationItem> OnItemPurchased;
        public event System.Action<int, SkinData> OnSkinApplied;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            // �⺻ ������ ��� ����
            UnlockDefaultItems();

            // ����� ������ �ε�
            LoadPlayerData();
        }

        // ICustomizationSystem ����
        public void ApplySkin(int vehicleId, SkinData skin)
        {
            vehicleSkins[vehicleId] = skin;
            OnSkinApplied?.Invoke(vehicleId, skin);

            // ����
            SavePlayerData();

            Debug.Log($"Applied skin to vehicle {vehicleId}");
        }

        public void PurchaseItem(int itemId)
        {
            var item = database.GetItem(itemId);
            if (item == null)
            {
                Debug.LogError($"Item {itemId} not found in database");
                return;
            }

            // �̹� ���������� Ȯ��
            if (ownedItemIds.Contains(itemId))
            {
                Debug.LogWarning($"Item {item.itemName} already owned");
                return;
            }

            // GP Ȯ��
            if (playerGP < item.gpCost)
            {
                Debug.LogWarning($"Not enough GP. Need {item.gpCost}, have {playerGP}");
                return;
            }

            // ���� ó��
            playerGP -= item.gpCost;
            ownedItemIds.Add(itemId);
            item.isUnlocked = true;

            OnGPChanged?.Invoke(playerGP);
            OnItemPurchased?.Invoke(item);

            // ����
            SavePlayerData();

            Debug.Log($"Purchased {item.itemName} for {item.gpCost} GP");
        }

        public List<CustomizationItem> GetOwnedItems()
        {
            return database.GetAllItems()
                .Where(item => ownedItemIds.Contains(item.itemId))
                .ToList();
        }

        // GP ���� �޼���
        public int GetPlayerGP()
        {
            return playerGP;
        }

        public void AddGP(int amount)
        {
            playerGP += amount;
            OnGPChanged?.Invoke(playerGP);
            SavePlayerData();

            Debug.Log($"Added {amount} GP. Total: {playerGP}");
        }

        // ������ ��ȸ
        public List<CustomizationItem> GetItemsByType(CustomizationType type)
        {
            return database.GetAllItems()
                .Where(item => item.type == type)
                .ToList();
        }

        public bool IsItemOwned(int itemId)
        {
            return ownedItemIds.Contains(itemId);
        }

        // ������ ��Ų ����
        public void ApplySkinToVehicle(GameObject vehicle, SkinData skin)
        {
            if (vehicle == null || skin == null) return;

            var skinApplier = vehicle.GetComponent<VehicleSkinApplier>();
            if (skinApplier == null)
            {
                skinApplier = vehicle.AddComponent<VehicleSkinApplier>();
            }

            skinApplier.ApplySkin(skin, database);
        }

        // �⺻ ������ ��� ����
        private void UnlockDefaultItems()
        {
            // �⺻ ����Ʈ, �� �� ��� ����
            ownedItemIds.AddRange(new int[] { 0, 100, 200, 300, 400, 500 });
        }

        // ������ ����/�ε�
        private void SavePlayerData()
        {
            // PlayerPrefs�� ����� ������ ����
            PlayerPrefs.SetInt("PlayerGP", playerGP);

            // ���� ������ ����
            string ownedItemsJson = string.Join(",", ownedItemIds);
            PlayerPrefs.SetString("OwnedItems", ownedItemsJson);

            // ���� ��Ų ����
            foreach (var kvp in vehicleSkins)
            {
                string skinJson = JsonUtility.ToJson(kvp.Value);
                PlayerPrefs.SetString($"VehicleSkin_{kvp.Key}", skinJson);
            }

            PlayerPrefs.Save();
        }

        private void LoadPlayerData()
        {
            // GP �ε�
            playerGP = PlayerPrefs.GetInt("PlayerGP", 1000);

            // ���� ������ �ε�
            string ownedItemsJson = PlayerPrefs.GetString("OwnedItems", "");
            if (!string.IsNullOrEmpty(ownedItemsJson))
            {
                string[] itemIds = ownedItemsJson.Split(',');
                foreach (string id in itemIds)
                {
                    if (int.TryParse(id, out int itemId))
                    {
                        ownedItemIds.Add(itemId);
                    }
                }
            }

            // ���� ��Ų �ε�
            for (int i = 0; i < 3; i++) // 3���� ���� Ÿ��
            {
                string skinJson = PlayerPrefs.GetString($"VehicleSkin_{i}", "");
                if (!string.IsNullOrEmpty(skinJson))
                {
                    SkinData skin = JsonUtility.FromJson<SkinData>(skinJson);
                    vehicleSkins[i] = skin;
                }
            }
        }
    }
}