using System.Collections;
using _Playable.Runtime.Core;
using Amanotes.MagicTilesCore;
using UnityEngine;
using UnityEngine.UI;

namespace _Playable.Runtime.View
{
    /// <summary>
    /// Muc tieu de nguoi choi tap kiem tien. Ban goc la cai dien thoai voi clip
    /// Phone_appear / Phone_play / Phone_disappear; du an khong co sprite dien thoai nen o day dung
    /// icon dia nhac + tween (xem G3 trong Docs/Playable-Clone-Plan.md).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class PlayableTapTargetView : MonoBehaviour
    {
        [SerializeField] private UIButtonTouch _button;
        [SerializeField] private RectTransform _icon;
        [SerializeField] private RectTransform _ring;
        [SerializeField] private CanvasGroup _ringGroup;

        [SerializeField] private float _appearDuration = 0.3f;
        [SerializeField] private float _disappearDuration = 0.15f;
        [SerializeField] private float _spinSpeed = 90f;
        [SerializeField] private float _ringDuration = 0.35f;
        [SerializeField] private float _ringScale = 1.6f;

        private RectTransform _rect;
        private Vector3 _baseScale = Vector3.one;
        private bool _tapped;
        private bool _spinning;
        private bool _cached;

        public RectTransform Rect => this._rect != null ? this._rect : this._rect = (RectTransform)this.transform;

        private void Awake()
        {
            if (this._button == null)
            {
                this._button = this.GetComponent<UIButtonTouch>();
            }

            this.EnsureCached();

            if (this._button != null)
            {
                this._button.OnClick += this.HandleClick;
            }

            if (this._ringGroup != null)
            {
                this._ringGroup.alpha = 0f;
            }

            // Khong tat o day - viec tat ban dau do PlayableStageView.ResetToInitial goi HideImmediate lo.
            // Tat trong Awake se lam Awake chay lai giua Appear() va tat nguoc object dang tween.
        }

        /// <summary>Tat ngay khong tween, giu nguyen scale goc. Dung khi reset canh.</summary>
        public void HideImmediate()
        {
            this.EnsureCached();
            this._spinning = false;
            this.SetInteractable(false);
            this.Rect.localScale = this._baseScale;
            this.gameObject.SetActive(false);
        }

        /// <summary>Nho scale goc. Goi lai duoc nhieu lan - Awake chua chac chay truoc HideImmediate.</summary>
        private void EnsureCached()
        {
            if (this._cached)
            {
                return;
            }

            this._cached = true;
            this._baseScale = this.Rect.localScale;
        }

        private void OnDestroy()
        {
            if (this._button != null)
            {
                this._button.OnClick -= this.HandleClick;
            }
        }

        private void Update()
        {
            if (this._spinning && this._icon != null)
            {
                this._icon.Rotate(Vector3.forward, -this._spinSpeed * Time.unscaledDeltaTime);
            }
        }

        public IEnumerator Appear()
        {
            this.EnsureCached();
            this.gameObject.SetActive(true);
            this.SetInteractable(true);
            yield return PlayableTween.PopIn(this.Rect, this._baseScale, this._appearDuration);
        }

        public IEnumerator Disappear()
        {
            this.SetInteractable(false);
            this._spinning = false;
            yield return PlayableTween.Scale(this.Rect, Vector3.zero, this._disappearDuration,
                PlayableEase.QuadIn);
            this.gameObject.SetActive(false);
            this.Rect.localScale = this._baseScale;
        }

        /// <summary>Bat trang thai dang phat nhac - icon quay tron.</summary>
        public void StartSpin()
        {
            this._spinning = true;
        }

        public void SetInteractable(bool interactable)
        {
            if (this._button != null)
            {
                this._button.Interactable = interactable;
            }
        }

        /// <summary>Cho den khi nguoi choi tap mot lan.</summary>
        public IEnumerator WaitForTap()
        {
            this._tapped = false;
            while (!this._tapped)
            {
                yield return null;
            }

            this._tapped = false;
        }

        /// <summary>Vong song lan ra moi lan tap.</summary>
        public void PlayRipple()
        {
            if (this._ring == null || this._ringGroup == null)
            {
                return;
            }

            this.StartCoroutine(this.RippleRoutine());
        }

        private IEnumerator RippleRoutine()
        {
            this._ring.gameObject.SetActive(true);
            this._ring.localScale = Vector3.one;
            this._ringGroup.alpha = 1f;

            this.StartCoroutine(PlayableTween.Fade(this._ringGroup, 0f, this._ringDuration));
            yield return PlayableTween.Scale(this._ring, Vector3.one * this._ringScale, this._ringDuration,
                PlayableEase.QuadOut);

            this._ring.gameObject.SetActive(false);
        }

        private void HandleClick()
        {
            this._tapped = true;
        }
    }
}
