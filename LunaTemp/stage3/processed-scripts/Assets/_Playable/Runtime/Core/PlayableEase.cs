using UnityEngine;

namespace _Playable.Runtime.Core
{
    /// <summary>
    /// Cac ham easing dung cho <see cref="PlayableTween"/>. Tat ca nhan t trong [0,1] va tra ve he so noi suy.
    /// Tu viet de package khong phu thuoc DOTween.
    /// </summary>
    public static class PlayableEase
    {
        public static float Linear(float t)
        {
            return t;
        }

        /// <summary>1 - (1-t)^3. Ban playable goc dung dung ham nay cho tween so tien.</summary>
        public static float CubicOut(float t)
        {
            float inv = 1f - t;
            return 1f - (inv * inv * inv);
        }

        public static float CubicIn(float t)
        {
            return t * t * t;
        }

        public static float QuadOut(float t)
        {
            float inv = 1f - t;
            return 1f - (inv * inv);
        }

        public static float QuadIn(float t)
        {
            return t * t;
        }

        public static float SineInOut(float t)
        {
            return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
        }

        /// <summary>Vot qua dich roi lui lai. Dung cho pop-in cua nut / praise.</summary>
        public static float BackOut(float t)
        {
            const float C1 = 1.70158f;
            const float C3 = C1 + 1f;
            float inv = t - 1f;
            return 1f + (C3 * inv * inv * inv) + (C1 * inv * inv);
        }
    }
}
