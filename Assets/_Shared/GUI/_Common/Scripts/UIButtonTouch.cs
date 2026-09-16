using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace Amanotes.MagicTilesCore
{
    [RequireComponent(typeof(RectTransform))]
    public class UIButtonTouch : MonoBehaviour, ITouchReceiver
    {
        [SerializeField] protected int pickPriority = 1000;
        [SerializeField] protected Canvas canvas;

        [Header("Press Scale")]
        [SerializeField] protected bool scaleOnPress = true;
        [SerializeField] protected float scaleTime = 0.2f;
        [SerializeField] protected Vector2 scaleSize = new Vector2(0.9f, 0.9f);
        [SerializeField] protected Ease scaleEase = Ease.InOutQuad;

        [Header("Click")]
        [SerializeField] protected UnityEvent onClick = new UnityEvent();

        public event Action OnClick;

        protected RectTransform rect;
        protected Camera uiCamera;
        protected bool pressedInside;
        protected Vector3 baseScale;
        protected Tween scaleTween;
        protected bool baseScaleCached;
        protected bool canClick = true;
        protected bool interactable = true;

        /// <summary>Khi false, nut khong nhan touch (dung thay cho Button.interactable).</summary>
        public bool Interactable
        {
            get => interactable;
            set => interactable = value;
        }

        protected virtual void Awake()
        {
            rect = (RectTransform)transform;
            if (canvas == null) canvas = GetComponentInParent<Canvas>();

            // Overlay: khong can camera (hit-test bang overload 2 tham so).
            // Screen Space - Camera / World Space: can camera de chuyen rect ve screen space. Neu canvas
            // chua gan worldCamera thi fallback ve Camera.main de van hit-test dung.
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
            }
            else
            {
                uiCamera = null;
            }
        }

        protected virtual void OnEnable() => TouchDispatcher.Instance.Register(this);

        protected virtual void OnDisable()
        {
            var dispatcher = TouchDispatcher.InstanceIfExists;
            if (dispatcher != null) dispatcher.Unregister(this);
            scaleTween?.Kill();
            pressedInside = false;
            canClick = true;
            if (scaleOnPress && baseScaleCached && rect != null) rect.localScale = baseScale;
        }

        public virtual int TouchPickPriority => pickPriority;

        public virtual bool ContainsWorldPoint(Vector3 worldPos)
        {
            if (!isActiveAndEnabled) return false;
            return RectContainsScreen(WorldToScreen(worldPos));
        }

        public virtual bool HandleTouchBegin(int fingerId, Vector2 screenPos, Vector3 worldPos)
        {
            if (!interactable) return false;
            if (scaleOnPress && !canClick) return false;
            pressedInside = RectContainsScreen(screenPos);
            if (pressedInside) SetPressed(true);
            return pressedInside;
        }

        public virtual void HandleTouchMove(int fingerId, Vector2 screenPos, Vector3 worldPos)
        {
            var inside = RectContainsScreen(screenPos);
            if (inside == pressedInside) return;
            pressedInside = inside;
            SetPressed(inside);
        }

        public virtual void HandleTouchEnd(int fingerId, Vector2 screenPos, Vector3 worldPos, bool cancelled)
        {
            SetPressed(false);
            if (!cancelled && pressedInside && RectContainsScreen(screenPos))
            {
                OnClick?.Invoke();
                onClick.Invoke();
            }
            pressedInside = false;
        }

        protected void SetPressed(bool pressed)
        {
            ApplyPressScale(pressed);
            OnPressStateChanged(pressed);
        }

        protected virtual void ApplyPressScale(bool pressed)
        {
            if (!scaleOnPress) return;
            scaleTween?.Kill();
            if (pressed)
            {
                // Cache the real base scale lazily on the first press. Caching in Awake
                // is unreliable because the RectTransform can still be scale 0 then.
                // canClick blocks re-press until the release tween restores base, so
                // localScale is always the true base scale at this point.
                if (!baseScaleCached)
                {
                    baseScale = rect.localScale;
                    baseScaleCached = true;
                }

                canClick = false;
                scaleTween = rect.DOScale(Vector3.Scale(baseScale, scaleSize), scaleTime).SetEase(scaleEase);
                return;
            }

            scaleTween = rect.DOScale(baseScale, scaleTime).SetEase(scaleEase).OnComplete(() => canClick = true);
        }

        protected virtual void OnPressStateChanged(bool pressed) { }

        protected bool RectContainsScreen(Vector2 screenPos)
        {
            // Luna Playworks khong ho tro overload RectangleContainsScreenPoint(rect, point, camera).
            // Voi canvas Screen Space - Overlay (uiCamera == null) dung overload 2 tham so duoc Luna
            // ho tro. Chi dung ban co camera khi that su can (Screen Space - Camera / World Space).
            if (uiCamera == null)
            {
                return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos);
            }

            return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, uiCamera);
        }

        protected Vector2 WorldToScreen(Vector3 worldPos)
        {
            var cam = TouchDispatcher.Instance.RaycastCamera;
            return cam != null ? (Vector2)cam.WorldToScreenPoint(worldPos) : (Vector2)worldPos;
        }
    }
}
