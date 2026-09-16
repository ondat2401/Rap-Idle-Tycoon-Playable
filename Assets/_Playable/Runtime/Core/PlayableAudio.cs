using System;
using System.Collections.Generic;
using _Playable.Runtime.Config;
using UnityEngine;

namespace _Playable.Runtime.Core
{
    /// <summary>
    /// Phat am thanh cho playable. Mot AudioSource cho sfx one-shot, hai AudioSource rieng cho beat va vocal
    /// (hai thu nay chay lap va bi dung doc lap nhau, giong ban goc).
    /// Slot bo trong = clip chua co -> bo qua im lang, khong nem loi.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayableAudio : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            public PlayableSfx Sfx;
            public AudioClip Clip;

            [Range(0f, 1f)]
            public float Volume;
        }

        [Header("Config")]
        [Tooltip("Nguon am thanh chinh. Neu gan config nay thi lay clip tu day, bo qua _entries inline.")]
        [SerializeField] private PlayableAudioConfig _config;

        [Header("Clip map inline (fallback khi khong co config)")]
        [SerializeField] private Entry[] _entries = new Entry[0];

        [Header("Source")]
        [SerializeField] private AudioSource _sfxSource;
        [SerializeField] private AudioSource _beatSource;
        [SerializeField] private AudioSource _vocalSource;

        [SerializeField] private bool _muted;

        private readonly Dictionary<PlayableSfx, PlayableAudioConfig.Entry> _map =
            new Dictionary<PlayableSfx, PlayableAudioConfig.Entry>();

        private void Awake()
        {
            this.BuildMap();
            this.EnsureSources();
        }

        private void BuildMap()
        {
            this._map.Clear();

            // Uu tien config SO. Neu khong co config thi dung _entries inline lam fallback.
            if (this._config != null)
            {
                this._config.PopulateMap(this._map);
                return;
            }

            if (this._entries == null)
            {
                return;
            }

            foreach (Entry entry in this._entries)
            {
                if (entry.Sfx == PlayableSfx.None || entry.Clip == null)
                {
                    continue;
                }

                this._map[entry.Sfx] = new PlayableAudioConfig.Entry
                {
                    Sfx = entry.Sfx,
                    Clip = entry.Clip,
                    Volume = entry.Volume
                };
            }
        }

        private void EnsureSources()
        {
            this._sfxSource = this.EnsureSource(this._sfxSource, "SfxSource", false);
            this._beatSource = this.EnsureSource(this._beatSource, "BeatSource", true);
            this._vocalSource = this.EnsureSource(this._vocalSource, "VocalSource", true);
        }

        private AudioSource EnsureSource(AudioSource source, string sourceName, bool loop)
        {
            if (source != null)
            {
                return source;
            }

            var child = new GameObject(sourceName);
            child.transform.SetParent(this.transform, false);
            AudioSource created = child.AddComponent<AudioSource>();
            created.playOnAwake = false;
            created.loop = loop;
            return created;
        }

        /// <summary>Phat mot lan. Bo qua neu chua co clip.</summary>
        public void Play(PlayableSfx sfx)
        {
            if (this._muted || !this._map.TryGetValue(sfx, out PlayableAudioConfig.Entry entry))
            {
                return;
            }

            this._sfxSource.PlayOneShot(entry.Clip, Mathf.Approximately(entry.Volume, 0f) ? 1f : entry.Volume);
        }

        /// <summary>Bat beat nen lap. Lay clip tu config SO.</summary>
        public void PlayBeatLoop()
        {
            if (this._config == null)
            {
                return;
            }

            this.PlayLoop(this._beatSource, this._config.BeatClip, this._config.BeatVolume);
        }

        /// <summary>Bat giong rap lap. Lay clip tu config SO.</summary>
        public void PlayVocalLoop()
        {
            if (this._config == null)
            {
                return;
            }

            this.PlayLoop(this._vocalSource, this._config.VocalClip, this._config.VocalVolume);
        }

        public void StopVocal()
        {
            if (this._vocalSource != null)
            {
                this._vocalSource.Stop();
            }
        }

        public void StopAll()
        {
            if (this._sfxSource != null)
            {
                this._sfxSource.Stop();
            }

            if (this._beatSource != null)
            {
                this._beatSource.Stop();
            }

            if (this._vocalSource != null)
            {
                this._vocalSource.Stop();
            }
        }

        private void PlayLoop(AudioSource source, AudioClip clip, float volume)
        {
            if (this._muted || source == null || clip == null)
            {
                return;
            }

            source.clip = clip;
            source.volume = Mathf.Approximately(volume, 0f) ? 1f : volume;
            source.loop = true;
            source.Play();
        }
    }
}
