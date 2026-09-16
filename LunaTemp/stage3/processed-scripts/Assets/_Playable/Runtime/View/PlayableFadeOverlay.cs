using System.Collections;
using _Playable.Runtime.Core;
using UnityEngine;
using UnityEngine.UI;

namespace _Playable.Runtime.View
{
    /// <summary>
    /// Man den chuyen canh - tuong duong <c>GameManager.BlackScreenTransition()</c> cua ban goc
    /// (delay 0.5s, mo trong 0.5s, dong lai trong 0.5s).
    /// </summary>
    public sealed class PlayableFadeOverlay : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private float _holdDuration = 0.5f;
        [SerializeField] private float _fadeDuration = 0.5f;

        private void Awake()
        {
            if (this._image == null)
            {
                this._image = this.GetComponent<Image>();
            }

            this.SetAlpha(0f);
            this.SetBlocking(false);
        }

        /// <summary>Che den roi mo ra. Goi giua hai canh de giau viec doi prop.</summary>
        public IEnumerator PlayTransition()
        {
            if (this._image == null)
            {
                yield break;
            }

            this.SetBlocking(true);
            yield return PlayableTween.FadeGraphic(this._image, 1f, this._fadeDuration);
            yield return PlayableTween.Delay(this._holdDuration);
            yield return PlayableTween.FadeGraphic(this._image, 0f, this._fadeDuration);
            this.SetBlocking(false);
        }

        private void SetAlpha(float alpha)
        {
            if (this._image == null)
            {
                return;
            }

            Color color = this._image.color;
            color.a = alpha;
            this._image.color = color;
        }

        private void SetBlocking(bool blocking)
        {
            if (this._image != null)
            {
                this._image.raycastTarget = blocking;
            }
        }
    }
}
