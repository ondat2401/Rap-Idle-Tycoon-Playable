using System;
using System.Collections.Generic;
using _Playable.Runtime.Core;
using UnityEngine;

namespace _Playable.Runtime.Config
{
    /// <summary>
    /// Cau hinh am thanh cua playable: doi chieu tung <see cref="PlayableSfx"/> voi mot AudioClip va am luong.
    /// Tach rieng thanh ScriptableObject de gan/thay clip qua Inspector, khong can build lai scene.
    ///
    /// Beat nen va giong rap la hai track lap chay doc lap nen tach thanh field rieng cho de chinh.
    /// Cac sfx one-shot con lai nam trong mang <see cref="_entries"/>.
    /// Slot de trong (Clip == null) = clip chua co -> he thong bo qua im lang, khong nem loi.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayableAudioConfig", menuName = "Playable/Playable Audio Config", order = 2)]
    public sealed class PlayableAudioConfig : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public PlayableSfx Sfx;
            public AudioClip Clip;

            [Range(0f, 1f)]
            public float Volume;
        }

        [Header("Beat nen (track lap)")]
        [LunaPlaygroundAsset ("Beat",0,"Song Config")] 
        [SerializeField] private AudioClip _beatClip;

        [LunaPlaygroundAsset ("Beat Volume",1,"Song Config")]
        [Range(0f, 1f)]
        [SerializeField] private float _beatVolume = 1f;

        [Header("Giong rap (track lap)")]
        [LunaPlaygroundAsset ("Vocal",2,"Song Config")]
        [SerializeField] private AudioClip _vocalClip;

        [LunaPlaygroundAsset ("Vocal Volume",3,"Song Config")]
        [Range(0f, 1f)]
        [SerializeField] private float _vocalVolume = 1f;

        [Header("SFX one-shot - de trong nhung clip chua co")]
        [Tooltip("Moi dong noi mot su kien sfx voi clip va am luong tuong ung. Khong gom beat/vocal.")]
        [SerializeField]
        private Entry[] _entries = new Entry[0];

        public IReadOnlyList<Entry> Entries => this._entries;

        public AudioClip BeatClip => this._beatClip;

        public float BeatVolume => this._beatVolume;

        public AudioClip VocalClip => this._vocalClip;

        public float VocalVolume => this._vocalVolume;

        /// <summary>Ghi len dictionary cac sfx one-shot hop le (bo qua slot None hoac chua co clip).</summary>
        public void PopulateMap(Dictionary<PlayableSfx, Entry> map)
        {
            if (map == null || this._entries == null)
            {
                return;
            }

            foreach (Entry entry in this._entries)
            {
                if (entry.Sfx == PlayableSfx.None || entry.Clip == null)
                {
                    continue;
                }

                map[entry.Sfx] = entry;
            }
        }
    }
}
