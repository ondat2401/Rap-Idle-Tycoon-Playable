using System;
using System.Collections;
using System.Collections.Generic;
using _Playable.Runtime.Config;
using _Playable.Runtime.Core;
using UnityEngine;

namespace _Playable.Runtime.View
{
    /// <summary>
    /// Prefab hien thi map cua playable: nen + cac slot vat the (nha, xe, san, loa, tuong...) + nhan vat.
    ///
    /// Lam theo dung <c>_Project.Features.Map.Runtime.MainMap</c>: prefab nam trong world space, moi slot
    /// la mot <see cref="SpriteRenderer"/> duoc Main Camera nhin; sorting layer/order cua tung renderer do
    /// prefab quy dinh, code khong dung toi. UI (Canvas overlay) ve de len tren.
    ///
    /// Slot khong co sprite cho level duoc yeu cau (vuot do dai mang hoac phan tu null) thi bi AN,
    /// giong MainMap an slot khi khong load duoc sprite.
    ///
    /// Khac ban goc chi o phan du lieu, do rang buoc package doc lap:
    /// sprite gan san theo mang trong Inspector thay vi load qua Addressables, va bang slot la mang
    /// <see cref="PlayableMapSlotBinding"/> thay vi Dictionary cua Odin.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayableMainMap : MonoBehaviour
    {
        /// <summary>Mot slot tren map: renderer cua no va danh sach sprite theo level.</summary>
        [Serializable]
        public struct PlayableMapSlotBinding
        {
            public PlayableMapSlot Slot;

            public SpriteRenderer Renderer;

            [Tooltip("Level 0 la trang thai khoi dau, 1..n la cac ban nang cap. " +
                     "Phan tu null hoac level vuot mang => slot bi an o level do.")]
            public Sprite[] Levels;
        }

        [SerializeField] private PlayableMapSlotBinding[] _slots = new PlayableMapSlotBinding[0];

        [Header("Hieu ung doi vat the")]
        [Tooltip("Khoang lech X (toa do local cua slot). Clone truot tu (x - offset) ve x cua slot goc, " +
                 "vat the cu truot tu x ra (x + offset). Vi du car goc (1.3, 1.14): clone tu (-3.7, 1.14) ve " +
                 "(1.3, 1.14), vat the cu ra (6.3, 1.14).")]
        [SerializeField] private float _slideOffsetX = 5f;

        [Header("Nhan vat tren map")]
        [SerializeField] private PlayableManView _man;

        [Tooltip("Mot the cho moi lua chon ban gai o buoc 1 - thu tu khop voi thu tu nut.")]
        [SerializeField] private PlayableWomanView[] _womanVariants = new PlayableWomanView[0];

        private readonly Dictionary<PlayableMapSlot, int> _currentLevels = new Dictionary<PlayableMapSlot, int>();
        private readonly Dictionary<PlayableMapSlot, Vector3> _basePositions = new Dictionary<PlayableMapSlot, Vector3>();
        private readonly Dictionary<PlayableMapSlot, SpriteRenderer> _activeClones = new Dictionary<PlayableMapSlot, SpriteRenderer>();

        private Dictionary<PlayableMapSlot, PlayableMapSlotBinding> _lookup;

        private Dictionary<PlayableMapSlot, PlayableMapSlotBinding> Lookup
        {
            get
            {
                if (this._lookup != null)
                {
                    return this._lookup;
                }

                this._lookup = new Dictionary<PlayableMapSlot, PlayableMapSlotBinding>();
                foreach (PlayableMapSlotBinding binding in this._slots)
                {
                    if (binding.Renderer == null)
                    {
                        continue;
                    }

                    this._lookup[binding.Slot] = binding;
                    this._basePositions[binding.Slot] = binding.Renderer.transform.localPosition;
                }

                return this._lookup;
            }
        }

        public PlayableManView Man => this._man;

        /// <summary>Ban gai da duoc chon o buoc 1. Null truoc khi chon.</summary>
        public PlayableWomanView Woman { get; private set; }

        /// <summary>Dua moi slot ve level 0 va vi tri goc, don clone con sot, an moi the ban gai.</summary>
        public void ResetToBase()
        {
            this._currentLevels.Clear();

            foreach (KeyValuePair<PlayableMapSlot, PlayableMapSlotBinding> pair in this.Lookup)
            {
                this.DestroyClone(pair.Key);
                pair.Value.Renderer.transform.localPosition = this._basePositions[pair.Key];
                this.SetLevelInstant(pair.Key, 0);
            }

            this.Woman = null;
            if (this._womanVariants != null)
            {
                foreach (PlayableWomanView variant in this._womanVariants)
                {
                    variant?.Initialize();
                }
            }
        }

        /// <summary>Chon the ban gai theo index nut, an cac the con lai.</summary>
        public PlayableWomanView SelectWoman(int index)
        {
            if (this._womanVariants == null || this._womanVariants.Length == 0)
            {
                Debug.LogWarning("[Playable] PlayableMainMap chua gan PlayableWomanView nao.", this);
                return null;
            }

            int clamped = Mathf.Clamp(index, 0, this._womanVariants.Length - 1);
            for (int i = 0; i < this._womanVariants.Length; i++)
            {
                if (i != clamped)
                {
                    this._womanVariants[i]?.SetVisible(false);
                }
            }

            this.Woman = this._womanVariants[clamped];
            return this.Woman;
        }

        public bool HasSlot(PlayableMapSlot slot)
        {
            return this.Lookup.ContainsKey(slot);
        }

        /// <summary>Sprite cua mot level, null neu slot khong co level do.</summary>
        public Sprite GetSprite(PlayableMapSlot slot, int level)
        {
            return this.TryResolve(slot, level, out _, out Sprite sprite) ? sprite : null;
        }

        public void SetLevelInstant(PlayableMapSlot slot, int level)
        {
            if (!this.TryResolve(slot, level, out SpriteRenderer renderer, out Sprite sprite))
            {
                return;
            }

            this._currentLevels[slot] = level;
            renderer.sprite = sprite;
            renderer.enabled = sprite != null;
        }

        /// <summary>
        /// Doi level bang hieu ung truot ngang theo X cua chinh slot do, hai vat the chay cung luc:
        /// - mot clone SpriteRenderer mang sprite moi dat tai (x - offset, y) roi truot ve (x, y) cua slot goc;
        /// - renderer cu truot tu (x, y) ra (x + offset, y).
        /// Xong thi renderer goc nhan sprite moi, ve lai vi tri goc va clone bi huy - binding cua slot
        /// van tro dung object ban dau.
        /// Slot khong co sprite o level moi: chi co vat the cu truot ra roi an.
        /// Slot dang an: chi co clone truot vao.
        /// </summary>
        public IEnumerator SetLevel(PlayableMapSlot slot, int level, PlayableTiming timing)
        {
            if (!this.TryResolve(slot, level, out SpriteRenderer renderer, out Sprite sprite))
            {
                yield break;
            }

            if (this._currentLevels.TryGetValue(slot, out int current) && current == level)
            {
                yield break;
            }

            this._currentLevels[slot] = level;

            Transform target = renderer.transform;
            Vector3 basePosition = this._basePositions[slot];
            float distance = this._slideOffsetX;
            float duration = timing.PropSlideDuration;

            bool wasVisible = renderer.enabled && renderer.sprite != null;
            int running = 0;

            if (wasVisible)
            {
                running++;
                this.StartCoroutine(this.RunAndCount(
                    PlayableTween.MoveLocal(target, basePosition + new Vector3(distance, 0f, 0f), duration,
                        PlayableEase.SineInOut),
                    () => running--));
            }

            SpriteRenderer clone = null;
            if (sprite != null)
            {
                clone = this.CreateClone(slot, renderer, sprite, basePosition - new Vector3(distance, 0f, 0f));
                running++;
                this.StartCoroutine(this.RunAndCount(
                    PlayableTween.MoveLocal(clone.transform, basePosition, duration, PlayableEase.SineInOut),
                    () => running--));
            }

            while (running > 0)
            {
                yield return null;
            }

            // Tra renderer goc ve cho cu voi sprite moi (hoac an neu level khong co sprite).
            target.localPosition = basePosition;
            renderer.sprite = sprite;
            renderer.enabled = sprite != null;

            if (clone != null)
            {
                this.DestroyClone(slot);
            }
        }

        /// <summary>
        /// Doi dong loat moi slot sang cung level, tru cac slot trong <paramref name="except"/>.
        /// Chay song song tat ca, cho den khi slot cuoi cung doi xong.
        /// </summary>
        public IEnumerator SetLevelAll(int level, PlayableTiming timing, params PlayableMapSlot[] except)
        {
            var skip = new HashSet<PlayableMapSlot>(except ?? new PlayableMapSlot[0]);
            int running = 0;

            foreach (PlayableMapSlot slot in this.Lookup.Keys)
            {
                if (skip.Contains(slot))
                {
                    continue;
                }

                running++;
                this.StartCoroutine(this.RunAndCount(this.SetLevel(slot, level, timing), () => running--));
            }

            while (running > 0)
            {
                yield return null;
            }
        }

        /// <summary>
        /// Tao clone cung parent, cung scale/rotation va cung thiet lap render (sorting, material, mau, flip)
        /// voi renderer goc de luc truot vao trong giong het vat the that.
        /// </summary>
        private SpriteRenderer CreateClone(PlayableMapSlot slot, SpriteRenderer source, Sprite sprite,
            Vector3 localPosition)
        {
            this.DestroyClone(slot);

            Transform sourceTransform = source.transform;
            var go = new GameObject($"{source.name}_swapClone");
            go.layer = source.gameObject.layer;

            Transform cloneTransform = go.transform;
            cloneTransform.SetParent(sourceTransform.parent, false);
            cloneTransform.localRotation = sourceTransform.localRotation;
            cloneTransform.localScale = sourceTransform.localScale;
            cloneTransform.localPosition = localPosition;

            var clone = go.AddComponent<SpriteRenderer>();
            clone.sprite = sprite;
            clone.sharedMaterial = source.sharedMaterial;
            clone.color = source.color;
            clone.flipX = source.flipX;
            clone.flipY = source.flipY;
            clone.drawMode = source.drawMode;
            clone.size = source.size;
            clone.maskInteraction = source.maskInteraction;
            clone.sortingLayerID = source.sortingLayerID;
            clone.sortingOrder = source.sortingOrder;

            this._activeClones[slot] = clone;
            return clone;
        }

        private void DestroyClone(PlayableMapSlot slot)
        {
            if (!this._activeClones.TryGetValue(slot, out SpriteRenderer clone))
            {
                return;
            }

            this._activeClones.Remove(slot);
            if (clone != null)
            {
                Destroy(clone.gameObject);
            }
        }

        private IEnumerator RunAndCount(IEnumerator routine, Action onDone)
        {
            yield return routine;
            onDone();
        }

        /// <summary>
        /// Tim renderer va sprite cua slot o level. Tra ve false chi khi slot chua gan renderer;
        /// level khong co sprite (vuot mang hoac null) van tra ve true voi <paramref name="sprite"/> = null.
        /// </summary>
        private bool TryResolve(PlayableMapSlot slot, int level, out SpriteRenderer renderer, out Sprite sprite)
        {
            renderer = null;
            sprite = null;

            if (!this.Lookup.TryGetValue(slot, out PlayableMapSlotBinding binding))
            {
                return false;
            }

            renderer = binding.Renderer;

            if (binding.Levels != null && level >= 0 && level < binding.Levels.Length)
            {
                sprite = binding.Levels[level];
            }

            return true;
        }
    }
}
