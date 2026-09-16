using System.Collections;
using _Playable.Runtime.Core;
using UnityEngine;
using UnityEngine.UI;

namespace _Playable.Runtime.View
{
    /// <summary>
    /// Bong thoai / bong goi y. Ban goc co 5 bong voi kieu loop khac nhau (loop scale cho bong thoai,
    /// loop pos cho bong goi y) nen kieu loop de o Inspector cho tung instance tu chon.
    /// </summary>
    public sealed class PlayableBubbleView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private RectTransform _content;
        [SerializeField] private Text _label;

        [Header("Fade")]
        [SerializeField] private float _fadeDuration = 0.25f;

        [Header("Loop - chon mot kieu, hoac khong kieu nao")]
        [SerializeField] private bool _loopScale;
        [SerializeField] private float _loopScaleDelta = 0.06f;
        [SerializeField] private bool _loopMove;
        [SerializeField] private Vector2 _loopMoveDelta = new Vector2(0f, 14f);

        private Coroutine _loopRoutine;
        private Vector3 _baseScale = Vector3.one;
        private Vector2 _basePosition;
        private bool _cached;

        // Khong tat GameObject trong Awake: bong bong thuong bi tat san trong prefab, luc do Awake chua
        // chay va se chay ngay giua Show() - tat lai lam coroutine tween chay tren object da tat.
        // Viec tat ban dau do PlayableFlow.Prepare goi HideImmediate lo.
        private void Awake()
        {
            this.EnsureInit();
        }

        private void EnsureInit()
        {
            if (this._content == null)
            {
                this._content = (RectTransform)this.transform;
            }

            this.Cache();
        }

        public void SetText(string text)
        {
            if (this._label != null)
            {
                this._label.text = text;
            }
        }

        public IEnumerator Show(float delay = 0f, float loopHalfDuration = 0.5f)
        {
            yield return PlayableTween.Delay(delay);

            this.EnsureInit();
            this.gameObject.SetActive(true);
            this._content.localScale = this._baseScale;
            this._content.anchoredPosition = this._basePosition;

            yield return PlayableTween.Fade(this._group, 1f, this._fadeDuration);
            this.StartLoop(loopHalfDuration);
        }

        public IEnumerator Hide()
        {
            this.StopLoop();
            yield return PlayableTween.Fade(this._group, 0f, this._fadeDuration);
            this.gameObject.SetActive(false);
        }

        /// <summary>Tat ngay khong tween - dung khi reset.</summary>
        public void HideImmediate()
        {
            this.EnsureInit();
            this.StopLoop();
            this.SetAlpha(0f);
            this.gameObject.SetActive(false);
        }

        private void StartLoop(float halfDuration)
        {
            this.StopLoop();

            if (this._loopScale)
            {
                this._loopRoutine = this.StartCoroutine(
                    PlayableTween.LoopScale(this._content, this._baseScale, this._loopScaleDelta, halfDuration));
            }
            else if (this._loopMove)
            {
                this._loopRoutine = this.StartCoroutine(
                    PlayableTween.LoopMove(this._content, this._basePosition, this._loopMoveDelta, halfDuration));
            }
        }

        private void StopLoop()
        {
            if (this._loopRoutine == null)
            {
                return;
            }

            this.StopCoroutine(this._loopRoutine);
            this._loopRoutine = null;
            this._content.localScale = this._baseScale;
            this._content.anchoredPosition = this._basePosition;
        }

        private void Cache()
        {
            if (this._cached)
            {
                return;
            }

            this._baseScale = this._content.localScale;
            this._basePosition = this._content.anchoredPosition;
            this._cached = true;
        }

        private void SetAlpha(float alpha)
        {
            if (this._group != null)
            {
                this._group.alpha = alpha;
            }
        }
    }
}
