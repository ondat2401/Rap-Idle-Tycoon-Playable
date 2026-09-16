using UnityEngine;
using DG.Tweening;

namespace Amanotes.Core
{
    /// <summary>
    /// Extended screen with DOTween animation support
    /// </summary>
    public abstract class AnimatedGUIBase : GUIBase, IAnimatedScreen
    {
        [Header("Animation Settings")]
        [SerializeField] private float showDuration = 0.3f;
        [SerializeField] private float hideDuration = 0.2f;
        [SerializeField] private Ease showEase = Ease.OutBack;
        [SerializeField] private Ease hideEase = Ease.InBack;

        private Tween currentTween;

        public override void Show()
        {
            ShowAnimated(showDuration);
        }

        public override void Hide()
        {
            HideAnimated(hideDuration);
        }

        public virtual void ShowAnimated(float duration)
        {
            if (State == ScreenState.Visible || State == ScreenState.Showing)
                return;

            currentTween?.Kill();
            State = ScreenState.Showing;
            gameObject.SetActive(true);

            // Reset state
            transform.localScale = Vector3.zero;
            canvasGroup.alpha = 0f;

            // Animate
            Sequence showSequence = DOTween.Sequence();
            showSequence.Append(transform.DOScale(Vector3.one, duration).SetEase(showEase));
            showSequence.Join(canvasGroup.DOFade(1f, duration));
            showSequence.OnComplete(() =>
            {
                State = ScreenState.Visible;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                OnScreenShow();
            });

            currentTween = showSequence;
        }

        public virtual void HideAnimated(float duration)
        {
            if (State == ScreenState.Hidden || State == ScreenState.Hiding)
                return;

            currentTween?.Kill();
            State = ScreenState.Hiding;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            Sequence hideSequence = DOTween.Sequence();
            hideSequence.Append(transform.DOScale(Vector3.zero, duration).SetEase(hideEase));
            hideSequence.Join(canvasGroup.DOFade(0f, duration));
            hideSequence.OnComplete(() =>
            {
                gameObject.SetActive(false);
                State = ScreenState.Hidden;
                OnScreenHide();
            });

            currentTween = hideSequence;
        }
    }
}
