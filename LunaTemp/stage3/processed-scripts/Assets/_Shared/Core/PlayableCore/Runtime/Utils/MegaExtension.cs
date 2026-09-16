using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace Amanotes.Core
{
    public static class DatLegacy
    {
        #region GameObject Extensions

        /// <summary>
        /// Scale active/inactive with animation.
        /// </summary>
        public static void SetActiveScale(this GameObject go, bool active, float scaleTime = 0.3f, Ease ease = Ease.OutBack, Action onComplete = null)
        {
            if (go == null) return;

            if (active)
            {
                if (!go.activeSelf) go.SetActive(true);
                go.transform.localScale = Vector3.zero;
                go.transform.DOScale(Vector3.one, scaleTime)
                    .SetEase(ease)
                    .OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                if (!go.activeSelf) return;
                go.transform.DOScale(Vector3.zero, scaleTime)
                    .SetEase(ease)
                    .OnComplete(() =>
                    {
                        go.SetActive(false);
                        go.transform.localScale = Vector3.one; // reset
                        onComplete?.Invoke();
                    });
            }
        }

        /// <summary>
        /// Scale to specific size.
        /// </summary>
        public static void ScaleTo(this GameObject go, float scale, float time = 0.3f, Ease ease = Ease.OutQuad, Action onComplete = null)
        {
            if (go == null) return;
            if (!go.activeSelf) go.SetActive(true);

            go.transform.DOScale(Vector3.one * scale, time)
                .SetEase(ease)
                .OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>
        /// Active/Inactive helper.
        /// </summary>
        public static void SetActiveSafe(this GameObject go, bool active)
        {
            if (go == null) return;
            if (go.activeSelf != active) go.SetActive(active);
        }

        public static void ActiveFalse(this GameObject go)
        {
            SetActiveSafe(go, false);
        }

        public static void ActiveTrue(this GameObject go, Action onComplete = null)
        {
            SetActiveSafe(go, true);
        }

        /// <summary>
        /// Get all direct children of GameObject.
        /// </summary>
        public static List<GameObject> GetChildren(this GameObject go)
        {
            var list = new List<GameObject>();
            if (go == null) return list;
            foreach (Transform child in go.transform) list.Add(child.gameObject);
            return list;
        }

        #endregion

        #region UI Extensions

        /// <summary>
        /// Fade image alpha.
        /// </summary>
        public static void Fade(this Image img, float target = 1f, float time = 0.3f, float delay = 0f, Ease ease = Ease.Linear, Action onComplete = null)
        {
            if (img == null) return;
            img.DOFade(target, time)
                .SetEase(ease)
                .SetDelay(delay)
                .OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>
        /// Fade text alpha (UI legacy).
        /// </summary>
        public static void Fade(this Text txt, float target = 1f, float time = 0.3f, float delay = 0f, Ease ease = Ease.Linear, Action onComplete = null)
        {
            if (txt == null) return;
            txt.DOFade(target, time)
                .SetEase(ease)
                .SetDelay(delay)
                .OnComplete(() => onComplete?.Invoke());
        }

        #endregion

        #region String / Number Extensions

        /// <summary>
        /// Format large number with K/M/B/T.
        /// </summary>
        public static string FormatNumber(this float num, bool upperCase = true)
        {
            string suffix = "";
            float value = num;

            if (num >= 1_000_000_000_000f) { value = num / 1_000_000_000_000f; suffix = upperCase ? "T" : "t"; }
            else if (num >= 1_000_000_000f) { value = num / 1_000_000_000f; suffix = upperCase ? "B" : "b"; }
            else if (num >= 1_000_000f) { value = num / 1_000_000f; suffix = upperCase ? "M" : "m"; }
            else if (num >= 1_000f) { value = num / 1_000f; suffix = upperCase ? "K" : "k"; }

            return value.ToString("0.#") + suffix;
        }

        #endregion

        #region Debug Extensions

        public static void Log(object message, DebugType type = DebugType.Info)
        {
#if UNITY_EDITOR
            switch (type)
            {
                case DebugType.Info: SDebug.Log(message); break;
                case DebugType.Warning: SDebug.LogWarning(message); break;
                case DebugType.Error: SDebug.LogError(message); break;
            }
#endif
        }

        #endregion
    }

    public enum DebugType
    {
        Info,
        Warning,
        Error
    }
}
