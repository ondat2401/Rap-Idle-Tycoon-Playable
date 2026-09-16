using UnityEditor;
using UnityEngine;
using Amanotes.Core;
using Amanotes.LunaFieldGenerator;

namespace Amanotes.LunaFieldGenerator.Editor
{
    /// <summary>
    /// A floating EditorWindow that displays generation progress with a styled progress bar.
    /// Usage: LunaProgressWindow.Show("Title") → LunaProgressWindow.Update("msg", 0.5f) → LunaProgressWindow.Close()
    /// </summary>
    public class LunaProgressWindow : EditorWindow
    {
        #region State

        private string title = "Generating...";
        private string message = "";
        private float progress = 0f;
        private int currentStep = 0;
        private int totalSteps = 0;

        private static LunaProgressWindow instance;

        // Colors
        private static readonly Color BgColor       = new Color(0.18f, 0.18f, 0.22f);
        private static readonly Color BarBgColor    = new Color(0.12f, 0.12f, 0.15f);
        private static readonly Color BarFillColor  = new Color(0.3f, 0.75f, 0.4f);
        private static readonly Color TitleColor    = new Color(0.9f, 0.9f, 1f);
        private static readonly Color MsgColor      = new Color(0.7f, 0.85f, 0.7f);
        private static readonly Color StepColor     = new Color(0.5f, 0.7f, 1f);

        #endregion

        #region Public API

        /// <summary>Open the progress window with a given title and optional total step count.</summary>
        public static void Show(string windowTitle, int steps = 0)
        {
            if (instance != null)
                instance.closeInternal();

            instance = CreateInstance<LunaProgressWindow>();
            instance.titleContent = new GUIContent("Luna Field Generator");
            instance.title = windowTitle;
            instance.totalSteps = steps;
            instance.minSize = new Vector2(420, 130);
            instance.maxSize = new Vector2(420, 130);
            instance.ShowUtility();
            instance.CenterOnScreen();
            // Force an immediate paint so the window appears before the blocking loop
            instance.Repaint();
            EditorApplication.QueuePlayerLoopUpdate();
        }

        /// <summary>Update progress. progress is 0..1. stepLabel is optional current item name.</summary>
        public static void Update(string msg, float progressValue, int step = 0)
        {
            if (instance == null) return;
            instance.message = msg;
            instance.progress = Mathf.Clamp01(progressValue);
            instance.currentStep = step;
            instance.Repaint();
            // Also drive the native progress bar so Unity repaints the editor chrome
            EditorUtility.DisplayProgressBar(instance.title, msg, progressValue);
        }

        /// <summary>Close the window.</summary>
        public static void Dismiss()
        {
            EditorUtility.ClearProgressBar();
            if (instance == null) return;
            instance.closeInternal();
            instance = null;
        }

        /// <summary>Returns true if the window is currently open.</summary>
        public static bool IsOpen => instance != null;

        #endregion

        #region GUI

        private void OnGUI()
        {
            // Background
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), BgColor);

            float padding = 18f;
            float w = position.width - padding * 2;
            float y = 16f;

            // Title
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = TitleColor }
            };
            GUI.Label(new Rect(padding, y, w, 22), title, titleStyle);
            y += 26f;

            // Step counter
            if (totalSteps > 0)
            {
                var stepStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleRight,
                    normal = { textColor = StepColor }
                };
                GUI.Label(new Rect(padding, y - 26f, w, 22),
                    $"{currentStep} / {totalSteps}", stepStyle);
            }

            // Progress bar background
            float barHeight = 14f;
            Rect barBg = new Rect(padding, y, w, barHeight);
            EditorGUI.DrawRect(barBg, BarBgColor);

            // Progress bar fill
            float fillW = Mathf.Max(0, w * progress);
            EditorGUI.DrawRect(new Rect(padding, y, fillW, barHeight), BarFillColor);

            // Percentage label centered on bar
            var pctStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            GUI.Label(barBg, $"{Mathf.RoundToInt(progress * 100)}%", pctStyle);
            y += barHeight + 10f;

            // Message
            var msgStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = true,
                normal = { textColor = MsgColor }
            };
            GUI.Label(new Rect(padding, y, w, 30), message, msgStyle);
        }

        #endregion

        #region Helpers

        private void closeInternal() => base.Close();

        private void CenterOnScreen()
        {
            Rect main = EditorGUIUtility.GetMainWindowPosition();
            float x = main.x + (main.width - minSize.x) * 0.5f;
            float y = main.y + (main.height - minSize.y) * 0.5f;
            position = new Rect(x, y, minSize.x, minSize.y);
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        #endregion
    }
}
