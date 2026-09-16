using UnityEngine;

namespace Amanotes.Core
{
    [CreateAssetMenu(fileName = "UIConfig", menuName = "UI Framework/UI Config")]
    public class UIConfig : ScriptableObject
    {
        [Header("Animation Settings")]
        public float defaultShowDuration = 0.3f;
        public float defaultHideDuration = 0.2f;
        public AnimationCurve showCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public AnimationCurve hideCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Header("Canvas Settings")]
        public int targetCanvasWidth = 1920;
        public int targetCanvasHeight = 1080;
        public float referencePixelsPerUnit = 100f;

        [Header("Performance")]
        public bool enableObjectPooling = true;
        public int initialPoolSize = 5;
    }
}
