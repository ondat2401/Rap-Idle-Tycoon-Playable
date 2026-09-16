using _Playable.Runtime.Core;
using TMPro;
using UnityEngine;

namespace _Playable.Runtime.View
{
    /// <summary>
    /// Dong chu goi y nhay len xuong ("TAP TO RAP!", "CHOOSE YOUR GIRL"). Bat len la tu nhay,
    /// tat di thi dung tween va tra ve vi tri goc.
    /// </summary>
    public sealed class PlayableFloatingText : MonoBehaviour
    {
        [SerializeField] private RectTransform _content;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Vector2 _bounceDelta = new Vector2(0f, 15f);
        [SerializeField] private float _bounceHalfDuration = 0.6f;

        private Coroutine _routine;
        private Vector2 _basePosition;
        private bool _cached;

        private void Awake()
        {
            this.Cache();
        }

        public void SetText(string text)
        {
            if (this._label != null)
            {
                this._label.SetText(text);
            }
        }

        public void Show()
        {
            this.Cache();
            this.gameObject.SetActive(true);
            this.StopBounce();
            this._routine = this.StartCoroutine(
                PlayableTween.LoopMove(this._content, this._basePosition, this._bounceDelta,
                    this._bounceHalfDuration));
        }

        public void Hide()
        {
            this.StopBounce();
            this.gameObject.SetActive(false);
        }

        private void StopBounce()
        {
            if (this._routine == null)
            {
                return;
            }

            this.StopCoroutine(this._routine);
            this._routine = null;
            this._content.anchoredPosition = this._basePosition;
        }

        /// <summary>
        /// Resolve <see cref="_content"/> va nho vi tri goc. Goi lai duoc nhieu lan - can the vi dong
        /// chu thuong bi tat san trong prefab nen Awake chua chac da chay truoc <see cref="Show"/>.
        /// </summary>
        private void Cache()
        {
            if (this._cached)
            {
                return;
            }

            if (this._content == null)
            {
                this._content = (RectTransform)this.transform;
            }

            this._basePosition = this._content.anchoredPosition;
            this._cached = true;
        }
    }
}
