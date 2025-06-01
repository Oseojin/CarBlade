using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using CarBlade.Physics;
using CarBlade.Combat;

namespace CarBlade.AudioVFX
{
    // 오디오 매니저
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AudioManager>();
                }
                return _instance;
            }
        }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource engineSource;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip[] engineSounds;
        [SerializeField] private AudioClip[] collisionSounds;
        [SerializeField] private AudioClip[] bladeClashSounds;
        [SerializeField] private AudioClip[] hornSounds;
        [SerializeField] private AudioClip boosterActivateSound;
        [SerializeField] private AudioClip destructionSound;
        [SerializeField] private AudioClip driftSound;

        [Header("Settings")]
        [SerializeField] private AnimationCurve enginePitchCurve;
        [SerializeField] private AnimationCurve engineVolumeCurve;
        [SerializeField] private float maxEngineVolume = 0.7f;

        // 3D 오디오 소스 풀
        private Queue<AudioSource> audioSourcePool = new Queue<AudioSource>();
        private const int POOL_SIZE = 20;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
            CreateAudioSourcePool();
        }

        private void InitializeAudioSources()
        {
            // 2D 오디오 소스 설정
            if (musicSource == null)
            {
                GameObject musicObj = new GameObject("MusicSource");
                musicObj.transform.SetParent(transform);
                musicSource = musicObj.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.spatialBlend = 0f;
                musicSource.volume = 0.5f;
            }

            if (sfxSource == null)
            {
                GameObject sfxObj = new GameObject("SFXSource");
                sfxObj.transform.SetParent(transform);
                sfxSource = sfxObj.AddComponent<AudioSource>();
                sfxSource.spatialBlend = 0f;
            }

            if (engineSource == null)
            {
                GameObject engineObj = new GameObject("EngineSource");
                engineObj.transform.SetParent(transform);
                engineSource = engineObj.AddComponent<AudioSource>();
                engineSource.spatialBlend = 0f;
                engineSource.loop = true;
                engineSource.volume = maxEngineVolume;
            }
        }

        private void CreateAudioSourcePool()
        {
            GameObject poolContainer = new GameObject("AudioSourcePool");
            poolContainer.transform.SetParent(transform);

            for (int i = 0; i < POOL_SIZE; i++)
            {
                GameObject sourceObj = new GameObject($"PooledAudioSource_{i}");
                sourceObj.transform.SetParent(poolContainer.transform);
                AudioSource source = sourceObj.AddComponent<AudioSource>();
                source.spatialBlend = 1f; // 3D sound
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 5f;
                source.maxDistance = 50f;
                source.gameObject.SetActive(false);
                audioSourcePool.Enqueue(source);
            }
        }

        // 엔진 사운드 재생 (로컬 플레이어용)
        public void PlayEngineSound(float normalizedSpeed)
        {
            if (engineSource == null || engineSounds.Length == 0) return;

            if (!engineSource.isPlaying && engineSounds[0] != null)
            {
                engineSource.clip = engineSounds[0];
                engineSource.Play();
            }

            // 속도에 따른 피치와 볼륨 조절
            engineSource.pitch = enginePitchCurve.Evaluate(normalizedSpeed);
            engineSource.volume = engineVolumeCurve.Evaluate(normalizedSpeed) * maxEngineVolume;
        }

        // 충돌 사운드 재생
        public void PlayCollision(Vector3 position, float impactForce)
        {
            if (collisionSounds.Length == 0) return;

            AudioClip clip = collisionSounds[Random.Range(0, collisionSounds.Length)];
            float volume = Mathf.Clamp01(impactForce / 50f);
            Play3DSound(clip, position, volume);
        }

        // 블레이드 클래시 사운드
        public void PlayBladeClash(Vector3 position)
        {
            if (bladeClashSounds.Length == 0) return;

            AudioClip clip = bladeClashSounds[Random.Range(0, bladeClashSounds.Length)];
            Play3DSound(clip, position, 1f);
        }

        // 경적 재생
        public void PlayHorn(Vector3 position, int hornId)
        {
            if (hornSounds.Length == 0 || hornId < 0 || hornId >= hornSounds.Length) return;

            Play3DSound(hornSounds[hornId], position, 1f);
        }

        // 부스터 활성화 사운드
        public void PlayBoosterActivate(Vector3 position)
        {
            if (boosterActivateSound != null)
            {
                Play3DSound(boosterActivateSound, position, 0.8f);
            }
        }

        // 파괴 사운드
        public void PlayDestruction(Vector3 position)
        {
            if (destructionSound != null)
            {
                Play3DSound(destructionSound, position, 1f);
            }
        }

        // 드리프트 사운드
        public void PlayDrift(Vector3 position)
        {
            if (driftSound != null)
            {
                Play3DSound(driftSound, position, 0.6f);
            }
        }

        // 2D 사운드 재생 (UI 등)
        public void PlayUISound(AudioClip clip, float volume = 1f)
        {
            if (sfxSource != null && clip != null)
            {
                sfxSource.PlayOneShot(clip, volume);
            }
        }

        // 3D 사운드 재생
        private void Play3DSound(AudioClip clip, Vector3 position, float volume)
        {
            if (clip == null) return;

            AudioSource source = GetPooledAudioSource();
            if (source != null)
            {
                source.transform.position = position;
                source.clip = clip;
                source.volume = volume;
                source.Play();

                StartCoroutine(ReturnToPool(source, clip.length));
            }
        }

        private AudioSource GetPooledAudioSource()
        {
            if (audioSourcePool.Count > 0)
            {
                AudioSource source = audioSourcePool.Dequeue();
                source.gameObject.SetActive(true);
                return source;
            }

            Debug.LogWarning("Audio pool exhausted!");
            return null;
        }

        private IEnumerator ReturnToPool(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay + 0.1f);

            source.Stop();
            source.clip = null;
            source.gameObject.SetActive(false);
            audioSourcePool.Enqueue(source);
        }

        // 볼륨 설정
        public void SetMasterVolume(float volume)
        {
            AudioListener.volume = volume;
        }

        public void SetMusicVolume(float volume)
        {
            if (musicSource != null)
                musicSource.volume = volume;
        }

        public void SetSFXVolume(float volume)
        {
            if (sfxSource != null)
                sfxSource.volume = volume;
        }
    }
}