using UnityEngine;
using System.Collections.Generic;

namespace Amanotes.Core
{
    /// <summary>
    /// Central manager for UI screens
    /// Coding standard alias: GUIManager
    /// </summary>
    public class GUIManager : SingletonMonoDontDestroy<GUIManager>
    {
        #region Serialized Fields

        [Header("Config")]
        [SerializeField] private UIConfig config;

        [Header("Canvas")]
        [SerializeField] private Canvas rootCanvas;

        #endregion

        #region Private Fields

        private Dictionary<string, IGUIBase> screenRegistry = new Dictionary<string, IGUIBase>();
        private Stack<IGUIBase> screenHistory = new Stack<IGUIBase>();
        private List<IGUIBase> visibleOverlays = new List<IGUIBase>();
        private IGUIBase currentScreen;

        #endregion

        #region Properties

        public UIConfig Config => config;

        #endregion

        #region Initialization

        protected override void Awake()
        {
            base.Awake();
            InitializeRootCanvas();
        }

        private void InitializeRootCanvas()
        {
            if (rootCanvas == null)
                rootCanvas = GetComponentInChildren<Canvas>();

            if (rootCanvas == null)
            {
                GameObject canvasGO = new GameObject("UIRoot Canvas");
                canvasGO.transform.SetParent(transform);
                rootCanvas = canvasGO.AddComponent<Canvas>();
                rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            AutoRegisterScreens();
        }

        /// <summary>
        /// Auto-register all GUIBase components found in Canvas Root children
        /// </summary>
        private void AutoRegisterScreens()
        {
            if (rootCanvas == null) return;

            var screens = rootCanvas.GetComponentsInChildren<GUIBase>(true);

            for (int i = 0; i < screens.Length; i++)
            {
                if (!string.IsNullOrEmpty(screens[i].ScreenId))
                    RegisterScreen(screens[i]);
                else
                    SDebug.LogWarning($"[GUIManager] Screen {screens[i].gameObject.name} has empty Screen ID. Skipping.");
            }

            SDebug.Log($"[GUIManager] Auto-registered {screenRegistry.Count} screens from Canvas Root.");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Register a screen
        /// </summary>
        public void RegisterScreen(IGUIBase screen)
        {
            if (screen == null) return;

            if (!screenRegistry.ContainsKey(screen.ScreenId))
            {
                screenRegistry.Add(screen.ScreenId, screen);
                SDebug.Log($"[GUIManager] Registered screen: {screen.ScreenId}");
            }
        }

        /// <summary>
        /// Unregister a screen
        /// </summary>
        public void UnregisterScreen(string screenId)
        {
            screenRegistry.Remove(screenId);
        }

        /// <summary>
        /// Show a screen by Screen ID
        /// </summary>
        public void ShowScreen(string screenId, bool addToHistory = true)
        {
            var screen = ResolveScreen(screenId);
            if (screen == null) return;

            CloseAllOverlays();

            if (currentScreen != null && currentScreen != screen)
            {
                if (addToHistory)
                    screenHistory.Push(currentScreen);

                currentScreen.Hide();
            }

            currentScreen = screen;
            screen.Show();
        }

        /// <summary>
        /// Show a screen stacked on top of the current screen without hiding it.
        /// Use for popups such as revive, pause or reward panels.
        /// </summary>
        public void ShowOverlay(string screenId)
        {
            var screen = ResolveScreen(screenId);
            if (screen == null) return;
            if (screen == currentScreen)
            {
                screen.Show();
                return;
            }

            if (!visibleOverlays.Contains(screen))
                visibleOverlays.Add(screen);

            if (screen is Component component)
                component.transform.SetAsLastSibling();

            screen.Show();
        }

        /// <summary>
        /// Hide an overlay previously shown with ShowOverlay. Leaves the current screen untouched.
        /// </summary>
        public void CloseOverlay(string screenId)
        {
            if (!screenRegistry.TryGetValue(screenId, out IGUIBase screen)) return;

            visibleOverlays.Remove(screen);
            screen.Hide();
        }

        /// <summary>
        /// Alias for ShowScreen — matches coding standard naming
        /// Usage: GUIManager.Instance.ShowGUI(GUIName.GUI_Gameplay);
        /// </summary>
        public void ShowGUI(string screenId, bool addToHistory = true)
        {
            ShowScreen(screenId, addToHistory);
        }

        /// <summary>
        /// Show screen with data
        /// </summary>
        public void ShowGUI<T>(string screenId, T data, bool addToHistory = true)
        {
            ShowScreen(screenId, data, addToHistory);
        }

        /// <summary>
        /// Show screen with data (Liskov Substitution)
        /// </summary>
        public void ShowScreen<T>(string screenId, T data, bool addToHistory = true)
        {
            var screen = ResolveScreen(screenId);
            if (screen == null) return;

            if (screen is IScreenWithData<T> screenWithData)
                screenWithData.SetData(data);

            ShowScreen(screenId, addToHistory);
        }

        /// <summary>
        /// Go back to previous screen
        /// </summary>
        public void GoBack()
        {
            if (screenHistory.Count == 0)
            {
                SDebug.LogWarning("[GUIManager] No screen history available");
                return;
            }

            CloseAllOverlays();
            currentScreen?.Hide();
            currentScreen = screenHistory.Pop();
            currentScreen?.Show();
        }

        public void CloseGUI(string screenId)
        {
            if (!screenRegistry.TryGetValue(screenId, out IGUIBase screen)) return;

            var wasOverlay = visibleOverlays.Remove(screen);
            screen.Hide();

            if (wasOverlay || currentScreen != screen) return;

            currentScreen = screenHistory.Count > 0 ? screenHistory.Pop() : null;
            currentScreen?.Show();
        }

        public void HideAllScreens()
        {
            foreach (var screen in screenRegistry.Values)
                screen.Hide();

            visibleOverlays.Clear();
            currentScreen = null;
            screenHistory.Clear();
        }

        /// <summary>
        /// Get screen by ID
        /// </summary>
        public T GetScreen<T>(string screenId) where T : class, IGUIBase
        {
            if (screenRegistry.TryGetValue(screenId, out IGUIBase screen))
                return screen as T;

            return null;
        }

        #endregion

        #region Private Methods

        private IGUIBase ResolveScreen(string screenId)
        {
            if (screenRegistry.TryGetValue(screenId, out IGUIBase screen))
                return screen;

            screen = FindAndRegisterScreen(screenId);
            if (screen == null)
                SDebug.LogError($"[GUIManager] Screen not found: {screenId}");

            return screen;
        }

        private void CloseAllOverlays()
        {
            for (int i = visibleOverlays.Count - 1; i >= 0; i--)
                visibleOverlays[i].Hide();

            visibleOverlays.Clear();
        }

        private IGUIBase FindAndRegisterScreen(string screenId)
        {
            if (rootCanvas == null) return null;

            var screens = rootCanvas.GetComponentsInChildren<GUIBase>(true);

            for (int i = 0; i < screens.Length; i++)
            {
                if (screens[i].ScreenId == screenId)
                {
                    RegisterScreen(screens[i]);
                    return screens[i];
                }
            }

            return null;
        }

        #endregion
    }
}
