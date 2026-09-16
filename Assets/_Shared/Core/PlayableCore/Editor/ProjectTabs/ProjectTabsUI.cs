using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amanotes.Core;

namespace Amanotes.Core.Editor
{
    /// <summary>
    /// UI renderer for Project Tabs: styles, drawing, input handling.
    /// All drawing delegates to backend for data/state.
    /// </summary>
    public class ProjectTabsUI
    {
        #region Constants

        private const float DRAG_THRESHOLD = 8f;
        private const float SPLITTER_WIDTH = 6f;
        private const float MIN_PANEL_WIDTH = 120f;
        private const float GRID_ITEM_SIZE = 72f;
        private const int BREADCRUMB_MAX_CHARS = 14;
        private const float TAB_HEIGHT = 22f;

        #endregion

        #region State

        private ProjectTabsBackend B;
        private bool stylesInitialized;
        public bool showFavorites;
        public int pendingRemoveIndex = -1;

        // Splitter
        private int activeSplitter = -1;
        private float splitterDragStartX, splitterDragStartRatioL, splitterDragStartRatioR;
        public List<Rect> splitterRects = new List<Rect>(8);

        // File drag
        private bool isDragReady;
        private Vector2 dragStartPos;
        private string dragCandidatePath;

        // Panel reorder (via tab bar drag)
        public int reorderDragIndex = -1;
        public int reorderDropTarget = -1;
        public List<Rect> panelRects = new List<Rect>(8);
        private List<Rect> tabRects = new List<Rect>(8);

        // Cross-panel drag
        private bool isCrossPanelDrag;
        private int crossDragSourcePanel = -1;

        // Breadcrumb edit mode
        private int breadcrumbEditPanel = -1;
        private string breadcrumbEditText = "";

        // Window-level horizontal scroll for many panels
        public Vector2 windowScrollPos;

        // Quick access sidebar
        public bool showQuickAccess = false;
        private Vector2 quickAccessScroll;

        // File preview tooltip
        private string hoverPath;
        private double hoverStartTime;
        private const double TOOLTIP_DELAY = 0.5;

        // Grid preview cache to avoid LoadAssetAtPath every frame
        private Dictionary<string, Texture2D> gridPreviewCache = new Dictionary<string, Texture2D>();
        private double gridPreviewCacheTime;
        private const double GRID_PREVIEW_CACHE_TTL = 3.0;

        // Tab colors for color-coded tabs
        private static readonly Color[] tabColors = new Color[]
        {
            new Color(0.35f, 0.65f, 0.95f, 0.7f), // blue
            new Color(0.45f, 0.80f, 0.45f, 0.7f), // green
            new Color(0.90f, 0.55f, 0.35f, 0.7f), // orange
            new Color(0.75f, 0.45f, 0.85f, 0.7f), // purple
            new Color(0.90f, 0.75f, 0.30f, 0.7f), // yellow
            new Color(0.40f, 0.80f, 0.80f, 0.7f), // teal
            new Color(0.90f, 0.45f, 0.55f, 0.7f), // pink
            new Color(0.60f, 0.70f, 0.45f, 0.7f), // olive
        };

        #endregion

        #region Styles

        private Color hoverColor, separatorColor, splitterHoverColor, selectColor;
        private Color altRowColor;
        private GUIStyle itemStyle, breadcrumbStyle, breadcrumbActiveStyle;
        private GUIStyle gridLabelStyle, statusBarStyle;
        private GUIStyle tabStyle, tabActiveStyle;
        private GUIStyle emptyStateStyle, monoSmallStyle;

        #endregion

        public ProjectTabsUI(ProjectTabsBackend backend)
        {
            B = backend;
        }

        #region Init Styles

        public void InitStyles()
        {
            if (stylesInitialized) return;
            bool dark = EditorGUIUtility.isProSkin;
            hoverColor = dark ? new Color(1f, 1f, 1f, 0.06f) : new Color(0f, 0f, 0f, 0.06f);
            selectColor = dark ? new Color(0.17f, 0.36f, 0.53f) : new Color(0.24f, 0.48f, 0.9f, 0.3f);
            separatorColor = dark ? new Color(0.12f, 0.12f, 0.12f) : new Color(0.65f, 0.65f, 0.65f);
            splitterHoverColor = dark ? new Color(0.35f, 0.6f, 0.9f, 0.5f) : new Color(0.2f, 0.45f, 0.85f, 0.4f);
            altRowColor = dark ? new Color(1f, 1f, 1f, 0.02f) : new Color(0f, 0f, 0f, 0.03f);

            itemStyle = new GUIStyle(EditorStyles.label)
            { fixedHeight = 0, fontSize = 11, padding = new RectOffset(2, 2, 3, 3), margin = new RectOffset(0, 0, 0, 0) };

            breadcrumbStyle = new GUIStyle(EditorStyles.toolbarButton)
            { fontSize = 10, fixedHeight = 18, padding = new RectOffset(4, 4, 1, 1), margin = new RectOffset(0, 0, 0, 0) };
            breadcrumbActiveStyle = new GUIStyle(breadcrumbStyle) { fontStyle = FontStyle.Bold, margin = new RectOffset(0, 0, 0, 0) };

            gridLabelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            { fontSize = 9, wordWrap = true, fixedHeight = 0, clipping = TextClipping.Clip };

            statusBarStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 9, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(4, 4, 1, 1),
                normal = { textColor = dark ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.45f, 0.45f, 0.45f) }
            };

            tabStyle = new GUIStyle(EditorStyles.toolbarButton)
            { fontSize = 10, fixedHeight = TAB_HEIGHT, padding = new RectOffset(8, 16, 2, 2), alignment = TextAnchor.MiddleLeft };
            tabActiveStyle = new GUIStyle(tabStyle) { fontStyle = FontStyle.Bold };

            emptyStateStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            { fontSize = 11, wordWrap = true, alignment = TextAnchor.MiddleCenter };

            monoSmallStyle = new GUIStyle(EditorStyles.miniTextField)
            { fontSize = 10, fixedHeight = 18 };

            stylesInitialized = true;
        }

        #endregion

        #region Tab Bar

        private Vector2 tabScrollPos;

        public void DrawTabBar(EditorWindow window)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(TAB_HEIGHT));

            // Scrollable tab area
            float fixedButtonsWidth = 22 + 22 + 26 + 8; // +, ★, refresh + spacing
            float availableWidth = window.position.width - fixedButtonsWidth;
            tabScrollPos = EditorGUILayout.BeginScrollView(tabScrollPos, false, false,
                GUI.skin.horizontalScrollbar, GUIStyle.none, GUIStyle.none,
                GUILayout.Height(TAB_HEIGHT), GUILayout.MaxWidth(availableWidth));

            EditorGUILayout.BeginHorizontal();
            tabRects.Clear();

            for (int i = 0; i < B.rootPaths.Count; i++)
            {
                bool isActive = (B.activePanelIndex == i);
                string label = Path.GetFileName(B.browsePaths[i]);
                if (string.IsNullOrEmpty(label)) label = "Assets";
                if (label.Length > 16) label = label.Substring(0, 15) + "…";

                GUIStyle style = isActive ? tabActiveStyle : tabStyle;
                Rect tabRect = GUILayoutUtility.GetRect(new GUIContent(label), style, GUILayout.MinWidth(60), GUILayout.MaxWidth(140));
                tabRects.Add(tabRect);

                // Color-coded top border
                Color tc = tabColors[i % tabColors.Length];
                EditorGUI.DrawRect(new Rect(tabRect.x, tabRect.y, tabRect.width, 2f), isActive ? tc : tc * 0.5f);

                // Tab background
                if (isActive)
                {
                    Color bg = EditorGUIUtility.isProSkin ? new Color(0.22f, 0.22f, 0.22f) : new Color(0.85f, 0.85f, 0.85f);
                    EditorGUI.DrawRect(new Rect(tabRect.x, tabRect.y + 2, tabRect.width, tabRect.height - 2), bg);
                }

                // Tab label
                float labelX = tabRect.x + 4;
                // Lock icon for first tab (auto-follow toggle)
                if (i == 0)
                {
                    GUIContent lockContent = B.isFirstPanelLocked
                        ? new GUIContent(EditorGUIUtility.IconContent("IN LockButton on act").image, "Locked (click to unlock auto-follow)")
                        : new GUIContent(EditorGUIUtility.IconContent("IN LockButton").image, "Unlocked (auto-follows selection)");
                    Rect lockRect = new Rect(tabRect.x + 2, tabRect.y + 3, 16, 16);
                    if (GUI.Button(lockRect, lockContent, GUIStyle.none))
                    {
                        B.isFirstPanelLocked = !B.isFirstPanelLocked;
                        B.SaveAll();
                    }
                    labelX = tabRect.x + 20;
                }
                GUI.Label(new Rect(labelX, tabRect.y + 2, tabRect.xMax - 20 - labelX, tabRect.height - 2),
                    new GUIContent(label, B.GetAssetRelativeLabel(B.browsePaths[i])), style);

                // Close button on tab
                if (B.rootPaths.Count > 1)
                {
                    Rect closeRect = new Rect(tabRect.xMax - 16, tabRect.y + 3, 14, 14);
                    if (GUI.Button(closeRect, "✕", EditorStyles.miniLabel))
                        pendingRemoveIndex = i;
                }

                // Tab click (skip lock icon area on first tab)
                if (Event.current.type == EventType.MouseDown && tabRect.Contains(Event.current.mousePosition))
                {
                    // Don't handle tab click if clicking lock icon on first tab
                    bool onLockIcon = (i == 0 && Event.current.mousePosition.x < tabRect.x + 20);
                    if (!onLockIcon)
                    {
                        if (Event.current.button == 0)
                        {
                            B.activePanelIndex = i;
                            GUI.FocusControl(null);
                            reorderDragIndex = i;
                            reorderDropTarget = -1;
                            Event.current.Use();
                        }
                        else if (Event.current.button == 1)
                        {
                            ShowTabContextMenu(i, window);
                            Event.current.Use();
                        }
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();

            // Add tab button (left click = Assets, right click = menu)
            Rect addRect = GUILayoutUtility.GetRect(new GUIContent("+"), EditorStyles.toolbarButton, GUILayout.Width(22));
            if (GUI.Button(addRect, "+", EditorStyles.toolbarButton))
            {
               B.AddPanel(Application.dataPath);
            }

            GUILayout.FlexibleSpace();

            // Favorites toggle
            bool favActive = showFavorites && B.favoritePaths.Count > 0;
            if (GUILayout.Button(new GUIContent("★", "Favorites"), favActive ? breadcrumbActiveStyle : EditorStyles.toolbarButton, GUILayout.Width(22)))
                showFavorites = !showFavorites;

            // Quick access toggle
            if (GUILayout.Button(new GUIContent("☰", "Quick Access"), showQuickAccess ? breadcrumbActiveStyle : EditorStyles.toolbarButton, GUILayout.Width(22)))
                showQuickAccess = !showQuickAccess;

            // Compact mode toggle
            if (GUILayout.Button(new GUIContent(B.compactMode ? "▾" : "▴", "Compact Mode"), EditorStyles.toolbarButton, GUILayout.Width(22)))
                B.compactMode = !B.compactMode;

            // Refresh
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_Refresh", "Refresh"), EditorStyles.toolbarButton, GUILayout.Width(26)))
            { B.InvalidateCache(); AssetDatabase.Refresh(); }

            EditorGUILayout.EndHorizontal();
        }

        private void ShowTabContextMenu(int index, EditorWindow window)
        {
            GenericMenu menu = new GenericMenu();
            // Lock toggle for first tab
            if (index == 0)
            {
                menu.AddItem(new GUIContent(B.isFirstPanelLocked ? "Unlock Auto-Follow" : "Lock Auto-Follow"), B.isFirstPanelLocked,
                    () => { B.isFirstPanelLocked = !B.isFirstPanelLocked; B.SaveAll(); window.Repaint(); });
                menu.AddSeparator("");
            }
            menu.AddItem(new GUIContent("Add Tab"), false, () => { B.AddPanel(Application.dataPath); window.Repaint(); });
            if (B.rootPaths.Count > 1)
                menu.AddItem(new GUIContent("Close Tab"), false, () => { pendingRemoveIndex = index; window.Repaint(); });
            else
                menu.AddDisabledItem(new GUIContent("Close Tab"));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Close Other Tabs"), B.rootPaths.Count > 1, () => { B.CloseOtherTabs(index); window.Repaint(); });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Duplicate Tab"), false, () => { B.DuplicateTab(index); window.Repaint(); });
            menu.AddItem(new GUIContent("Reset to Assets Root"), false, () => { B.NavigateTo(index, Application.dataPath); window.Repaint(); });
            menu.ShowAsContext();
        }

        public void HandleTabReorder(EditorWindow window)
        {
            if (reorderDragIndex < 0) return;
            Event e = Event.current;
            if (e.type == EventType.MouseDrag)
            {
                reorderDropTarget = -1;
                for (int i = 0; i < tabRects.Count; i++)
                {
                    if (i != reorderDragIndex && tabRects[i].Contains(e.mousePosition))
                    { reorderDropTarget = i; break; }
                }
                e.Use(); window.Repaint();
            }
            if (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp)
            {
                if (reorderDropTarget >= 0 && reorderDropTarget != reorderDragIndex)
                    B.SwapPanels(reorderDragIndex, reorderDropTarget);
                reorderDragIndex = -1; reorderDropTarget = -1;
                e.Use(); window.Repaint();
            }
        }

        #endregion

        #region Favorites Bar

        public void DrawFavoritesBar(EditorWindow window)
        {
            if (!showFavorites || B.favoritePaths.Count == 0) return;
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("★", EditorStyles.miniLabel, GUILayout.Width(14));
            foreach (string fav in B.favoritePaths.ToArray())
            {
                string label = Path.GetFileName(fav);
                if (string.IsNullOrEmpty(label)) label = "Assets";
                if (GUILayout.Button(new GUIContent(label, B.GetAssetRelativeLabel(fav)), EditorStyles.toolbarButton, GUILayout.MaxWidth(80)))
                {
                    int pi = B.activePanelIndex >= 0 && B.activePanelIndex < B.rootPaths.Count ? B.activePanelIndex : 0;
                    B.NavigateTo(pi, fav);
                    window.Repaint();
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Panel Nav

        public void DrawPanelNav(int index, EditorWindow window)
        {
            string browsePath = B.browsePaths[index];
            string rootPath = B.rootPaths[index];

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Back
            bool canBack = B.navHistoryBack[index].Count > 0;
            EditorGUI.BeginDisabledGroup(!canBack);
            if (GUILayout.Button("◀", EditorStyles.toolbarButton, GUILayout.Width(20)))
            { B.NavigateBack(index); window.Repaint(); }
            EditorGUI.EndDisabledGroup();

            // Forward
            bool canFwd = B.navHistoryForward[index].Count > 0;
            EditorGUI.BeginDisabledGroup(!canFwd);
            if (GUILayout.Button("▶", EditorStyles.toolbarButton, GUILayout.Width(20)))
            { B.NavigateForward(index); window.Repaint(); }
            EditorGUI.EndDisabledGroup();

            // Up
            bool canUp = browsePath != rootPath;
            EditorGUI.BeginDisabledGroup(!canUp);
            if (GUILayout.Button("↑", EditorStyles.toolbarButton, GUILayout.Width(20)))
            {
                try
                {
                    string parent = Directory.GetParent(browsePath)?.FullName;
                    if (parent != null && Path.GetFullPath(parent).Length >= Path.GetFullPath(rootPath).Length)
                    { B.NavigateTo(index, parent); window.Repaint(); }
                }
                catch { }
            }
            EditorGUI.EndDisabledGroup();

            // Breadcrumb (click-to-edit)
            if (breadcrumbEditPanel >= B.rootPaths.Count) breadcrumbEditPanel = -1;
            if (breadcrumbEditPanel == index)
                DrawBreadcrumbEdit(index, window);
            else
                DrawBreadcrumb(index, window);

            // Favorite
            bool isFav = B.favoritePaths.Contains(browsePath);
            if (GUILayout.Button(new GUIContent(isFav ? "★" : "☆", "Toggle Favorite"), EditorStyles.toolbarButton, GUILayout.Width(20)))
            { B.ToggleFavorite(browsePath); window.Repaint(); }

            // Reveal
            if (GUILayout.Button(EditorGUIUtility.IconContent("d_FolderOpened Icon", "Reveal"), EditorStyles.toolbarButton, GUILayout.Width(22)))
                EditorUtility.RevealInFinder(browsePath);

            // View toggle
            string viewIcon = B.viewModes[index] == ProjectTabsBackend.ViewMode.List ? "d_align_vertically" : "d_GridLayoutGroup Icon";
            if (GUILayout.Button(EditorGUIUtility.IconContent(viewIcon, "Toggle View"), EditorStyles.toolbarButton, GUILayout.Width(22)))
                B.viewModes[index] = B.viewModes[index] == ProjectTabsBackend.ViewMode.List ? ProjectTabsBackend.ViewMode.Grid : ProjectTabsBackend.ViewMode.List;

            EditorGUILayout.EndHorizontal();

            // Search + filter row (hidden in compact mode)
            if (!B.compactMode)
            {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(2);
            B.searchFilters[index] = EditorGUILayout.TextField(B.searchFilters[index], EditorStyles.toolbarSearchField);
            if (!string.IsNullOrEmpty(B.searchFilters[index]))
            { if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(18))) { B.searchFilters[index] = ""; GUI.FocusControl(null); } }

            // Type filter
            string tfLabel = B.typeFilters[index] == ProjectTabsBackend.TypeFilter.All ? "All" : B.typeFilters[index].ToString().Substring(0, 2);
            if (GUILayout.Button(new GUIContent(tfLabel, "Type: " + B.typeFilters[index]), EditorStyles.toolbarButton, GUILayout.Width(26)))
            {
                GenericMenu tfm = new GenericMenu();
                foreach (ProjectTabsBackend.TypeFilter tf in System.Enum.GetValues(typeof(ProjectTabsBackend.TypeFilter)))
                {
                    var captured = tf;
                    tfm.AddItem(new GUIContent(tf.ToString()), B.typeFilters[index] == tf, () => { B.typeFilters[index] = captured; window.Repaint(); });
                }
                tfm.ShowAsContext();
            }

            // Sort
            string sortLabel = B.sortModes[index] == ProjectTabsBackend.SortMode.Name ? "Az" :
                               B.sortModes[index] == ProjectTabsBackend.SortMode.Type ? "Ty" : "Dt";
            if (GUILayout.Button(new GUIContent(sortLabel, "Sort: " + B.sortModes[index]), EditorStyles.toolbarButton, GUILayout.Width(24)))
            {
                GenericMenu sm = new GenericMenu();
                sm.AddItem(new GUIContent("Name"), B.sortModes[index] == ProjectTabsBackend.SortMode.Name, () => { B.sortModes[index] = ProjectTabsBackend.SortMode.Name; window.Repaint(); });
                sm.AddItem(new GUIContent("Type"), B.sortModes[index] == ProjectTabsBackend.SortMode.Type, () => { B.sortModes[index] = ProjectTabsBackend.SortMode.Type; window.Repaint(); });
                sm.AddItem(new GUIContent("Date Modified"), B.sortModes[index] == ProjectTabsBackend.SortMode.DateModified, () => { B.sortModes[index] = ProjectTabsBackend.SortMode.DateModified; window.Repaint(); });
                sm.ShowAsContext();
            }
            GUILayout.Space(2);
            EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawBreadcrumb(int index, EditorWindow window)
        {
            string browsePath = B.browsePaths[index];
            string rootPath = B.rootPaths[index];
            string rootLabel = B.GetAssetRelativeLabel(rootPath);
            string rel = B.GetRelativePath(rootPath, browsePath);

            string rootDisplay = Truncate(rootLabel, BREADCRUMB_MAX_CHARS);
            if (GUILayout.Button(new GUIContent(rootDisplay, rootLabel), string.IsNullOrEmpty(rel) ? breadcrumbActiveStyle : breadcrumbStyle, GUILayout.MaxWidth(80)))
            { B.NavigateTo(index, rootPath); window.Repaint(); }

            if (string.IsNullOrEmpty(rel)) return;
            string[] parts = rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Where(p => !string.IsNullOrEmpty(p)).ToArray();
            string acc = rootPath;
            for (int i = 0; i < parts.Length; i++)
            {
                acc = Path.Combine(acc, parts[i]);
                string pp = acc;
                string display = Truncate(parts[i], BREADCRUMB_MAX_CHARS);

                // Double-click last segment to edit path
                Rect btnRect = GUILayoutUtility.GetRect(new GUIContent(display), i == parts.Length - 1 ? breadcrumbActiveStyle : breadcrumbStyle, GUILayout.MaxWidth(80));
                if (Event.current.type == EventType.MouseDown && btnRect.Contains(Event.current.mousePosition))
                {
                    if (Event.current.clickCount == 2 && i == parts.Length - 1)
                    {
                        breadcrumbEditPanel = index;
                        breadcrumbEditText = B.GetAssetRelativeLabel(browsePath);
                        Event.current.Use();
                        return;
                    }
                    else if (Event.current.button == 0)
                    {
                        B.NavigateTo(index, pp); window.Repaint();
                        Event.current.Use();
                        return;
                    }
                }
                GUI.Label(btnRect, new GUIContent(display, parts[i]), i == parts.Length - 1 ? breadcrumbActiveStyle : breadcrumbStyle);
            }
        }

        private void DrawBreadcrumbEdit(int index, EditorWindow window)
        {
            GUI.SetNextControlName("BreadcrumbEdit");
            breadcrumbEditText = EditorGUILayout.TextField(breadcrumbEditText, monoSmallStyle, GUILayout.ExpandWidth(true));
            EditorGUI.FocusTextInControl("BreadcrumbEdit");

            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                {
                    // Try to resolve the typed path
                    string resolved = breadcrumbEditText.Replace("Assets", Application.dataPath);
                    try { resolved = Path.GetFullPath(resolved); } catch { }
                    if (Directory.Exists(resolved))
                    { B.NavigateTo(index, resolved); window.Repaint(); }
                    breadcrumbEditPanel = -1;
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.Escape)
                {
                    breadcrumbEditPanel = -1;
                    Event.current.Use();
                }
            }
        }

        private string Truncate(string text, int maxChars)
        {
            if (text.Length <= maxChars) return text;
            return text.Substring(0, maxChars - 1) + "…";
        }

        #endregion

        #region Panel Content

        public void DrawPanelContent(int index, EditorWindow window)
        {
            string browsePath = B.browsePaths[index];
            if (!Directory.Exists(browsePath))
            { EditorGUILayout.HelpBox("Folder not found.", MessageType.Warning); return; }

            if (Event.current.type == EventType.MouseDown)
                B.activePanelIndex = index;

            // Ctrl+A
            if (Event.current.type == EventType.KeyDown && B.activePanelIndex == index
                && Event.current.keyCode == KeyCode.A && (Event.current.control || Event.current.command))
            {
                B.SelectAll(index);
                Event.current.Use();
                window.Repaint();
            }

            B.scrollPositions[index] = EditorGUILayout.BeginScrollView(B.scrollPositions[index]);

            if (Event.current.type == EventType.ContextClick)
            { ShowBackgroundContextMenu(browsePath, window); Event.current.Use(); }

            var entries = B.GetSortedEntries(browsePath, index);

            if (entries.Count == 0)
                DrawEmptyState();
            else if (B.viewModes[index] == ProjectTabsBackend.ViewMode.Grid)
                DrawGridView(index, entries, window);
            else
                DrawListView(index, entries, window);

            // Click empty space to deselect
            Rect emptyRect = GUILayoutUtility.GetRect(0, Mathf.Max(20, 0), GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && emptyRect.Contains(Event.current.mousePosition))
            {
                B.ClearSelection();
                Event.current.Use(); window.Repaint();
            }

            EditorGUILayout.EndScrollView();
            HandleDropTarget(index, window);
        }

        private void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical();
            GUILayout.Label(EditorGUIUtility.IconContent("d_FolderEmpty Icon"), GUILayout.Width(48), GUILayout.Height(48));
            GUILayout.Label("Drop files here or right-click to create", emptyStateStyle, GUILayout.Width(200));
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        #endregion

        #region Status Bar

        public void DrawStatusBar(int index)
        {
            string browsePath = B.browsePaths[index];
            if (!Directory.Exists(browsePath)) return;

            var entries = B.GetSortedEntries(browsePath, index);
            int folderCount = 0, fileCount = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].isDirectory) folderCount++;
                else fileCount++;
            }

            string status = $"{folderCount} folders, {fileCount} files";
            if (B.selectedPaths.Count > 0) status += $"  ·  {B.selectedPaths.Count} selected";

            // Color-coded left border matching tab color
            Rect barRect = EditorGUILayout.GetControlRect(false, 16);
            Color tc = tabColors[index % tabColors.Length];
            EditorGUI.DrawRect(new Rect(barRect.x, barRect.y, 2, barRect.height), tc * 0.6f);
            GUI.Label(new Rect(barRect.x + 6, barRect.y, barRect.width - 6, barRect.height), status, statusBarStyle);
        }

        #endregion

        #region List View

        // Track row index for alternating colors across folders/files/expanded children
        private int listRowIndex;

        private void DrawListView(int index, List<ProjectTabsBackend.FileEntry> entries, EditorWindow window)
        {
            string searchFilter = B.searchFilters[index];
            bool hasSearch = !string.IsNullOrEmpty(searchFilter);
            listRowIndex = 0;

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e.isDirectory) DrawFolderItemList(index, e.fullPath, e.name, hasSearch ? searchFilter : null, window);
                else DrawFileItemList(index, e.fullPath, e.name, hasSearch ? searchFilter : null, window);
            }
        }

        private void DrawFolderItemList(int panelIndex, string fullPath, string name, string highlight, EditorWindow window)
        {
            bool isExpanded = B.expandedFolders[panelIndex].Contains(fullPath);

            Rect row = EditorGUILayout.BeginHorizontal(GUILayout.Height(22));
            DrawItemBackground(row, fullPath, panelIndex);

            // Color tag bar (left edge)
            int ct = B.GetColorTag(fullPath);
            if (ct > 0 && ct < ProjectTabsBackend.colorTagColors.Length)
                EditorGUI.DrawRect(new Rect(row.x, row.y, 3, row.height), ProjectTabsBackend.colorTagColors[ct]);

            GUILayout.Space(6);
            B.GetDirectoryContents(fullPath, out string[] subDirs, out string[] subFiles);
            bool hasChildren = subDirs.Length > 0;
            if (!hasChildren)
            {
                for (int j = 0; j < subFiles.Length; j++)
                {
                    if (!subFiles[j].EndsWith(".meta")) { hasChildren = true; break; }
                }
            }

            if (hasChildren)
            {
                string arrow = isExpanded ? "▼" : "▶";
                if (GUILayout.Button(arrow, EditorStyles.miniLabel, GUILayout.Width(14), GUILayout.Height(20)))
                {
                    if (isExpanded) B.expandedFolders[panelIndex].Remove(fullPath);
                    else B.expandedFolders[panelIndex].Add(fullPath);
                }
            }
            else GUILayout.Label("", GUILayout.Width(14), GUILayout.Height(20));

            GUILayout.Label(GetFolderIcon(fullPath), GUILayout.Width(16), GUILayout.Height(20));

            if (B.renamingPath == fullPath)
                DrawInlineRename(fullPath, window);
            else
            {
                Rect lr = GUILayoutUtility.GetRect(new GUIContent(name), itemStyle, GUILayout.ExpandWidth(true));
                HandleItemClick(lr, fullPath, panelIndex, true, window);
                DrawLabelWithHighlight(lr, name, highlight);
            }

            bool hovered = row.Contains(Event.current.mousePosition);
            if (hovered) { if (GUILayout.Button("›", EditorStyles.miniLabel, GUILayout.Width(14), GUILayout.Height(20))) { B.NavigateTo(panelIndex, fullPath); window.Repaint(); } }
            else GUILayout.Label("", GUILayout.Width(14), GUILayout.Height(20));

            GUILayout.Space(2);
            EditorGUILayout.EndHorizontal();

            // Drop into this folder
            HandleFolderDropTarget(row, fullPath, window);

            if (isExpanded)
                DrawExpandedChildren(panelIndex, fullPath, 1, window);
        }

        private void DrawExpandedChildren(int panelIndex, string parentPath, int depth, EditorWindow window)
        {
            if (depth > 8) return;
            B.GetDirectoryContents(parentPath, out string[] subDirs, out string[] subFiles);
            float indent = 16f * depth;

            for (int i = 0; i < subDirs.Length; i++)
            {
                string n = Path.GetFileName(subDirs[i]);
                if (n.StartsWith(".")) continue;

                bool isExpanded = B.expandedFolders[panelIndex].Contains(subDirs[i]);
                B.GetDirectoryContents(subDirs[i], out string[] gd, out string[] gf);
                bool hasChildren = gd.Length > 0;
                if (!hasChildren)
                {
                    for (int j = 0; j < gf.Length; j++)
                    {
                        if (!gf[j].EndsWith(".meta")) { hasChildren = true; break; }
                    }
                }

                Rect row = EditorGUILayout.BeginHorizontal(GUILayout.Height(20));
                GUILayout.Space(indent);
                DrawItemBackground(row, subDirs[i], panelIndex);

                if (hasChildren)
                {
                    if (GUILayout.Button(isExpanded ? "▼" : "▶", EditorStyles.miniLabel, GUILayout.Width(14), GUILayout.Height(18)))
                    { if (isExpanded) B.expandedFolders[panelIndex].Remove(subDirs[i]); else B.expandedFolders[panelIndex].Add(subDirs[i]); }
                }
                else GUILayout.Label("", GUILayout.Width(14), GUILayout.Height(18));

                GUILayout.Label(GetFolderIcon(subDirs[i]), GUILayout.Width(14), GUILayout.Height(18));
                Rect lr = GUILayoutUtility.GetRect(new GUIContent(n), itemStyle, GUILayout.ExpandWidth(true));
                HandleItemClick(lr, subDirs[i], panelIndex, true, window);
                GUI.Label(lr, n, EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();

                if (isExpanded) DrawExpandedChildren(panelIndex, subDirs[i], depth + 1, window);
            }

            for (int i = 0; i < subFiles.Length; i++)
            {
                if (subFiles[i].EndsWith(".meta")) continue;
                string n = Path.GetFileName(subFiles[i]);
                if (n.StartsWith(".")) continue;

                EditorGUILayout.BeginHorizontal(GUILayout.Height(18));
                GUILayout.Space(indent);
                GUILayout.Label(GetFileIcon(n), GUILayout.Width(14), GUILayout.Height(16));
                Rect lr = GUILayoutUtility.GetRect(new GUIContent(n), itemStyle, GUILayout.ExpandWidth(true));
                HandleItemClick(lr, subFiles[i], panelIndex, false, window);
                GUI.Label(lr, n, EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawFileItemList(int panelIndex, string fullPath, string name, string highlight, EditorWindow window)
        {
            Rect row = EditorGUILayout.BeginHorizontal(GUILayout.Height(20));
            DrawItemBackground(row, fullPath, panelIndex);

            // Color tag bar (left edge)
            int ct = B.GetColorTag(fullPath);
            if (ct > 0 && ct < ProjectTabsBackend.colorTagColors.Length)
                EditorGUI.DrawRect(new Rect(row.x, row.y, 3, row.height), ProjectTabsBackend.colorTagColors[ct]);

            GUILayout.Space(6);
            GUILayout.Label(GetFileIcon(name), GUILayout.Width(16), GUILayout.Height(18));

            if (B.renamingPath == fullPath)
                DrawInlineRename(fullPath, window);
            else
            {
                Rect lr = GUILayoutUtility.GetRect(new GUIContent(name), itemStyle, GUILayout.ExpandWidth(true));
                HandleItemClick(lr, fullPath, panelIndex, false, window);
                DrawLabelWithHighlight(lr, name, highlight);
                TrackHover(lr, fullPath);
            }

            // File size column (cached to avoid I/O every frame)
            long size = B.GetFileSizeCached(fullPath);
            GUILayout.Label(ProjectTabsBackend.FormatFileSize(size), statusBarStyle, GUILayout.Width(52));

            GUILayout.Space(2);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>Draw label with search term highlighted in yellow.</summary>
        private void DrawLabelWithHighlight(Rect rect, string text, string highlight)
        {
            if (string.IsNullOrEmpty(highlight))
            {
                GUI.Label(rect, text, itemStyle);
                return;
            }

            // Find match position
            int idx = text.ToLower().IndexOf(highlight.ToLower());
            if (idx < 0)
            {
                GUI.Label(rect, text, itemStyle);
                return;
            }

            // Draw full text first
            GUI.Label(rect, text, itemStyle);

            // Overlay highlight on matched portion
            string before = text.Substring(0, idx);
            string match = text.Substring(idx, highlight.Length);
            float beforeW = itemStyle.CalcSize(new GUIContent(before)).x;
            float matchW = itemStyle.CalcSize(new GUIContent(match)).x;

            Color hlColor = EditorGUIUtility.isProSkin ? new Color(0.9f, 0.8f, 0.2f, 0.25f) : new Color(1f, 0.9f, 0.3f, 0.4f);
            Rect hlRect = new Rect(rect.x + beforeW, rect.y + 1, matchW, rect.height - 2);
            EditorGUI.DrawRect(hlRect, hlColor);
        }

        #endregion

        #region Grid View

        private void DrawGridView(int index, List<ProjectTabsBackend.FileEntry> entries, EditorWindow window)
        {
            float panelW = panelRects.Count > index && panelRects[index].width > 10 ? panelRects[index].width - 20 : 200;
            int cols = Mathf.Max(1, Mathf.FloorToInt(panelW / GRID_ITEM_SIZE));

            int i = 0;
            while (i < entries.Count)
            {
                EditorGUILayout.BeginHorizontal();
                for (int c = 0; c < cols && i < entries.Count; c++, i++)
                    DrawGridItem(index, entries[i], window);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawGridItem(int panelIndex, ProjectTabsBackend.FileEntry entry, EditorWindow window)
        {
            Rect itemRect = GUILayoutUtility.GetRect(GRID_ITEM_SIZE, GRID_ITEM_SIZE + 18, GUILayout.Width(GRID_ITEM_SIZE));
            bool isSelected = B.GetPanelSelection(panelIndex).Contains(entry.fullPath);
            bool isHovered = itemRect.Contains(Event.current.mousePosition);

            if (isSelected) EditorGUI.DrawRect(itemRect, selectColor);
            else if (isHovered) EditorGUI.DrawRect(itemRect, hoverColor);

            // Thumbnail preview for assets, fallback to icon (cached)
            Rect iconRect = new Rect(itemRect.x + (GRID_ITEM_SIZE - 40) / 2, itemRect.y + 2, 40, 40);
            bool drewPreview = false;
            if (!entry.isDirectory)
            {
                double now = EditorApplication.timeSinceStartup;
                if (now - gridPreviewCacheTime > GRID_PREVIEW_CACHE_TTL)
                {
                    gridPreviewCache.Clear();
                    gridPreviewCacheTime = now;
                }

                if (!gridPreviewCache.TryGetValue(entry.fullPath, out Texture2D preview))
                {
                    string ap = B.AssetPathFromAbsolute(entry.fullPath);
                    if (ap != null)
                    {
                        Object asset = AssetDatabase.LoadAssetAtPath<Object>(ap);
                        preview = asset != null ? AssetPreview.GetAssetPreview(asset) : null;
                    }
                    gridPreviewCache[entry.fullPath] = preview;
                }
                if (preview != null)
                {
                    GUI.DrawTexture(iconRect, preview, ScaleMode.ScaleToFit);
                    drewPreview = true;
                }
            }
            if (!drewPreview)
            {
                GUIContent icon = entry.isDirectory ? GetFolderIcon(entry.fullPath) : GetFileIcon(entry.name);
                GUI.Label(new Rect(iconRect.x + 4, iconRect.y + 4, 32, 32), icon);
            }

            // Label
            Rect labelRect = new Rect(itemRect.x + 2, itemRect.y + 44, GRID_ITEM_SIZE - 4, 24);
            if (B.renamingPath == entry.fullPath)
                DrawInlineRenameRect(entry.fullPath, labelRect, window);
            else
                GUI.Label(labelRect, entry.name, gridLabelStyle);

            // Click
            if (Event.current.type == EventType.MouseDown && itemRect.Contains(Event.current.mousePosition))
            {
                if (Event.current.clickCount == 2)
                {
                    if (entry.isDirectory) B.NavigateTo(panelIndex, entry.fullPath);
                    else B.OpenAssetAtPath(entry.fullPath);
                    Event.current.Use(); window.Repaint();
                }
                else if (Event.current.button == 1)
                {
                    if (entry.isDirectory) ShowFolderContextMenu(entry.fullPath, panelIndex, window);
                    else ShowFileContextMenu(entry.fullPath, window);
                    Event.current.Use();
                }
                else if (Event.current.button == 0)
                {
                    B.HandleSelection(entry.fullPath, panelIndex, Event.current.control || Event.current.command, Event.current.shift);
                    BeginDragCandidate(entry.fullPath, panelIndex);
                    B.PingAssetAtPath(entry.fullPath);
                    Event.current.Use();
                }
            }
        }

        #endregion

        #region Item Interaction

        private void HandleItemClick(Rect rect, string fullPath, int panelIndex, bool isFolder, EditorWindow window)
        {
            if (Event.current.type != EventType.MouseDown || !rect.Contains(Event.current.mousePosition)) return;

            if (Event.current.clickCount == 2)
            {
                if (isFolder) B.NavigateTo(panelIndex, fullPath);
                else B.OpenAssetAtPath(fullPath);
                Event.current.Use(); window.Repaint();
            }
            else if (Event.current.button == 1)
            {
                if (isFolder) ShowFolderContextMenu(fullPath, panelIndex, window);
                else ShowFileContextMenu(fullPath, window);
                Event.current.Use();
            }
            else if (Event.current.button == 0)
            {
                B.HandleSelection(fullPath, panelIndex, Event.current.control || Event.current.command, Event.current.shift);
                BeginDragCandidate(fullPath, panelIndex);
                B.PingAssetAtPath(fullPath);
                Event.current.Use();
            }
        }

        private void DrawItemBackground(Rect row, string fullPath, int panelIndex)
        {
            if (row.width < 1) return;
            if (listRowIndex % 2 == 1)
                EditorGUI.DrawRect(row, altRowColor);
            listRowIndex++;

            bool isSelected = B.GetPanelSelection(panelIndex).Contains(fullPath);
            bool isHovered = row.Contains(Event.current.mousePosition);
            if (isSelected) EditorGUI.DrawRect(row, selectColor);
            else if (isHovered) EditorGUI.DrawRect(row, hoverColor);
        }

        #endregion

        #region Inline Rename

        private void DrawInlineRename(string fullPath, EditorWindow window)
        {
            GUI.SetNextControlName("RenameField");
            B.renameText = EditorGUILayout.TextField(B.renameText, GUILayout.ExpandWidth(true));
            if (B.renameFocusNeeded) { EditorGUI.FocusTextInControl("RenameField"); B.renameFocusNeeded = false; }

            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                { B.CommitRename(fullPath); Event.current.Use(); window.Repaint(); }
                else if (Event.current.keyCode == KeyCode.Escape)
                { B.renamingPath = null; Event.current.Use(); window.Repaint(); }
            }
        }

        private void DrawInlineRenameRect(string fullPath, Rect rect, EditorWindow window)
        {
            GUI.SetNextControlName("RenameFieldGrid");
            B.renameText = EditorGUI.TextField(rect, B.renameText);
            if (B.renameFocusNeeded) { EditorGUI.FocusTextInControl("RenameFieldGrid"); B.renameFocusNeeded = false; }

            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                { B.CommitRename(fullPath); Event.current.Use(); window.Repaint(); }
                else if (Event.current.keyCode == KeyCode.Escape)
                { B.renamingPath = null; Event.current.Use(); window.Repaint(); }
            }
        }

        #endregion

        #region Keyboard Navigation

        public void HandleKeyboardNavigation(EditorWindow window)
        {
            if (Event.current.type != EventType.KeyDown) return;
            if (B.activePanelIndex < 0 || B.activePanelIndex >= B.browsePaths.Count) return;
            if (B.renamingPath != null) return;

            string browsePath = B.browsePaths[B.activePanelIndex];
            var entries = B.GetSortedEntries(browsePath, B.activePanelIndex);
            if (entries.Count == 0) return;

            switch (Event.current.keyCode)
            {
                case KeyCode.UpArrow:
                    MoveSelection(entries, -1); Event.current.Use(); window.Repaint(); break;
                case KeyCode.DownArrow:
                    MoveSelection(entries, 1); Event.current.Use(); window.Repaint(); break;
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    if (B.selectedPaths.Count == 1)
                    {
                        string sel = B.selectedPaths.First();
                        if (Directory.Exists(sel)) B.NavigateTo(B.activePanelIndex, sel);
                        else B.OpenAssetAtPath(sel);
                        Event.current.Use(); window.Repaint();
                    }
                    break;
                case KeyCode.Backspace:
                case KeyCode.Delete:
                    if (B.selectedPaths.Count > 0 && !(Event.current.control || Event.current.command))
                    { B.DeleteSelected(); Event.current.Use(); window.Repaint(); }
                    break;
                case KeyCode.F2:
                    if (B.selectedPaths.Count == 1)
                    { B.StartRename(B.selectedPaths.First()); Event.current.Use(); window.Repaint(); }
                    break;
                case KeyCode.LeftArrow:
                    if (Event.current.alt) { B.NavigateBack(B.activePanelIndex); Event.current.Use(); window.Repaint(); }
                    break;
                case KeyCode.RightArrow:
                    if (Event.current.alt) { B.NavigateForward(B.activePanelIndex); Event.current.Use(); window.Repaint(); }
                    break;
            }
        }

        private void MoveSelection(List<ProjectTabsBackend.FileEntry> entries, int direction)
        {
            int currentIdx = -1;
            if (B.selectedPaths.Count > 0)
            {
                string last = B.lastClickedPath ?? B.selectedPaths.First();
                for (int i = 0; i < entries.Count; i++)
                    if (entries[i].fullPath == last) { currentIdx = i; break; }
            }
            int nextIdx = Mathf.Clamp(currentIdx + direction, 0, entries.Count - 1);
            B.selectedPaths.Clear();
            B.selectedPaths.Add(entries[nextIdx].fullPath);
            B.lastClickedPath = entries[nextIdx].fullPath;
            B.SyncUnitySelection();
            B.PingAssetAtPath(entries[nextIdx].fullPath);
        }

        #endregion

        #region Drag & Drop

        private void BeginDragCandidate(string fullPath, int panelIndex)
        {
            isDragReady = true;
            dragStartPos = Event.current.mousePosition;
            dragCandidatePath = fullPath;
            crossDragSourcePanel = panelIndex;
        }

        public void HandleGlobalDrag(EditorWindow window)
        {
            if (!isDragReady) return;
            if (Event.current.type == EventType.MouseDrag)
            {
                if (Vector2.Distance(dragStartPos, Event.current.mousePosition) < DRAG_THRESHOLD) return;

                var objects = new List<Object>();
                var paths = new List<string>();
                var dragPaths = B.selectedPaths.Count > 0 ? B.selectedPaths : new HashSet<string> { dragCandidatePath };

                foreach (string sp in dragPaths)
                {
                    string ap = B.AssetPathFromAbsolute(sp);
                    if (ap == null) continue;
                    Object asset = AssetDatabase.LoadAssetAtPath<Object>(ap);
                    if (asset != null) { objects.Add(asset); paths.Add(ap); }
                }
                if (objects.Count == 0) { isDragReady = false; return; }

                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = objects.ToArray();
                DragAndDrop.paths = paths.ToArray();
                DragAndDrop.StartDrag(objects.Count == 1 ? objects[0].name : $"{objects.Count} items");
                isCrossPanelDrag = true;
                isDragReady = false;
                dragCandidatePath = null;
                Event.current.Use();
            }
            if (Event.current.type == EventType.MouseUp) { isDragReady = false; dragCandidatePath = null; }
        }

        private void HandleDropTarget(int panelIndex, EditorWindow window)
        {
            if (panelIndex >= panelRects.Count) return;
            Rect panelArea = panelRects[panelIndex];
            if (!panelArea.Contains(Event.current.mousePosition)) return;

            if (Event.current.type == EventType.DragUpdated)
            {
                // Accept any drag: internal cross-panel or external OS files
                bool hasPaths = (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0);
                bool hasObjects = (DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0);
                if (hasPaths || hasObjects || !isCrossPanelDrag)
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                else if (crossDragSourcePanel != panelIndex)
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                else
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                Event.current.Use();
            }

            if (Event.current.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                string targetFolder = B.AssetPathFromAbsolute(B.browsePaths[panelIndex]);
                bool handled = false;

                // Check DragAndDrop.paths (works for both internal and some external drags)
                if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                {
                    foreach (string srcPath in DragAndDrop.paths)
                    {
                        if (string.IsNullOrEmpty(srcPath)) continue;

                        // .unitypackage from anywhere
                        if (srcPath.EndsWith(".unitypackage", System.StringComparison.OrdinalIgnoreCase))
                        {
                            AssetDatabase.ImportPackage(srcPath, true);
                            handled = true;
                            continue;
                        }

                        // External file (absolute path not inside Assets) — try import as .unitypackage check
                        if (Path.IsPathRooted(srcPath) && !srcPath.StartsWith("Assets"))
                        {
                            // It's an OS file path, check extension
                            if (File.Exists(srcPath) && srcPath.EndsWith(".unitypackage", System.StringComparison.OrdinalIgnoreCase))
                            {
                                AssetDatabase.ImportPackage(srcPath, true);
                                handled = true;
                            }
                            continue;
                        }

                        // Internal asset cross-panel move/copy
                        if (isCrossPanelDrag && crossDragSourcePanel != panelIndex && targetFolder != null)
                        {
                            string fileName = Path.GetFileName(srcPath);
                            string destPath = AssetDatabase.GenerateUniqueAssetPath(targetFolder + "/" + fileName);
                            if (Event.current.alt) AssetDatabase.CopyAsset(srcPath, destPath);
                            else AssetDatabase.MoveAsset(srcPath, destPath);
                            handled = true;
                        }
                    }
                }

                if (handled)
                {
                    B.InvalidateCache();
                    AssetDatabase.Refresh();
                }
                isCrossPanelDrag = false;
                Event.current.Use(); window.Repaint();
            }
        }

        public void HandleCrossPanelDrop()
        {
            if (Event.current.type == EventType.DragExited)
                isCrossPanelDrag = false;
        }

        #endregion

        #region Splitter

        public void DrawSplitterVisual(Rect rect, int index)
        {
            Rect line = new Rect(rect.x + (SPLITTER_WIDTH - 1f) * 0.5f, rect.y, 1, rect.height);
            EditorGUI.DrawRect(line, separatorColor);
            bool active = activeSplitter == index;
            bool hovered = rect.Contains(Event.current.mousePosition);
            if (active || hovered)
                EditorGUI.DrawRect(new Rect(rect.x + 1, rect.y, SPLITTER_WIDTH - 2, rect.height), splitterHoverColor);
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
        }

        public void HandleSplitterInput(float aw, EditorWindow window)
        {
            if (B.rootPaths.Count < 2) return;
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                for (int i = 0; i < splitterRects.Count; i++)
                {
                    if (splitterRects[i].Contains(e.mousePosition))
                    {
                        activeSplitter = i;
                        splitterDragStartX = e.mousePosition.x;
                        splitterDragStartRatioL = B.panelRatios[i];
                        splitterDragStartRatioR = B.panelRatios[i + 1];
                        e.Use(); return;
                    }
                }
            }
            if (activeSplitter >= 0 && e.type == EventType.MouseDrag)
            {
                float d = e.mousePosition.x - splitterDragStartX;
                float rd = d / Mathf.Max(aw, 1f);
                float nL = splitterDragStartRatioL + rd, nR = splitterDragStartRatioR - rd;
                float mr = MIN_PANEL_WIDTH / Mathf.Max(aw, 1f);
                if (nL >= mr && nR >= mr)
                { B.panelRatios[activeSplitter] = nL; B.panelRatios[activeSplitter + 1] = nR; B.SaveAll(); }
                e.Use(); window.Repaint();
            }
            if (activeSplitter >= 0 && (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp))
            { activeSplitter = -1; e.Use(); }
        }

        /// <summary>Draw min-width indicator when panel is at minimum size.</summary>
        public void DrawMinWidthIndicator(Rect panelRect, float width)
        {
            if (width <= MIN_PANEL_WIDTH + 5f)
            {
                Color warnColor = EditorGUIUtility.isProSkin ? new Color(1f, 0.6f, 0.2f, 0.3f) : new Color(1f, 0.5f, 0f, 0.2f);
                EditorGUI.DrawRect(new Rect(panelRect.x, panelRect.y, 2, panelRect.height), warnColor);
                EditorGUI.DrawRect(new Rect(panelRect.xMax - 2, panelRect.y, 2, panelRect.height), warnColor);
            }
        }

        public float GetSplitterWidth() => SPLITTER_WIDTH;
        public float GetMinPanelWidth() => MIN_PANEL_WIDTH;

        #endregion

        #region Context Menus

        private void ShowBackgroundContextMenu(string folderPath, EditorWindow window)
        {
            string af = B.AssetPathFromAbsolute(folderPath);
            GenericMenu m = new GenericMenu();
            AddCreateMenuItems(m, af, folderPath, window);
            m.AddSeparator("");
            m.AddItem(new GUIContent("Show in Explorer"), false, () => EditorUtility.RevealInFinder(folderPath));
            m.AddItem(new GUIContent("Refresh"), false, () => { B.InvalidateCache(); AssetDatabase.Refresh(); window.Repaint(); });
            m.AddItem(new GUIContent("Import Package..."), false, () =>
            {
                string pkg = EditorUtility.OpenFilePanel("Import Package", "", "unitypackage");
                if (!string.IsNullOrEmpty(pkg)) AssetDatabase.ImportPackage(pkg, true);
            });
            bool isFav = B.favoritePaths.Contains(folderPath);
            m.AddItem(new GUIContent(isFav ? "Remove from Favorites" : "Add to Favorites"), false, () => { B.ToggleFavorite(folderPath); window.Repaint(); });
            if (B.selectedPaths.Count > 0)
            {
                m.AddSeparator("");
                m.AddItem(new GUIContent($"Delete Selected ({B.selectedPaths.Count})"), false, () => { B.DeleteSelected(); window.Repaint(); });
                m.AddItem(new GUIContent($"Export Selected ({B.selectedPaths.Count})..."), false, () =>
                {
                    var assetPaths = new List<string>();
                    foreach (string sp in B.selectedPaths)
                    {
                        string ap = B.AssetPathFromAbsolute(sp);
                        if (ap != null) assetPaths.Add(ap);
                    }
                    if (assetPaths.Count > 0) ExportPackage(assetPaths.ToArray());
                });
            }
            m.ShowAsContext();
        }

        private void ShowFolderContextMenu(string fullPath, int pi, EditorWindow window)
        {
            string ap = B.AssetPathFromAbsolute(fullPath);
            GenericMenu m = new GenericMenu();
            m.AddItem(new GUIContent("Open"), false, () => { B.NavigateTo(pi, fullPath); window.Repaint(); });
            m.AddSeparator("");
            AddCreateMenuItems(m, ap, fullPath, window);
            m.AddSeparator("");
            m.AddItem(new GUIContent("Show in Explorer"), false, () => EditorUtility.RevealInFinder(fullPath));
            bool isFav = B.favoritePaths.Contains(fullPath);
            m.AddItem(new GUIContent(isFav ? "Remove from Favorites" : "Add to Favorites"), false, () => { B.ToggleFavorite(fullPath); window.Repaint(); });
            if (ap != null)
            {
                m.AddItem(new GUIContent("Rename"), false, () => { B.StartRename(fullPath); window.Repaint(); });
                m.AddItem(new GUIContent("Copy Path"), false, () => EditorGUIUtility.systemCopyBuffer = ap);
                m.AddItem(new GUIContent("Delete"), false, () => { B.DeleteSingle(fullPath); window.Repaint(); });
                m.AddSeparator("");
                AddColorTagMenuItems(m, fullPath, window);
                AddQuickAccessMenuItem(m, fullPath, window);
                m.AddSeparator("");
                m.AddItem(new GUIContent("Export Package..."), false, () => ExportPackage(new string[] { ap }));
            }
            m.ShowAsContext();
        }

        private void ShowFileContextMenu(string fullPath, EditorWindow window)
        {
            string ap = B.AssetPathFromAbsolute(fullPath);
            GenericMenu m = new GenericMenu();
            m.AddItem(new GUIContent("Open"), false, () => B.OpenAssetAtPath(fullPath));
            m.AddSeparator("");
            m.AddItem(new GUIContent("Show in Explorer"), false, () => EditorUtility.RevealInFinder(fullPath));
            if (ap != null)
            {
                m.AddItem(new GUIContent("Copy Path"), false, () => EditorGUIUtility.systemCopyBuffer = ap);
                m.AddSeparator("");
                m.AddItem(new GUIContent("Rename"), false, () => { B.StartRename(fullPath); window.Repaint(); });
                m.AddItem(new GUIContent("Duplicate"), false, () => { B.DuplicateAsset(fullPath); window.Repaint(); });
                m.AddItem(new GUIContent("Delete"), false, () => { B.DeleteSingle(fullPath); window.Repaint(); });
                m.AddSeparator("");
                m.AddItem(new GUIContent("Reimport"), false, () => AssetDatabase.ImportAsset(ap));
                m.AddSeparator("");
                AddColorTagMenuItems(m, fullPath, window);
                AddQuickAccessMenuItem(m, fullPath, window);
            }
            if (B.selectedPaths.Count > 1)
            {
                m.AddSeparator("");
                m.AddItem(new GUIContent($"Delete Selected ({B.selectedPaths.Count})"), false, () => { B.DeleteSelected(); window.Repaint(); });
            }
            m.ShowAsContext();
        }

        private void AddCreateMenuItems(GenericMenu m, string af, string ff, EditorWindow window)
        {
            if (string.IsNullOrEmpty(af)) return;
            m.AddItem(new GUIContent("Create/Folder"), false, () =>
            { AssetDatabase.CreateFolder(af, "New Folder"); B.InvalidateCache(); AssetDatabase.Refresh(); window.Repaint(); });
            m.AddSeparator("Create/");
            m.AddItem(new GUIContent("Create/C# Script"), false, () =>
            { string tp = B.GetScriptTemplatePath(); if (!string.IsNullOrEmpty(tp)) ProjectWindowUtil.CreateScriptAssetFromTemplateFile(tp, "NewScript.cs"); window.Repaint(); });
            m.AddSeparator("Create/");
            m.AddItem(new GUIContent("Create/Material"), false, () =>
            { AssetDatabase.CreateAsset(new Material(Shader.Find("Standard")), AssetDatabase.GenerateUniqueAssetPath(af + "/New Material.mat")); B.InvalidateCache(); AssetDatabase.Refresh(); window.Repaint(); });
            m.AddItem(new GUIContent("Create/Animator Controller"), false, () =>
            { UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(AssetDatabase.GenerateUniqueAssetPath(af + "/New Animator.controller")); B.InvalidateCache(); AssetDatabase.Refresh(); window.Repaint(); });
            m.AddItem(new GUIContent("Create/Animation Clip"), false, () =>
            { AssetDatabase.CreateAsset(new AnimationClip(), AssetDatabase.GenerateUniqueAssetPath(af + "/New Animation.anim")); B.InvalidateCache(); AssetDatabase.Refresh(); window.Repaint(); });
            m.AddSeparator("Create/");
            m.AddItem(new GUIContent("Create/Scene"), false, () =>
            {
                var s = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.DefaultGameObjects, UnityEditor.SceneManagement.NewSceneMode.Additive);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s, AssetDatabase.GenerateUniqueAssetPath(af + "/New Scene.unity"));
                UnityEditor.SceneManagement.EditorSceneManager.CloseScene(s, true); B.InvalidateCache(); AssetDatabase.Refresh(); window.Repaint();
            });
            m.AddItem(new GUIContent("Create/Prefab"), false, () =>
            {
                var go = new GameObject("New Prefab");
                PrefabUtility.SaveAsPrefabAsset(go, AssetDatabase.GenerateUniqueAssetPath(af + "/New Prefab.prefab"));
                Object.DestroyImmediate(go); B.InvalidateCache(); AssetDatabase.Refresh(); window.Repaint();
            });
            m.AddSeparator("Create/");
            m.AddItem(new GUIContent("Create/Text File"), false, () =>
            { File.WriteAllText(Path.Combine(ff, "New Text.txt"), ""); B.InvalidateCache(); AssetDatabase.Refresh(); window.Repaint(); });
        }

        #endregion

        #region Quick Access Sidebar

        public void DrawQuickAccessSidebar(EditorWindow window)
        {
            if (!showQuickAccess) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(130));
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Quick Access", EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("✕", EditorStyles.miniLabel, GUILayout.Width(14)))
                showQuickAccess = false;
            EditorGUILayout.EndHorizontal();

            quickAccessScroll = EditorGUILayout.BeginScrollView(quickAccessScroll);
            foreach (string path in B.quickAccessPaths.ToArray())
            {
                string label = Path.GetFileName(path);
                if (string.IsNullOrEmpty(label)) label = "Assets";
                bool isDir = Directory.Exists(path);

                EditorGUILayout.BeginHorizontal();
                GUIContent icon = isDir ? GetFolderIcon(path) : GetFileIcon(label);
                GUILayout.Label(icon, GUILayout.Width(14), GUILayout.Height(16));
                if (GUILayout.Button(new GUIContent(Truncate(label, 14), path), EditorStyles.miniLabel, GUILayout.ExpandWidth(true)))
                {
                    int pi = B.activePanelIndex >= 0 && B.activePanelIndex < B.rootPaths.Count ? B.activePanelIndex : 0;
                    if (isDir) B.NavigateTo(pi, path);
                    else B.OpenAssetAtPath(path);
                    window.Repaint();
                }
                if (GUILayout.Button("✕", EditorStyles.miniLabel, GUILayout.Width(12)))
                { B.RemoveQuickAccess(path); window.Repaint(); }
                EditorGUILayout.EndHorizontal();
            }
            if (B.quickAccessPaths.Count == 0)
                GUILayout.Label("Right-click → Pin", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Folder Drop Target (drag into subfolder)

        private void HandleFolderDropTarget(Rect folderRow, string folderPath, EditorWindow window)
        {
            if (!folderRow.Contains(Event.current.mousePosition)) return;

            if (Event.current.type == EventType.DragUpdated)
            {
                string targetAp = B.AssetPathFromAbsolute(folderPath);
                if (targetAp != null && DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                {
                    // Don't allow drop into self
                    bool selfDrag = false;
                    foreach (string p in DragAndDrop.paths)
                        if (p == targetAp) { selfDrag = true; break; }
                    if (!selfDrag)
                    {
                        DragAndDrop.visualMode = Event.current.alt ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Move;
                        Event.current.Use();
                    }
                }
            }

            if (Event.current.type == EventType.DragPerform)
            {
                string targetAp = B.AssetPathFromAbsolute(folderPath);
                if (targetAp != null && DragAndDrop.paths != null)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (string srcPath in DragAndDrop.paths)
                    {
                        string fileName = Path.GetFileName(srcPath);
                        string destPath = AssetDatabase.GenerateUniqueAssetPath(targetAp + "/" + fileName);
                        if (Event.current.alt) AssetDatabase.CopyAsset(srcPath, destPath);
                        else AssetDatabase.MoveAsset(srcPath, destPath);
                    }
                    B.InvalidateCache();
                    AssetDatabase.Refresh();
                    Event.current.Use();
                    window.Repaint();
                }
            }
        }

        #endregion

        #region Hover & File Preview Tooltip

        /// <summary>Returns true if a hover tooltip is active or pending, requiring repaint.</summary>
        public bool NeedsHoverRepaint()
        {
            return !string.IsNullOrEmpty(hoverPath);
        }

        private void TrackHover(Rect rect, string path)
        {
            if (rect.Contains(Event.current.mousePosition))
            {
                if (hoverPath != path) { hoverPath = path; hoverStartTime = EditorApplication.timeSinceStartup; }
            }
            else if (hoverPath == path)
            {
                hoverPath = null;
            }
        }

        public void DrawFilePreviewTooltip(EditorWindow window)
        {
            if (string.IsNullOrEmpty(hoverPath)) return;
            if (EditorApplication.timeSinceStartup - hoverStartTime < TOOLTIP_DELAY) return;
            if (Directory.Exists(hoverPath)) return; // only for files

            string ap = B.AssetPathFromAbsolute(hoverPath);
            if (ap == null) return;

            Vector2 mouse = Event.current.mousePosition;
            float tooltipW = 180, tooltipH = 80;
            Rect tipRect = new Rect(mouse.x + 16, mouse.y + 8, tooltipW, tooltipH);

            // Keep on screen
            if (tipRect.xMax > window.position.width) tipRect.x = mouse.x - tooltipW - 8;
            if (tipRect.yMax > window.position.height) tipRect.y = mouse.y - tooltipH - 8;

            // Background
            Color bg = EditorGUIUtility.isProSkin ? new Color(0.18f, 0.18f, 0.18f, 0.95f) : new Color(0.95f, 0.95f, 0.95f, 0.95f);
            EditorGUI.DrawRect(tipRect, bg);
            Color border = EditorGUIUtility.isProSkin ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.7f, 0.7f, 0.7f);
            EditorGUI.DrawRect(new Rect(tipRect.x, tipRect.y, tipRect.width, 1), border);
            EditorGUI.DrawRect(new Rect(tipRect.x, tipRect.yMax - 1, tipRect.width, 1), border);
            EditorGUI.DrawRect(new Rect(tipRect.x, tipRect.y, 1, tipRect.height), border);
            EditorGUI.DrawRect(new Rect(tipRect.xMax - 1, tipRect.y, 1, tipRect.height), border);

            // Preview thumbnail
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(ap);
            Texture2D preview = asset != null ? AssetPreview.GetAssetPreview(asset) : null;
            float infoX = tipRect.x + 4;
            if (preview != null)
            {
                GUI.DrawTexture(new Rect(tipRect.x + 4, tipRect.y + 4, 48, 48), preview, ScaleMode.ScaleToFit);
                infoX = tipRect.x + 56;
            }

            // Info
            string fileName = Path.GetFileName(hoverPath);
            string ext = Path.GetExtension(hoverPath).ToUpper();
            long size = B.GetFileSizeCached(hoverPath);
            string sizeStr = ProjectTabsBackend.FormatFileSize(size);

            GUI.Label(new Rect(infoX, tipRect.y + 4, tooltipW - 60, 14), fileName, EditorStyles.miniBoldLabel);
            GUI.Label(new Rect(infoX, tipRect.y + 20, tooltipW - 60, 14), ext.TrimStart('.') + " file", EditorStyles.miniLabel);
            GUI.Label(new Rect(infoX, tipRect.y + 36, tooltipW - 60, 14), sizeStr, EditorStyles.miniLabel);

            try
            {
                string modified = File.GetLastWriteTime(hoverPath).ToString("yyyy-MM-dd HH:mm");
                GUI.Label(new Rect(infoX, tipRect.y + 52, tooltipW - 60, 14), modified, EditorStyles.miniLabel);
            }
            catch { }

            window.Repaint();
        }

        #endregion

        #region Color Tag Context Menu Helpers

        public void AddColorTagMenuItems(GenericMenu menu, string path, EditorWindow window)
        {
            int current = B.GetColorTag(path);
            for (int i = 0; i < ProjectTabsBackend.colorTagNames.Length; i++)
            {
                int idx = i;
                menu.AddItem(new GUIContent("Color Tag/" + ProjectTabsBackend.colorTagNames[i]), current == i,
                    () => { B.SetColorTag(path, idx); window.Repaint(); });
            }
        }

        public void AddQuickAccessMenuItem(GenericMenu menu, string path, EditorWindow window)
        {
            bool pinned = B.quickAccessPaths.Contains(path);
            if (pinned)
                menu.AddItem(new GUIContent("Unpin from Quick Access"), false, () => { B.RemoveQuickAccess(path); window.Repaint(); });
            else
                menu.AddItem(new GUIContent("Pin to Quick Access"), false, () => { B.AddQuickAccess(path); window.Repaint(); });
        }

        #endregion

        #region Export Package

        private void ExportPackage(string[] assetPaths)
        {
            if (assetPaths == null || assetPaths.Length == 0) return;
            string defaultName = assetPaths.Length == 1 ? Path.GetFileNameWithoutExtension(assetPaths[0]) : "ExportedPackage";
            string savePath = EditorUtility.SaveFilePanel("Export Package", "", defaultName, "unitypackage");
            if (string.IsNullOrEmpty(savePath)) return;
            AssetDatabase.ExportPackage(assetPaths, savePath, ExportPackageOptions.Recurse | ExportPackageOptions.Interactive);
        }

        #endregion

        #region File Icons

        private GUIContent GetFolderIcon(string folderPath)
        {
            B.GetDirectoryContents(folderPath, out string[] dirs, out string[] files);
            bool hasContent = dirs.Length > 0;
            if (!hasContent)
            {
                for (int i = 0; i < files.Length; i++)
                {
                    if (!files[i].EndsWith(".meta")) { hasContent = true; break; }
                }
            }
            return EditorGUIUtility.IconContent(hasContent ? "d_Folder Icon" : "d_FolderEmpty Icon");
        }

        public GUIContent GetFileIcon(string fn)
        {
            string e = Path.GetExtension(fn).ToLower();
            switch (e)
            {
                case ".cs": return EditorGUIUtility.IconContent("cs Script Icon");
                case ".shader": return EditorGUIUtility.IconContent("Shader Icon");
                case ".mat": return EditorGUIUtility.IconContent("Material Icon");
                case ".prefab": return EditorGUIUtility.IconContent("Prefab Icon");
                case ".unity": return EditorGUIUtility.IconContent("SceneAsset Icon");
                case ".asset": return EditorGUIUtility.IconContent("ScriptableObject Icon");
                case ".png": case ".jpg": case ".jpeg": case ".tga": case ".psd":
                    return EditorGUIUtility.IconContent("Texture Icon");
                case ".anim": return EditorGUIUtility.IconContent("AnimationClip Icon");
                case ".controller": return EditorGUIUtility.IconContent("AnimatorController Icon");
                case ".mp3": case ".wav": case ".ogg":
                    return EditorGUIUtility.IconContent("AudioClip Icon");
                case ".ttf": case ".otf": return EditorGUIUtility.IconContent("Font Icon");
                case ".txt": case ".md": case ".json": case ".xml":
                    return EditorGUIUtility.IconContent("TextAsset Icon");
                default: return EditorGUIUtility.IconContent("DefaultAsset Icon");
            }
        }

        #endregion
    }
}
