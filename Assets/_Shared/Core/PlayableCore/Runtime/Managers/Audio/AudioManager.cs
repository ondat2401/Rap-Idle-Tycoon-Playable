using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Amanotes.Core
{
    public class AudioManager : SingletonMonoDontDestroy<AudioManager>
    {
        #region Serialized Fields

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSourcePrefab;

        [Header("Audio Clips Database")]
        [SerializeField] private List<AudioClip> audioClips = new List<AudioClip>();

        [Header("Pool Settings")]
        [SerializeField] private int initialPoolSize = 5;
        [SerializeField] private int maxPoolSize = 20;

        [Header("Volume Settings")]
        [SerializeField, Range(0, 1)] private float masterVolume = 1f;
        [SerializeField, Range(0, 1)] private float bgmVolume = 1f;
        [SerializeField, Range(0, 1)] private float sfxVolume = 1f;

        #endregion

        #region Private Fields

        private Transform sfxContainer;
        private Dictionary<string, AudioClip> clipDict = new Dictionary<string, AudioClip>();
        private Queue<AudioSource> sfxPool = new Queue<AudioSource>();
        private List<AudioSource> activeSfxSources = new List<AudioSource>();

        private Coroutine bgmFadeCoroutine;
        private string currentBGM;

        // Cache để tránh tạo nhiều WaitForSeconds
        private readonly Dictionary<float, WaitForSeconds> waitCache = new Dictionary<float, WaitForSeconds>();

        #endregion

        #region Initialization

        protected override void Awake()
        {
            base.Awake();
            InitializeClipDictionary();
            InitializeSfxPool();
        }

        private void InitializeClipDictionary()
        {
            clipDict.Clear();
            foreach (var clip in audioClips)
            {
                if (clip == null)
                {
                    Debug.LogWarning("[AudioManager] Null clip in audioClips list");
                    continue;
                }

                if (clipDict.ContainsKey(clip.name))
                {
                    Debug.LogWarning($"[AudioManager] Duplicate clip name: {clip.name}");
                    continue;
                }

                clipDict[clip.name] = clip;
            }

            Debug.Log($"[AudioManager] Loaded {clipDict.Count} audio clips");
        }

        private void InitializeSfxPool()
        {
            sfxContainer = new GameObject("SFX Pool").transform;
            sfxContainer.SetParent(transform);

            for (int i = 0; i < initialPoolSize; i++)
            {
                CreatePooledSource(i);
            }

            Debug.Log($"[AudioManager] SFX Pool initialized with {initialPoolSize} sources");
        }

        private AudioSource CreatePooledSource(int index)
        {
            AudioSource source;

            if (sfxSourcePrefab != null)
            {
                source = UnityEngine.Object.Instantiate(sfxSourcePrefab, sfxContainer);
            }
            else
            {
                var obj = new GameObject();
                obj.transform.SetParent(sfxContainer);
                source = obj.AddComponent<AudioSource>();
                source.playOnAwake = false;
            }
            source.gameObject.name = $"SFX_Source_{index}";
            source.gameObject.SetActive(false);
            sfxPool.Enqueue(source);
            return source;
        }

        #endregion

        #region BGM Control

        /// <summary>
        /// Play background music with fade transition
        /// </summary>
        public void PlayBGM(string clipName, float fadeTime = 1f, float volume = 1f, bool loop = true)
        {
            AudioClip clip;
            if (!clipDict.TryGetValue(clipName, out clip))
            {
                Debug.LogWarning($"[AudioManager] BGM '{clipName}' not found!");
                return;
            }

            if (currentBGM == clipName && bgmSource.isPlaying)
            {
                Debug.Log($"[AudioManager] BGM '{clipName}' is already playing");
                return;
            }

            if (bgmFadeCoroutine != null)
                StopCoroutine(bgmFadeCoroutine);

            bgmFadeCoroutine = StartCoroutine(FadeToBGM(clip, fadeTime, volume, loop));
            currentBGM = clipName;
        }

        private IEnumerator FadeToBGM(AudioClip newClip, float fadeTime, float volume, bool loop)
        {
            // Fade out current BGM
            if (bgmSource.isPlaying && fadeTime > 0)
            {
                float startVol = bgmSource.volume;
                float elapsed = 0f;

                while (elapsed < fadeTime)
                {
                    elapsed += Time.unscaledDeltaTime;
                    bgmSource.volume = Mathf.Lerp(startVol, 0, elapsed / fadeTime);
                    yield return null;
                }

                bgmSource.Stop();
            }

            // Setup new clip
            bgmSource.clip = newClip;
            bgmSource.loop = loop;
            bgmSource.volume = 0f;
            bgmSource.Play();

            // Fade in
            if (fadeTime > 0)
            {
                float elapsed = 0f;
                float targetVolume = volume * bgmVolume * masterVolume;

                while (elapsed < fadeTime)
                {
                    elapsed += Time.unscaledDeltaTime;
                    bgmSource.volume = Mathf.Lerp(0, targetVolume, elapsed / fadeTime);
                    yield return null;
                }

                bgmSource.volume = targetVolume;
            }
            else
            {
                bgmSource.volume = volume * bgmVolume * masterVolume;
            }
        }

        public void StopBGM(float fadeTime = 1f)
        {
            if (bgmFadeCoroutine != null)
                StopCoroutine(bgmFadeCoroutine);

            bgmFadeCoroutine = StartCoroutine(FadeOutBGM(fadeTime));
            currentBGM = null;
        }

        private IEnumerator FadeOutBGM(float fadeTime)
        {
            if (!bgmSource.isPlaying) yield break;

            float startVol = bgmSource.volume;
            float elapsed = 0f;

            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVol, 0, elapsed / fadeTime);
                yield return null;
            }

            bgmSource.Stop();
            bgmSource.clip = null;
            bgmSource.volume = startVol;
        }

        public void PauseBGM() => bgmSource.Pause();
        public void ResumeBGM() => bgmSource.UnPause();
        public void SetBGMPitch(float pitch) => bgmSource.pitch = pitch;

        #endregion

        #region SFX Control (Pooled)

        /// <summary>
        /// Play SFX using pooled AudioSource
        /// </summary>
        public AudioSource PlaySFX(string clipName, float volume = 1f, float pitch = 1f)
        {
            AudioClip clip;
            if (!clipDict.TryGetValue(clipName, out clip))
            {
                Debug.LogWarning($"[AudioManager] SFX '{clipName}' not found!");
                return null;
            }

            var source = GetPooledSource();
            if (source == null)
            {
                Debug.LogWarning("[AudioManager] SFX pool exhausted!");
                return null;
            }

            ConfigureSfxSource(source, clip, volume, pitch);
            source.Play();

            StartCoroutine(ReturnToPoolWhenFinished(source, clip.length / pitch));
            return source;
        }

        /// <summary>
        /// Play SFX at specific world position (3D)
        /// </summary>
        public AudioSource PlaySFXAtPosition(string clipName, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            var source = PlaySFX(clipName, volume, pitch);
            if (source != null)
            {
                source.transform.position = position;
            }
            return source;
        }

        /// <summary>
        /// Play SFX with random pitch variation
        /// </summary>
        public AudioSource PlaySFXRandomPitch(string clipName, float minPitch = 0.9f, float maxPitch = 1.1f, float volume = 1f)
        {
            float pitch = UnityEngine.Random.Range(minPitch, maxPitch);
            return PlaySFX(clipName, volume, pitch);
        }

        /// <summary>
        /// Play OneShot (không cần return AudioSource)
        /// </summary>
        public void PlaySFXOneShot(string clipName, float volume = 1f)
        {
            AudioClip clip;
            if (!clipDict.TryGetValue(clipName, out clip))
            {
                Debug.LogWarning($"[AudioManager] SFX '{clipName}' not found!");
                return;
            }

            var source = GetPooledSource();
            if (source == null) return;

            source.volume = volume * sfxVolume * masterVolume;
            source.PlayOneShot(clip);

            StartCoroutine(ReturnToPoolWhenFinished(source, clip.length));
        }

        /// <summary>
        /// Play SFX OneShot directly from AudioClip reference
        /// </summary>
        public void PlaySFXOneShot(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            var source = GetPooledSource();
            if (source == null) return;
            source.spatialBlend = 0;
            source.volume = volume * sfxVolume * masterVolume;
            source.PlayOneShot(clip);
            StartCoroutine(ReturnToPoolWhenFinished(source, clip.length));
        }

        private void ConfigureSfxSource(AudioSource source, AudioClip clip, float volume, float pitch)
        {
            source.clip = clip;
            source.volume = volume * sfxVolume * masterVolume;
            source.pitch = pitch;
            source.loop = false;
            source.spatialBlend = 0;
        }

        private AudioSource GetPooledSource()
        {
            // Lấy từ pool
            if (sfxPool.Count > 0)
            {
                var source = sfxPool.Dequeue();
                source.gameObject.SetActive(true);
                activeSfxSources.Add(source);
                return source;
            }

            // Tạo mới nếu chưa đạt max
            if (activeSfxSources.Count < maxPoolSize)
            {
                var source = CreatePooledSource(maxPoolSize);
                sfxPool.Dequeue(); // Remove from queue
                source.gameObject.SetActive(true);
                activeSfxSources.Add(source);
                return source;
            }

            // Pool đầy, tìm source đã chơi xong
            foreach (var source in activeSfxSources)
            {
                if (!source.isPlaying)
                {
                    return source;
                }
            }

            return null; // Pool exhausted
        }

        private IEnumerator ReturnToPoolWhenFinished(AudioSource source, float duration)
        {
            yield return GetWaitForSeconds(duration);

            // Đảm bảo source đã chơi xong
            while (source.isPlaying)
            {
                yield return null;
            }

            ReturnSourceToPool(source);
        }

        private void ReturnSourceToPool(AudioSource source)
        {
            source.Stop();
            source.clip = null;
            source.gameObject.SetActive(false);
            activeSfxSources.Remove(source);
            sfxPool.Enqueue(source);
        }

        public void StopAllSFX()
        {
            foreach (var source in activeSfxSources)
            {
                if (source.isPlaying)
                {
                    source.Stop();
                }
            }
        }

        #endregion

        #region Volume Control

        public float MasterVolume
        {
            get => masterVolume;
            set
            {
                masterVolume = Mathf.Clamp01(value);
                UpdateAllVolumes();
            }
        }

        public float BGMVolume
        {
            get => bgmVolume;
            set
            {
                bgmVolume = Mathf.Clamp01(value);
                UpdateBGMVolume();
            }
        }

        public float SFXVolume
        {
            get => sfxVolume;
            set
            {
                sfxVolume = Mathf.Clamp01(value);
            }
        }

        private void UpdateAllVolumes()
        {
            UpdateBGMVolume();
        }

        private void UpdateBGMVolume()
        {
            if (bgmSource != null && bgmSource.isPlaying)
            {
                bgmSource.volume = bgmVolume * masterVolume;
            }
        }

        #endregion

        #region Utilities

        public bool IsBGMPlaying => bgmSource != null && bgmSource.isPlaying;
        public string CurrentBGM => currentBGM;
        public int ActiveSFXCount => activeSfxSources.Count;
        public int PooledSFXCount => sfxPool.Count;

        public bool HasClip(string clipName) => clipDict.ContainsKey(clipName);

        public AudioClip GetClip(string clipName)
        {
            AudioClip clip;
            if (clipDict.TryGetValue(clipName, out clip))
                return clip;
            return null;
        }

        private WaitForSeconds GetWaitForSeconds(float duration)
        {
            WaitForSeconds wait;
            if (!waitCache.TryGetValue(duration, out wait))
            {
                wait = new WaitForSeconds(duration);
                waitCache[duration] = wait;
            }
            return wait;
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        [ContextMenu("Debug: List All Clips")]
        private void DebugListClips()
        {
            Debug.Log($"=== AudioManager Clips ({clipDict.Count}) ===");
            foreach (var kvp in clipDict)
            {
                Debug.Log($"- {kvp.Key} ({kvp.Value.length:F2}s)");
            }
        }

        [ContextMenu("Debug: Pool Status")]
        private void DebugPoolStatus()
        {
            Debug.Log($"=== SFX Pool Status ===\n" +
                      $"Active: {activeSfxSources.Count}\n" +
                      $"Pooled: {sfxPool.Count}\n" +
                      $"Max: {maxPoolSize}");
        }

        [ContextMenu("Reload Audio Database")]
        private void ReloadDatabase()
        {
            InitializeClipDictionary();
        }
#endif

        #endregion
    }
}
