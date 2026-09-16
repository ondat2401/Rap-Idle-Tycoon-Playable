using System.Collections.Generic;
using _Playable.Runtime.Config;
using _Playable.Runtime.Core;
using UnityEngine;

namespace _Playable.Runtime.View
{
    /// <summary>
    /// Ban tay huong dan khi nguoi choi de yen. Sao lai dung hanh vi <c>FingerCtrl</c> ban goc:
    /// hien sau mot khoang khong tuong tac, nhay vong qua tung muc tieu dang hien,
    /// pulse scale muc tieu, va bien mat ngay khi co tuong tac.
    /// </summary>
    public sealed class PlayableGuideHand : MonoBehaviour
    {
        [Tooltip("Anh ban tay - phai la node CON, khong phai chinh GameObject giu component nay.")]
        [SerializeField] private RectTransform _hand;

        [Tooltip("CanvasGroup tren node ban tay. An bang alpha thay vi SetActive de Update van chay.")]
        [SerializeField] private CanvasGroup _handGroup;

        [SerializeField] private Vector2 _handOffset = new Vector2(30f, -40f);

        private readonly List<RectTransform> _targets = new List<RectTransform>();
        private readonly Dictionary<RectTransform, Vector3> _baseScales = new Dictionary<RectTransform, Vector3>();

        private float _idleTimeout = 3f;
        private float _switchInterval = 2f;
        private float _pulse = 0.05f;
        private float _pulseSmall = 0.02f;
        private float _loopHalfDuration = 0.5f;

        private bool _smallPulse;
        private bool _showing;
        private int _index;
        private float _lastInteractionTime;
        private float _switchTimer;
        private Coroutine _pulseRoutine;

        private void Awake()
        {
            if (this._hand == null)
            {
                Debug.LogError("[Playable] GuideHand chua gan node ban tay.", this);
                this.enabled = false;
                return;
            }

            if (this._hand == this.transform)
            {
                Debug.LogError(
                    "[Playable] GuideHand: node ban tay phai la con, khong duoc la chinh GameObject nay.", this);
                this.enabled = false;
                return;
            }

            if (this._handGroup == null)
            {
                this._handGroup = this._hand.GetComponent<CanvasGroup>();
            }

            this.SetHandVisible(false);
            this._lastInteractionTime = Time.unscaledTime;
        }

        private void SetHandVisible(bool visible)
        {
            if (this._handGroup != null)
            {
                this._handGroup.alpha = visible ? 1f : 0f;
                return;
            }

            // Khong co CanvasGroup thi tat node con - van an toan vi Update nam tren node cha.
            this._hand.gameObject.SetActive(visible);
        }

        public void Initialize(PlayableConfig config)
        {
            if (config == null)
            {
                return;
            }

            this._idleTimeout = config.GuideIdleTimeout;
            this._switchInterval = config.GuideSwitchInterval;
            this._pulse = config.GuidePulse;
            this._pulseSmall = config.GuidePulseSmall;
            this._loopHalfDuration = config.Timing.LoopHalfDuration;
        }

        /// <summary>
        /// Dat danh sach muc tieu hien tai. <paramref name="smallPulse"/> danh cho tap target -
        /// ban goc pulse no nhe hon cac nut khac.
        /// </summary>
        public void SetTargets(IEnumerable<RectTransform> targets, bool smallPulse = false)
        {
            this.Hide();

            this._targets.Clear();
            this._baseScales.Clear();
            this._smallPulse = smallPulse;

            if (targets != null)
            {
                foreach (RectTransform target in targets)
                {
                    if (target == null || this._baseScales.ContainsKey(target))
                    {
                        continue;
                    }

                    this._targets.Add(target);
                    this._baseScales[target] = target.localScale;
                }
            }

            this._index = 0;
            this._lastInteractionTime = Time.unscaledTime;
        }

        public void SetTargets(RectTransform target, bool smallPulse = false)
        {
            this.SetTargets(target == null ? null : new[] { target }, smallPulse);
        }

        public void ClearTargets()
        {
            this.SetTargets((IEnumerable<RectTransform>)null);
        }

        /// <summary>Goi moi khi nguoi choi cham man hinh.</summary>
        public void NotifyInteraction()
        {
            this._lastInteractionTime = Time.unscaledTime;
            this.Hide();
        }

        private void Update()
        {
            if (this._targets.Count == 0)
            {
                return;
            }

            if (!this._showing)
            {
                if (Time.unscaledTime - this._lastInteractionTime >= this._idleTimeout)
                {
                    this.Show();
                }

                return;
            }

            if (this._targets.Count <= 1)
            {
                return;
            }

            this._switchTimer -= Time.unscaledDeltaTime;
            if (this._switchTimer <= 0f)
            {
                this._index = (this._index + 1) % this._targets.Count;
                this.FocusCurrent();
            }
        }

        private void Show()
        {
            if (this._targets.Count == 0)
            {
                return;
            }

            this._showing = true;
            this._index = 0;
            this.SetHandVisible(true);
            this.FocusCurrent();
        }

        private void Hide()
        {
            if (!this._showing)
            {
                return;
            }

            this._showing = false;
            this.StopPulse();
            this.RestoreScales();
            this.SetHandVisible(false);
        }

        private void FocusCurrent()
        {
            this._switchTimer = this._switchInterval;
            this.StopPulse();
            this.RestoreScales();

            RectTransform target = this._targets[this._index];
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                return;
            }

            this._hand.position = target.position;
            this._hand.anchoredPosition += this._handOffset;

            Vector3 baseScale = this._baseScales.TryGetValue(target, out Vector3 cached)
                ? cached
                : target.localScale;

            float delta = this._smallPulse ? this._pulseSmall : this._pulse;
            this._pulseRoutine = this.StartCoroutine(
                PlayableTween.LoopScale(target, baseScale, delta, this._loopHalfDuration));
        }

        private void StopPulse()
        {
            if (this._pulseRoutine == null)
            {
                return;
            }

            this.StopCoroutine(this._pulseRoutine);
            this._pulseRoutine = null;
        }

        private void RestoreScales()
        {
            foreach (KeyValuePair<RectTransform, Vector3> pair in this._baseScales)
            {
                if (pair.Key != null)
                {
                    pair.Key.localScale = pair.Value;
                }
            }
        }
    }
}
