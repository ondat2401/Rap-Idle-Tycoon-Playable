using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace Amanotes.Core
{
    [RequireComponent(typeof(RectTransform))]
    public class ButtonScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private Tween tween;
        private RectTransform _rectTransform;
        [SerializeField] float scaleTime = 0.2f;
        [SerializeField] Vector2 scaleSize = new Vector2(0.9f, 0.9f);
        [SerializeField] Ease scaleEase = Ease.InOutQuad;

        void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            tween?.Kill();
            tween = _rectTransform.DOScale(scaleSize, scaleTime).SetEase(scaleEase);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            tween?.Kill();
            tween = _rectTransform.DOScale(Vector2.one, scaleTime).SetEase(scaleEase);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tween?.Kill();
            tween = _rectTransform.DOScale(Vector2.one, scaleTime).SetEase(scaleEase);
        }
    }
}
