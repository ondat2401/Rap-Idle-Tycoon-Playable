using UnityEditor;
using UnityEngine;
using Amanotes.Core;

namespace Amanotes.Core.Editor
{
    /// <summary>
    /// EditorWindow shell for Project Tabs. Delegates to ProjectTabsBackend (data)
    /// and ProjectTabsUI (rendering). Lightweight orchestrator only.
    /// </summary>
    public class ProjectTabsWindow : EditorWindow
    {
        private ProjectTabsBackend backend;
        private ProjectTabsUI ui;

        [MenuItem("Tools/Playable Standard Pipeline/Project Tabs #t")]
        public static void ShowWindow()
        {
            var window = GetWindow<ProjectTabsWindow>("Project Tabs");
            window.minSize = new Vector2(350, 250);
        }

        [MenuItem("Tools/Playable Standard Pipeline/Project Tabs (Float)")]
        public static void ShowWindowFloat()
        {
            // Utility window = always on top of other Unity windows
            var window = GetWindow<ProjectTabsWindow>(true, "Project Tabs", true);
            window.minSize = new Vector2(350, 250);
        }

        [MenuItem("Window/General/Project Tabs")]
        public static void ShowWindowFromMenu() => ShowWindow();

        private void OnEnable()
        {
            backend = new ProjectTabsBackend();
            ui = new ProjectTabsUI(backend);
            backend.LoadAll();
            if (backend.rootPaths.Count == 0)
                backend.AddPanel(Application.dataPath);
            if (backend.activePanelIndex < 0)
                backend.activePanelIndex = 0;

            titleContent = new GUIContent("Tabs", EditorGUIUtility.IconContent("d_Project").image);

            Selection.selectionChanged += OnUnitySelectionChanged;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnUnitySelectionChanged;
        }

        /// <summary>
        /// When user clicks a script/asset in Inspector or Project window,
        /// auto-navigate first panel to that asset's folder (unless locked).
        /// </summary>

        private void OnUnitySelectionChanged()
        {
            if (backend == null) return;
            if (backend.suppressExternalSelectionCallback) return;
            if (backend.isFirstPanelLocked) return;
            if (backend.rootPaths.Count == 0) return;

            if (Selection.activeObject == null) return;
            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(assetPath)) return;
            // Ignore scene objects (no asset path or "Assets" root itself)
            if (assetPath == "Assets") return;

            if (backend.NavigateFirstPanelToAsset(assetPath))
                Repaint();
        }

        private void OnGUI()
        {
            ui.InitStyles();
            backend.BeginFrame();
            backend.EnsureRatios();

            // Tab bar (replaces old toolbar + header)
            ui.DrawTabBar(this);

            if (backend.rootPaths.Count == 0)
            {
                EditorGUILayout.HelpBox("No panels. Click '+' to add.", MessageType.Info);
                return;
            }

            // Favorites bar
            ui.DrawFavoritesBar(this);

            // Main content area: optional quick access sidebar + panels
            EditorGUILayout.BeginHorizontal();

            // Quick access sidebar
            ui.DrawQuickAccessSidebar(this);

            // Panel layout — horizontal scroll so many panels don't get squished
            int count = backend.rootPaths.Count;
            float splitterW = ui.GetSplitterWidth();
            float minW = ui.GetMinPanelWidth();
            float sidebarW = ui.showQuickAccess ? 134f : 0f;
            float totalMinWidth = count * minW + (count - 1) * splitterW;
            float windowContentW = position.width - sidebarW;
            float contentWidth = Mathf.Max(windowContentW, totalMinWidth);
            float available = contentWidth - (count - 1) * splitterW;
            float[] widths = new float[count];
            for (int i = 0; i < count; i++)
                widths[i] = Mathf.Max(available * backend.panelRatios[i], minW);

            ui.windowScrollPos = EditorGUILayout.BeginScrollView(ui.windowScrollPos, true, false,
                GUI.skin.horizontalScrollbar, GUIStyle.none, GUIStyle.none);

            EditorGUILayout.BeginHorizontal(GUILayout.Width(contentWidth));
            ui.splitterRects.Clear();
            ui.panelRects.Clear();

            for (int i = 0; i < count; i++)
            {
                Rect pr = EditorGUILayout.BeginVertical(GUILayout.Width(widths[i]));
                backend.EnsureListSize(i);
                ui.DrawPanelNav(i, this);
                ui.DrawPanelContent(i, this);
                ui.DrawStatusBar(i);
                EditorGUILayout.EndVertical();
                ui.panelRects.Add(pr);

                ui.DrawMinWidthIndicator(pr, widths[i]);

                if (i < count - 1)
                {
                    Rect sr = GUILayoutUtility.GetRect(splitterW, 0, GUILayout.Width(splitterW), GUILayout.ExpandHeight(true));
                    ui.splitterRects.Add(sr);
                    ui.DrawSplitterVisual(sr, i);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndHorizontal(); // end main content area

            // Input handling
            ui.HandleSplitterInput(available, this);
            ui.HandleGlobalDrag(this);
            ui.HandleTabReorder(this);
            ui.HandleCrossPanelDrop();
            ui.HandleKeyboardNavigation(this);

            // Deferred remove
            if (ui.pendingRemoveIndex >= 0)
            {
                int removed = ui.pendingRemoveIndex;
                backend.RemovePanel(removed);
                ui.pendingRemoveIndex = -1;
                // Clamp activePanelIndex
                if (backend.activePanelIndex >= backend.rootPaths.Count)
                    backend.activePanelIndex = backend.rootPaths.Count - 1;
                backend.selectedPaths.Clear();
                backend.lastClickedPath = null;
                Repaint();
            }

            if (Event.current.type == EventType.MouseMove)
            {
                // Only repaint if hovering for tooltip, not unconditionally
                if (ui.NeedsHoverRepaint()) Repaint();
            }

            // File preview tooltip (drawn last, on top)
            ui.DrawFilePreviewTooltip(this);
        }
    }
}
