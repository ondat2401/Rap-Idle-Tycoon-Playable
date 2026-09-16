using System;
using System.Collections;
using System.Globalization;
using _Playable.Runtime.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Playable.Runtime.View
{
    /// <summary>
    /// Thanh tien + progress bar. Tween so tien dung dung cong thuc ban goc:
    /// noi suy cubic-out trong 0.5 giay, lam tron xuong, chan tran o muc tieu,
    /// progress = gia tri / muc tieu, va ban <see cref="MoneyFull"/> dung mot lan.
    /// </summary>
    public sealed class PlayableHudView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private TMP_Text _value;
        [SerializeField] private Image _fill;

        [Header("Mau bao thieu tien")]
        [SerializeField] private Color _normalColor = Color.black;
        [SerializeField] private Color _errorColor = Color.red;
        [SerializeField] private float _rejectHalfDuration = 0.1f;
        [SerializeField] private float _rejectScaleDelta = 0.1f;

        private long _goal = 1L;
        private bool _fullFired;

        /// <summary>Ban dung mot lan khi so hien thi cham muc tieu.</summary>
        public event Action MoneyFull;

        public void Initialize(long goal, long startValue)
        {
            this._goal = Math.Max(1L, goal);
            this._fullFired = false;

            if (this._value != null)
            {
                this._value.color = this._normalColor;
            }

            this.SetInstant(startValue);
        }

        public void SetInstant(long value)
        {
            this.Render(value);
        }

        public void Show()
        {
            this.SetGroup(1f, true);
        }

        public void Hide()
        {
            this.SetGroup(0f, false);
        }

        /// <summary>Tween so tien tu <paramref name="from"/> sang <paramref name="to"/>.</summary>
        public IEnumerator AnimateTo(long from, long to, float duration)
        {
            yield return PlayableTween.Value(from, to, duration, v => this.Render((long)Mathf.Floor(v)),
                PlayableEase.CubicOut);
        }

        /// <summary>Nhap nhay do khi khong du tien - giong hieu ung handleNotEnoughMoney ban goc.</summary>
        public IEnumerator PlayNotEnough()
        {
            if (this._value == null)
            {
                yield break;
            }

            Transform target = this._value.transform;
            Vector3 baseScale = target.localScale;

            this._value.color = this._errorColor;
            yield return PlayableTween.Scale(target, baseScale + (Vector3.one * this._rejectScaleDelta),
                this._rejectHalfDuration);
            yield return PlayableTween.Scale(target, baseScale, this._rejectHalfDuration);
            this._value.color = this._normalColor;
        }

        private void Render(long value)
        {
            long clamped = value < 0L ? 0L : (value > this._goal ? this._goal : value);

            if (this._value != null)
            {
                this._value.text = clamped.ToString("N0", CultureInfo.InvariantCulture);
            }

            if (this._fill != null)
            {
                this._fill.fillAmount = Mathf.Clamp01((float)clamped / this._goal);
            }

            if (!this._fullFired && clamped >= this._goal)
            {
                this._fullFired = true;
                this.MoneyFull?.Invoke();
            }
        }

        private void SetGroup(float alpha, bool interactable)
        {
            if (this._group == null)
            {
                return;
            }

            this._group.alpha = alpha;
            this._group.blocksRaycasts = interactable;
        }
    }
}
