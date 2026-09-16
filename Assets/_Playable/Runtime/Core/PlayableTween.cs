using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace _Playable.Runtime.Core
{
    /// <summary>
    /// Tween coroutine toi gian, tu viet de package khong phu thuoc DOTween.
    /// Moi ham tra ve <see cref="IEnumerator"/> nen caller tu quyet dinh chay bang StartCoroutine
    /// hay yield return tuan tu. Cac ham Loop* chay vo han - caller giu Coroutine de StopCoroutine.
    /// Dung unscaled time de khong bi anh huong boi Time.timeScale.
    /// </summary>
    public static class PlayableTween
    {
        public static IEnumerator Delay(float seconds)
        {
            if (seconds <= 0f)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        /// <summary>Noi suy mot gia tri float va day ra callback moi frame. Nen tang cho cac tween khac.</summary>
        public static IEnumerator Value(float from, float to, float duration, Action<float> onUpdate,
            Func<float, float> ease = null)
        {
            if (onUpdate == null)
            {
                yield break;
            }

            if (ease == null) ease = PlayableEase.Linear;

            if (duration <= 0f)
            {
                onUpdate(to);
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                onUpdate(Mathf.LerpUnclamped(from, to, ease(t)));
                yield return null;
            }

            onUpdate(to);
        }

        public static IEnumerator Scale(Transform target, Vector3 to, float duration, Func<float, float> ease = null)
        {
            if (target == null)
            {
                yield break;
            }

            Vector3 from = target.localScale;
            yield return Value(0f, 1f, duration, t =>
            {
                if (target != null)
                {
                    target.localScale = Vector3.LerpUnclamped(from, to, t);
                }
            }, ease);
        }

        /// <summary>Pop-in: scale tu 0 len <paramref name="to"/> theo BackOut.</summary>
        public static IEnumerator PopIn(Transform target, Vector3 to, float duration)
        {
            if (target == null)
            {
                yield break;
            }

            target.localScale = Vector3.zero;
            yield return Scale(target, to, duration, PlayableEase.BackOut);
        }

        /// <summary>Di chuyen localPosition - dung cho vat the world space (nhan vat tren map).</summary>
        public static IEnumerator MoveLocal(Transform target, Vector3 to, float duration,
            Func<float, float> ease = null)
        {
            if (target == null)
            {
                yield break;
            }

            Vector3 from = target.localPosition;
            yield return Value(0f, 1f, duration, t =>
            {
                if (target != null)
                {
                    target.localPosition = Vector3.LerpUnclamped(from, to, t);
                }
            }, ease);
        }

        public static IEnumerator MoveAnchored(RectTransform target, Vector2 to, float duration,
            Func<float, float> ease = null)
        {
            if (target == null)
            {
                yield break;
            }

            Vector2 from = target.anchoredPosition;
            yield return Value(0f, 1f, duration, t =>
            {
                if (target != null)
                {
                    target.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
                }
            }, ease);
        }

        public static IEnumerator MoveAnchoredBy(RectTransform target, Vector2 delta, float duration,
            Func<float, float> ease = null)
        {
            if (target == null)
            {
                yield break;
            }

            yield return MoveAnchored(target, target.anchoredPosition + delta, duration, ease);
        }

        public static IEnumerator Fade(CanvasGroup group, float to, float duration, Func<float, float> ease = null)
        {
            if (group == null)
            {
                yield break;
            }

            float from = group.alpha;
            yield return Value(from, to, duration, v =>
            {
                if (group != null)
                {
                    group.alpha = v;
                }
            }, ease);
        }

        public static IEnumerator FadeGraphic(Graphic graphic, float to, float duration, Func<float, float> ease = null)
        {
            if (graphic == null)
            {
                yield break;
            }

            float from = graphic.color.a;
            yield return Value(from, to, duration, v =>
            {
                if (graphic == null)
                {
                    return;
                }

                Color color = graphic.color;
                color.a = v;
                graphic.color = color;
            }, ease);
        }

        /// <summary>Chay lan luot nhieu buoc. Bo qua cac buoc null.</summary>
        public static IEnumerator Sequence(params IEnumerator[] steps)
        {
            if (steps == null)
            {
                yield break;
            }

            foreach (IEnumerator step in steps)
            {
                if (step != null)
                {
                    yield return step;
                }
            }
        }

        /// <summary>
        /// Ping-pong scale quanh <paramref name="baseScale"/>. Chay vo han - caller phai StopCoroutine
        /// va tu khoi phuc scale goc. Ban goc dung delta 0.05 (rieng tap target 0.02), nua chu ky 0.5s.
        /// </summary>
        public static IEnumerator LoopScale(Transform target, Vector3 baseScale, float delta, float halfDuration)
        {
            if (target == null)
            {
                yield break;
            }

            Vector3 peak = baseScale + (Vector3.one * delta);
            while (true)
            {
                yield return Scale(target, peak, halfDuration, PlayableEase.SineInOut);
                yield return Scale(target, baseScale, halfDuration, PlayableEase.SineInOut);
            }
        }

        /// <summary>Ping-pong vi tri quanh <paramref name="basePosition"/>. Chay vo han.</summary>
        public static IEnumerator LoopMove(RectTransform target, Vector2 basePosition, Vector2 delta,
            float halfDuration)
        {
            if (target == null)
            {
                yield break;
            }

            Vector2 peak = basePosition + delta;
            while (true)
            {
                yield return MoveAnchored(target, peak, halfDuration, PlayableEase.SineInOut);
                yield return MoveAnchored(target, basePosition, halfDuration, PlayableEase.SineInOut);
            }
        }
    }
}
