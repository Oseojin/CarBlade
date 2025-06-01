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
        [SerializeField] private int playerGP = 1000; // 시작 GP
        private List<int> ownedItemIds = new List<int>();
        private Dictionary<int, SkinData> vehicleSkins = new Dictionary<int, SkinData>();

        // 이벤트
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

            // 기본 아이템 잠금 해제
            UnlockDefaultItems();

            // 저장된 데이터 로드
            LoadPlayerData();
        }

        // ICustomizationSystem 구현
        public void ApplySkin(int vehicleId, SkinData skin)
        {
            vehicleSkins[vehicleId] = skin;
            OnSkinApplied?.Invoke(vehicleId, skin);

            // 저장
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

            // 이미 보유중인지 확인
            if (ownedItemIds.Contains(itemId))
            {
                Debug.LogWarning($"Item {item.itemName} already owned");
                return;
            }

            // GP 확인
            if (playerGP < item.gpCost)
            {
                Debug.LogWarning($"Not enough GP. Need {item.gpCost}, have {playerGP}");
                return;
            }

            // 구매 처리
            playerGP -= item.gpCost;
            ownedItemIds.Add(itemId);
            item.isUnlocked = true;

            OnGPChanged?.Invoke(playerGP);
            OnItemPurchased?.Invoke(item);

            // 저장
            SavePlayerData();

            Debug.Log($"Purchased {item.itemName} for {item.gpCost} GP");
        }

        public List<CustomizationItem> GetOwnedItems()
        {
            return database.GetAllItems()
                .Where(item => ownedItemIds.Contains(item.itemId))
                .ToList();
        }

        // GP 관련 메서드
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

        // 아이템 조회
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

        // 차량에 스킨 적용
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

        // 기본 아이템 잠금 해제
        private void UnlockDefaultItems()
        {
            // 기본 페인트, 휠 등 잠금 해제
            ownedItemIds.AddRange(new int[] { 0, 100, 200, 300, 400, 500 });
        }

        // 데이터 저장/로드
        private void SavePlayerData()
        {
            // PlayerPrefs를 사용한 간단한 저장
            PlayerPrefs.SetInt("PlayerGP", playerGP);

            // 보유 아이템 저장
            string ownedItemsJson = string.Join(",", ownedItemIds);
            PlayerPrefs.SetString("OwnedItems", ownedItemsJson);

            // 차량 스킨 저장
            foreach (var kvp in vehicleSkins)
            {
                string skinJson = JsonUtility.ToJson(kvp.Value);
                PlayerPrefs.SetString($"VehicleSkin_{kvp.Key}", skinJson);
            }

            PlayerPrefs.Save();
        }

        private void LoadPlayerData()
        {
            // GP 로드
            playerGP = PlayerPrefs.GetInt("PlayerGP", 1000);

            // 보유 아이템 로드
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

            // 차량 스킨 로드
            for (int i = 0; i < 3; i++) // 3가지 차량 타입
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