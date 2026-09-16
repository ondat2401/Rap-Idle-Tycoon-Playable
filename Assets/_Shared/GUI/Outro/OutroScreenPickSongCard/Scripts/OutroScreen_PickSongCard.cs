using System;
using System.Collections.Generic;
using Amanotes.Core;
using DG.Tweening;
using OrientationTracking.Core;
using UnityEngine;

namespace Amanotes.MagicTiles3
{
    [Serializable]
    internal class OutroShowAnim
    {
        public float duration = 0.35f;
        public float stagger = 0.08f;
        public Ease ease = Ease.OutBack;
    }

    [Serializable]
    internal class OutroLoopAnim
    {
        public float punch = 0.08f;
        public float duration = 0.6f;
        public float nextDelay = 0.15f;
        public float interval = 2f;
        public Ease ease = Ease.InOutSine;
    }

    [Serializable]
    public abstract class SongCardEntry
    {
        public abstract Texture2D Texture { get; }
    }

    [Serializable]
    public class SongCardEntry1 : SongCardEntry
    {
        [LunaPlaygroundAsset("Song Card 1", 0, "OutroScreen")]
        public Texture2D texture;
        public override Texture2D Texture => texture;
    }

    [Serializable]
    public class SongCardEntry2 : SongCardEntry
    {
        [LunaPlaygroundAsset("Song Card 2", 1, "OutroScreen")]
        public Texture2D texture;
        public override Texture2D Texture => texture;
    }

    [Serializable]
    public class SongCardEntry3 : SongCardEntry
    {
        [LunaPlaygroundAsset("Song Card 3", 2, "OutroScreen")]
        public Texture2D texture;
        public override Texture2D Texture => texture;
    }

    [Serializable]
    public class SongCardEntry4 : SongCardEntry
    {
        [LunaPlaygroundAsset("Song Card 4", 3, "OutroScreen")]
        public Texture2D texture;
        public override Texture2D Texture => texture;
    }

    [Serializable]
    public class SongCardEntry5 : SongCardEntry
    {
        [LunaPlaygroundAsset("Song Card 5", 4, "OutroScreen")]
        public Texture2D texture;
        public override Texture2D Texture => texture;
    }

    [Serializable]
    public class SongCardEntry6 : SongCardEntry
    {
        [LunaPlaygroundAsset("Song Card 6", 5, "OutroScreen")]
        public Texture2D texture;
        public override Texture2D Texture => texture;
    }

    public class OutroScreen_PickSongCard : OutroScreen
    {
        [Header("Song Cards")]
        [SerializeField] private SongCardEntry1 songCard1 = new SongCardEntry1();
        [SerializeField] private SongCardEntry2 songCard2 = new SongCardEntry2();
        [SerializeField] private SongCardEntry3 songCard3 = new SongCardEntry3();
        [SerializeField] private SongCardEntry4 songCard4 = new SongCardEntry4();
        [SerializeField] private SongCardEntry5 songCard5 = new SongCardEntry5();
        [SerializeField] private SongCardEntry6 songCard6 = new SongCardEntry6();

        [NonSerialized] private SongCardEntry[] _songCards;
        private SongCardEntry[] SongCards
        {
            get
            {
                if (_songCards == null)
                    _songCards = new SongCardEntry[] { songCard1, songCard2, songCard3, songCard4, songCard5, songCard6 };
                return _songCards;
            }
        }

        [Header("References")]
        [SerializeField] private OutroCard cardPrefab;
        [SerializeField] private RectTransform cardParent;

        [Header("Phone Portrait")]
        [LunaPlaygroundField("Phone_PT_Spacing", 1, "OutroScreen")]
        public Vector2 phone_PT_Spacing = new Vector2(20, 20);
        [LunaPlaygroundField("Phone_PT_Rows", 2, "OutroScreen")]
        public int phone_PT_Rows = 2;
        [LunaPlaygroundField("Phone_PT_CardScale", 3, "OutroScreen")]
        public float phone_PT_CardScale = 1f;

        [Header("Phone Landscape")]
        [LunaPlaygroundField("Phone_LS_Spacing", 4, "OutroScreen")]
        public Vector2 phone_LS_Spacing = new Vector2(20, 20);
        [LunaPlaygroundField("Phone_LS_Rows", 5, "OutroScreen")]
        public int phone_LS_Rows = 1;
        [LunaPlaygroundField("Phone_LS_CardScale", 6, "OutroScreen")]
        public float phone_LS_CardScale = 1f;

        [Header("Tablet Portrait")]
        [LunaPlaygroundField("Tablet_PT_Spacing", 11, "OutroScreen")]
        public Vector2 tablet_PT_Spacing = new Vector2(30, 30);
        [LunaPlaygroundField("Tablet_PT_Rows", 12, "OutroScreen")]
        public int tablet_PT_Rows = 2;
        [LunaPlaygroundField("Tablet_PT_CardScale", 13, "OutroScreen")]
        public float tablet_PT_CardScale = 1f;

        [Header("Tablet Landscape")]
        [LunaPlaygroundField("Tablet_LS_Spacing", 14, "OutroScreen")]
        public Vector2 tablet_LS_Spacing = new Vector2(30, 30);
        [LunaPlaygroundField("Tablet_LS_Rows", 15, "OutroScreen")]
        public int tablet_LS_Rows = 1;
        [LunaPlaygroundField("Tablet_LS_CardScale", 16, "OutroScreen")]
        public float tablet_LS_CardScale = 1f;

        [Header("Show Animation")]
        [SerializeField] private OutroShowAnim showAnim = new OutroShowAnim();

        [Header("Loop Animation")]
        [SerializeField] private OutroLoopAnim loopAnim = new OutroLoopAnim();

        private readonly List<OutroCard> _cards = new List<OutroCard>(6);
        private Sequence _popSequence;
        private Sequence _loopSequence;
        private float _currentScale = 1f;
        private bool _shown;

        private void OnEnable()
        {
            EventBus.Subscribe<OnOrientationChangedEvent>(OnOrientationChanged);
            BuildCards();
            LayoutCards();
            HideCards();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnOrientationChangedEvent>(OnOrientationChanged);
            KillPop();
            _shown = false;
        }

        public override void OnScreenShow()
        {
            base.OnScreenShow();
            _shown = true;
            LayoutCards();
            PlayPop();
        }

        public override void OnScreenHide()
        {
            base.OnScreenHide();
            _shown = false;
            KillPop();
        }

        private void HideCards()
        {
            for (int i = 0; i < _cards.Count; i++)
                _cards[i].Rect.localScale = Vector3.zero;
        }

        private void BuildCards()
        {
            if (cardPrefab == null || cardParent == null) return;
            if (_cards.Count > 0) return;

            var entries = SongCards;
            for (int i = 0; i < entries.Length; i++)
            {
                var tex = entries[i].Texture;
                if (tex == null) continue;

                var card = Instantiate(cardPrefab, cardParent);
                card.SetTexture(tex);
                card.gameObject.SetActive(true);
                _cards.Add(card);
            }
        }

        private void LayoutCards()
        {
            int count = _cards.Count;
            if (count == 0) return;

            ResolveSlot(out Vector2 spacing, out int rows, out float cardScale);
            _currentScale = cardScale;

            rows = Mathf.Clamp(rows, 1, count);
            int cols = Mathf.CeilToInt(count / (float)rows);

            Vector2 baseSize = Vector2.zero;
            for (int i = 0; i < count; i++)
                baseSize = Vector2.Max(baseSize, _cards[i].Rect.sizeDelta);

            Vector2 cell = baseSize * cardScale;

            float totalHeight = rows * cell.y + (rows - 1) * spacing.y;
            float startY = totalHeight * 0.5f - cell.y * 0.5f;

            for (int i = 0; i < count; i++)
            {
                int row = i / cols;
                int col = i % cols;

                int itemsInRow = Mathf.Min(cols, count - row * cols);
                float rowWidth = itemsInRow * cell.x + (itemsInRow - 1) * spacing.x;
                float startX = -rowWidth * 0.5f + cell.x * 0.5f;

                float x = startX + col * (cell.x + spacing.x);
                float y = startY - row * (cell.y + spacing.y);

                _cards[i].Rect.anchoredPosition = new Vector2(x, y);
            }
        }

        private void ResolveSlot(out Vector2 spacing, out int rows, out float cardScale)
        {
            bool isPhone = !OrientationTracker.IsInitialized || OrientationTracker.Instance.IsPhone;
            bool isPortrait = !OrientationTracker.IsInitialized || OrientationTracker.Instance.IsPortrait;

            if (isPhone)
            {
                spacing = isPortrait ? phone_PT_Spacing : phone_LS_Spacing;
                rows = isPortrait ? phone_PT_Rows : phone_LS_Rows;
                cardScale = isPortrait ? phone_PT_CardScale : phone_LS_CardScale;
                return;
            }

            spacing = isPortrait ? tablet_PT_Spacing : tablet_LS_Spacing;
            rows = isPortrait ? tablet_PT_Rows : tablet_LS_Rows;
            cardScale = isPortrait ? tablet_PT_CardScale : tablet_LS_CardScale;
        }

        private void PlayPop()
        {
            KillPop();

            int count = _cards.Count;
            if (count == 0) return;

            _popSequence = DOTween.Sequence();
            for (int i = 0; i < count; i++)
            {
                var rect = _cards[i].Rect;
                rect.localScale = Vector3.zero;
                _popSequence.Insert(i * showAnim.stagger, rect.DOScale(Vector3.one * _currentScale, showAnim.duration).SetEase(showAnim.ease));
            }

            float showEnd = (count - 1) * showAnim.stagger + showAnim.duration;
            StartLoopPop(showEnd);
        }

        private void StartLoopPop(float startDelay)
        {
            int count = _cards.Count;
            if (count == 0) return;

            Vector3 rest = Vector3.one * _currentScale;
            Vector3 peak = Vector3.one * (_currentScale * (1f + loopAnim.punch));
            float half = loopAnim.duration * 0.5f;

            _loopSequence = DOTween.Sequence();
            for (int i = 0; i < count; i++)
            {
                var rect = _cards[i].Rect;
                _loopSequence.Append(rect.DOScale(peak, half).SetEase(loopAnim.ease));
                _loopSequence.Append(rect.DOScale(rest, half).SetEase(loopAnim.ease));
                _loopSequence.AppendInterval(loopAnim.nextDelay);
            }

            _loopSequence.AppendInterval(loopAnim.interval);
            _loopSequence.SetLoops(-1, LoopType.Restart);
            _loopSequence.SetDelay(startDelay);
        }

        private void KillPop()
        {
            if (_popSequence != null)
            {
                _popSequence.Kill();
                _popSequence = null;
            }

            if (_loopSequence != null)
            {
                _loopSequence.Kill();
                _loopSequence = null;
            }
        }

        private void OnOrientationChanged(OnOrientationChangedEvent evt)
        {
            LayoutCards();
            if (_shown) PlayPop();
        }
    }
}
