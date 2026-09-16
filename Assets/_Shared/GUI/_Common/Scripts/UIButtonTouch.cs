using System;
using UnityEngine;

namespace Amanotes.MagicTilesCore
{
    [RequireComponent(typeof(RectTransform))]
    public class UIButtonTouch : MonoBehaviour, ITouchReceiver
    {
        [SerializeField] protected int pickPriority = 1000;
        [SerializeField] protected Canvas canvas;

        public event Action OnClick;

        protected RectTransform rect;
        protected Camera uiCamera;
        protected bool pressedInside;

        protected virtual void Awake()
        {
            rect = (RectTransform)transform;
            if (canvas == null) canvas = GetComponentInParent<Canvas>();
            uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
        }

        protected virtual void OnEnable() => TouchDispatcher.Instance.Register(this);

        protected virtual void OnDisable()
        {
            var dispatcher = TouchDispatcher.InstanceIfExists;
            if (dispatcher != null) dispatcher.Unregister(this);
        }

        public virtual int TouchPickPriority => pickPriority;

        public virtual bool ContainsWorldPoint(Vector3 worldPos)
        {
            if (!isActiveAndEnabled) return false;
            return RectContainsScreen(WorldToScreen(worldPos));
        }

        public virtual bool HandleTouchBegin(int fingerId, Vector2 screenPos, Vector3 worldPos)
        {
            pressedInside = RectContainsScreen(screenPos);
            if (pressedInside) OnPressStateChanged(true);
            return pressedInside;
        }

        public virtual void HandleTouchMove(int fingerId, Vector2 screenPos, Vector3 worldPos)
        {
            var inside = RectContainsScreen(screenPos);
            if (inside == pressedInside) return;
            pressedInside = inside;
            OnPressStateChanged(inside);
        }

        public virtual void HandleTouchEnd(int fingerId, Vector2 screenPos, Vector3 worldPos, bool cancelled)
        {
            OnPressStateChanged(false);
            if (!cancelled && pressedInside && RectContainsScreen(screenPos))
                OnClick?.Invoke();
            pressedInside = false;
        }

        protected virtual void OnPressStateChanged(bool pressed) { }

        protected bool RectContainsScreen(Vector2 screenPos)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, uiCamera);
        }

        protected Vector2 WorldToScreen(Vector3 worldPos)
        {
            var cam = TouchDispatcher.Instance.RaycastCamera;
            return cam != null ? (Vector2)cam.WorldToScreenPoint(worldPos) : (Vector2)worldPos;
        }
    }
}
