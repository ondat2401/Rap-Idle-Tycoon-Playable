using System.Collections;
using _Playable.Runtime.Core;
using UnityEngine;

namespace _Playable.Runtime.View
{
    /// <summary>
    /// Chu khen bat ra sau moi lan mua ("NICE!", "AWESOME!"). Ban goc dung animation clip;
    /// o day thay bang tween pop-in + giu + fade cho khoi phai dat them asset.
    /// </summary>
    public sealed class PlayablePraiseView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _niceGroup;
        [SerializeField] private CanvasGroup _awesomeGroup;
        [SerializeField] private float _popDuration = 0.25f;
        [SerializeField] private float _holdDuration = 0.4f;
        [SerializeField] private float _fadeDuration = 0.2f;

        private void Awake()
        {
            this.HideImmediate(this._niceGroup);
            this.HideImmediate(this._awesomeGroup);
        }

        public IEnumerator PlayNice()
        {
            yield return this.Play(this._niceGroup);
        }

        public IEnumerator PlayAwesome()
        {
            yield return this.Play(this._awesomeGroup);
        }

        private IEnumerator Play(CanvasGroup group)
        {
            if (group == null)
            {
                yield break;
            }

            group.gameObject.SetActive(true);
            group.alpha = 1f;

            yield return PlayableTween.PopIn(group.transform, Vector3.one, this._popDuration);
            yield return PlayableTween.Delay(this._holdDuration);
            yield return PlayableTween.Fade(group, 0f, this._fadeDuration);

            group.gameObject.SetActive(false);
        }

        private void HideImmediate(CanvasGroup group)
        {
            if (group == null)
            {
                return;
            }

            group.alpha = 0f;
            group.gameObject.SetActive(false);
        }
    }
}
