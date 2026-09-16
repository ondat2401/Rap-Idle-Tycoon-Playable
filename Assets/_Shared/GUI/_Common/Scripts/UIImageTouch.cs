using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Amanotes.MagicTilesCore
{
    [RequireComponent(typeof(RectTransform))]
    public class UIImageTouch : UIButtonTouch
    {
        [SerializeField] private RawImage targetGraphic;

        [SerializeField] private bool scaleOnPress = true;
        [SerializeField] private Vector3 pressedScale = new Vector3(0.9f, 0.9f, 1f);

        [SerializeField] private bool tintOnPress = true;
        [SerializeField] private Color pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);

        [SerializeField] private bool swapSpriteOnPress;
        [SerializeField] private Texture2D pressedTexture;

        [SerializeField] private float animTime = 0.12f;
        [SerializeField] private Ease animEase = Ease.OutQuad;

        private Vector3 _normalScale;
        private Color _normalColor;
        private Texture _normalTexture;
        private Tween _scaleTween;
        private Tween _colorTween;

        protected override void Awake()
        {
            base.Awake();

            if (targetGraphic == null) targetGraphic = GetComponent<RawImage>();
            _normalScale = rect.localScale;
            if (targetGraphic == null) return;

            _normalColor = targetGraphic.color;
            _normalTexture = targetGraphic.texture;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            RestoreInstant();
        }

        protected override void OnPressStateChanged(bool pressed)
        {
            if (pressed) AnimatePressed();
            else AnimateNormal();
        }

        private void AnimatePressed()
        {
            if (scaleOnPress)
            {
                _scaleTween?.Kill();
                _scaleTween = rect.DOScale(Vector3.Scale(_normalScale, pressedScale), animTime).SetEase(animEase);
            }

            if (targetGraphic == null) return;

            if (tintOnPress)
            {
                _colorTween?.Kill();
                _colorTween = targetGraphic.DOColor(pressedColor, animTime).SetEase(animEase);
            }

            if (swapSpriteOnPress && pressedTexture != null)
                targetGraphic.texture = pressedTexture;
        }

        private void AnimateNormal()
        {
            if (scaleOnPress)
            {
                _scaleTween?.Kill();
                _scaleTween = rect.DOScale(_normalScale, animTime).SetEase(animEase);
            }

            if (targetGraphic == null) return;

            if (tintOnPress)
            {
                _colorTween?.Kill();
                _colorTween = targetGraphic.DOColor(_normalColor, animTime).SetEase(animEase);
            }

            if (swapSpriteOnPress) targetGraphic.texture = _normalTexture;
        }

        private void RestoreInstant()
        {
            _scaleTween?.Kill();
            _colorTween?.Kill();
            if (rect != null) rect.localScale = _normalScale;
            if (targetGraphic == null) return;

            targetGraphic.color = _normalColor;
            targetGraphic.texture = _normalTexture;
        }
    }
}
