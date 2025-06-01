using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using CarBlade.Physics;
using CarBlade.Combat;

namespace CarBlade.AudioVFX
{
    public class VFXManager : MonoBehaviour
    {
        private static VFXManager _instance;
        public static VFXManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<VFXManager>();
                }
                return _instance;
            }
        }

        [Header("Effect Prefabs")]
        [SerializeField] private GameObject bladeClashEffect;
        [SerializeField] private GameObject sparksEffect;
        [SerializeField] private GameObject destructionEffect;
        [SerializeField] private GameObject boosterEffect;
        [SerializeField] private GameObject driftSmokeEffect;
        [SerializeField] private GameObject speedLinesEffect;

        [Header("Pool Settings")]
        [SerializeField] private int defaultPoolSize = 10;

        private Dictionary<string, Queue<GameObject>> effectPools = new Dictionary<string, Queue<GameObject>>();
        private Dictionary<string, GameObject> effectPrefabs = new Dictionary<string, GameObject>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeEffectPools();
        }

        private void InitializeEffectPools()
        {
            // ������ ���
            RegisterEffectPrefab("BladeClash", bladeClashEffect);
            RegisterEffectPrefab("Sparks", sparksEffect);
            RegisterEffectPrefab("Destruction", destructionEffect);
            RegisterEffectPrefab("Booster", boosterEffect);
            RegisterEffectPrefab("DriftSmoke", driftSmokeEffect);
            RegisterEffectPrefab("SpeedLines", speedLinesEffect);

            // Ǯ ����
            foreach (var kvp in effectPrefabs)
            {
                CreatePool(kvp.Key, kvp.Value, defaultPoolSize);
            }
        }

        private void RegisterEffectPrefab(string effectName, GameObject prefab)
        {
            if (prefab != null)
            {
                effectPrefabs[effectName] = prefab;
            }
        }

        private void CreatePool(string poolName, GameObject prefab, int size)
        {
            if (prefab == null) return;

            GameObject poolContainer = new GameObject($"Pool_{poolName}");
            poolContainer.transform.SetParent(transform);

            Queue<GameObject> pool = new Queue<GameObject>();

            for (int i = 0; i < size; i++)
            {
                GameObject obj = Instantiate(prefab, poolContainer.transform);
                obj.name = $"{poolName}_{i}";
                obj.SetActive(false);
                pool.Enqueue(obj);
            }

            effectPools[poolName] = pool;
        }

        // ���̵� Ŭ���� ����Ʈ
        public void SpawnBladeClash(Vector3 position, Quaternion rotation)
        {
            GameObject effect = GetEffect("BladeClash");
            if (effect != null)
            {
                effect.transform.position = position;
                effect.transform.rotation = rotation;
                effect.SetActive(true);
                StartCoroutine(ReturnToPool(effect, "BladeClash", 2f));

                // ����ũ �߰�
                GameObject sparks = GetEffect("Sparks");
                if (sparks != null)
                {
                    sparks.transform.position = position;
                    sparks.transform.rotation = rotation;
                    sparks.SetActive(true);
                    StartCoroutine(ReturnToPool(sparks, "Sparks", 1f));
                }
            }

            // ���� ���
            AudioManager.Instance?.PlayBladeClash(position);
        }

        // �ı� ����Ʈ
        public void SpawnDestruction(Vector3 position)
        {
            GameObject effect = GetEffect("Destruction");
            if (effect != null)
            {
                effect.transform.position = position;
                effect.transform.rotation = Quaternion.identity;
                effect.SetActive(true);
                StartCoroutine(ReturnToPool(effect, "Destruction", 3f));
            }

            AudioManager.Instance?.PlayDestruction(position);
        }

        // ���� ����Ʈ ����
        public GameObject AttachBoosterEffect(Transform vehicle)
        {
            GameObject effect = GetEffect("Booster");
            if (effect != null)
            {
                effect.transform.SetParent(vehicle);
                effect.transform.localPosition = new Vector3(0, 0.5f, -2f);
                effect.transform.localRotation = Quaternion.identity;
                effect.SetActive(true);
            }
            return effect;
        }

        public GameObject AttachDriftSmoke(Transform vehicle)
        {
            GameObject effect = GetEffect("DriftSmoke");
            if (effect != null)
            {
                effect.transform.SetParent(vehicle);
                effect.transform.localPosition = new Vector3(0, 0.1f, -1.5f);
                effect.transform.localRotation = Quaternion.identity;
                effect.SetActive(true);
            }
            return effect;
        }

        // ����Ʈ ��ȯ
        public void ReturnEffect(GameObject effect, string effectName)
        {
            if (effect == null || !effectPools.ContainsKey(effectName)) return;

            effect.SetActive(false);
            effect.transform.SetParent(transform);
            effectPools[effectName].Enqueue(effect);
        }

        private GameObject GetEffect(string effectName)
        {
            if (!effectPools.ContainsKey(effectName) || effectPools[effectName].Count == 0)
            {
                Debug.LogWarning($"Effect pool '{effectName}' is empty!");
                return null;
            }

            return effectPools[effectName].Dequeue();
        }

        private IEnumerator ReturnToPool(GameObject effect, string effectName, float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnEffect(effect, effectName);
        }
    }
}