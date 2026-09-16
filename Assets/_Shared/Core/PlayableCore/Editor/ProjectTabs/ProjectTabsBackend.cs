using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amanotes.Core;

namespace Amanotes.Core.Editor
{
    /// <summary>
    /// Backend: data model, directory cache, persistence, navigation history,
    /// selection state, file operations, and path utilities for Project Tabs.
    /// </summary>
    public class ProjectTabsBackend
    {
        #region Constants

        public const string PREFS_KEY = "PTW_Roots";
        public const string PREFS_BROWSE = "PTW_Browse";
        public const string PREFS_RATIOS = "PTW_Ratios";
        public const string PREFS_FAVORITES = "PTW_Favorites";
        public const string PREFS_COLOR_TAGS = "PTW_ColorTags";
        public const string PREFS_QUICK_ACCESS = "PTW_QuickAccess";
        public const string PREFS_LOCK_FIRST = "PTW_LockFirst";
        public const int NAV_HISTORY_MAX = 32;
        public const double DIR_CACHE_TTL = 5.0;

        #endregion

        #region Enums

        public enum SortMode { Name, Type, DateModified }
        public enum ViewMode { List, Grid }
        public enum TypeFilter { All, Scripts, Textures, Prefabs, Scenes, Audio, Materials }

        #endregion

        #region Panel Data

        public List<string> rootPaths = new List<string>(8);
        public List<string> browsePaths = new List<string>(8);
        public List<Vector2> scrollPositions = new List<Vector2>(8);
        public List<string> searchFilters = new List<string>(8);
        public List<float> panelRatios = new List<float>(8);
        public List<SortMode> sortModes = new List<SortMode>(8);
        public List<ViewMode> viewModes = new List<ViewMode>(8);
        public List<TypeFilter> typeFilters = new List<TypeFilter>(8);
        public List<List<string>> navHistoryBack = new List<List<string>>(8);
        public List<List<string>> navHistoryForward = new List<List<string>>(8);

        #endregion

        #region Shared State

        public HashSet<string> favoritePaths = new HashSet<string>();
        public HashSet<string> selectedPaths = new HashSet<string>();
        public List<HashSet<string>> expandedFolders = new List<HashSet<string>>(8);
        public string lastClickedPath;

        // Per-panel selections
        public List<HashSet<string>> panelSelections = new List<HashSet<string>>(8);

        /// <summary>Get selected paths for a specific panel.</summary>
        public HashSet<string> GetPanelSelection(int panelIndex)
        {
            if (panelIndex >= 0 && panelIndex < panelSelections.Count)
                return panelSelections[panelIndex];
            return selectedPaths;
        }
        public int activePanelIndex = -1;
        public string renamingPath;
        public string renameText;
        public bool renameFocusNeeded;

        // Color tags: path -> color index (0-7)
        public Dictionary<string, int> colorTags = new Dictionary<string, int>();

        // Quick access sidebar paths
        public List<string> quickAccessPaths = new List<string>(16);

        // Compact mode hides search/filter bar
        public bool compactMode = false;

        // Lock first panel: when unlocked, first panel auto-follows Unity selection
        public bool isFirstPanelLocked = false;

        #endregion

        #region Color Tags

        public static readonly string[] colorTagNames = { "None", "Red", "Orange", "Yellow", "Green", "Blue", "Purple", "Gray" };
        public static readonly Color[] colorTagColors =
        {
            Color.clear,
            new Color(0.9f, 0.3f, 0.3f, 0.6f),
            new Color(0.9f, 0.6f, 0.2f, 0.6f),
            new Color(0.9f, 0.85f, 0.2f, 0.6f),
            new Color(0.3f, 0.8f, 0.3f, 0.6f),
            new Color(0.3f, 0.5f, 0.9f, 0.6f),
            new Color(0.7f, 0.3f, 0.9f, 0.6f),
            new Color(0.5f, 0.5f, 0.5f, 0.6f),
        };

        public void SetColorTag(string path, int colorIndex)
        {
            if (colorIndex <= 0) colorTags.Remove(path);
            else colorTags[path] = colorIndex;
            SaveAll();
        }

        public int GetColorTag(string path)
        {
            return colorTags.TryGetValue(path, out int c) ? c : 0;
        }

        #endregion

        #region Quick Access

        public void AddQuickAccess(string path)
        {
            if (!quickAccessPaths.Contains(path))
            { quickAccessPaths.Add(path); SaveAll(); }
        }

        public void RemoveQuickAccess(string path)
        {
            if (quickAccessPaths.Remove(path)) SaveAll();
        }

        #endregion

        #region File Size Utility

        // File size cache: avoid I/O every frame
        private Dictionary<string, long> fileSizeCache = new Dictionary<string, long>();
        private double fileSizeCacheTime;
        private const double FILE_SIZE_CACHE_TTL = 5.0;

        public static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024f * 1024f):F1} MB";
            return $"{bytes / (1024f * 1024f * 1024f):F2} GB";
        }

        public long GetFileSizeCached(string path)
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - fileSizeCacheTime > FILE_SIZE_CACHE_TTL)
            {
                fileSizeCache.Clear();
                fileSizeCacheTime = now;
            }
            if (fileSizeCache.TryGetValue(path, out long cached)) return cached;
            long size;
            try { size = new FileInfo(path).Length; } catch { size = 0; }
            fileSizeCache[path] = size;
            return size;
        }

        public static long GetFileSize(string path)
        {
            try { return new FileInfo(path).Length; } catch { return 0; }
        }

        #endregion

        #region Directory Cache

        public struct CachedDirectoryListing
        {
            public double timestamp;
            public string[] directories;
            public string[] files;
        }

        private Dictionary<string, CachedDirectoryListing> dirCache = new Dictionary<string, CachedDirectoryListing>();

        public void GetDirectoryContents(string path, out string[] dirs, out string[] files)
        {
            double now = EditorApplication.timeSinceStartup;
            if (dirCache.TryGetValue(path, out var cached) && (now - cached.timestamp) < DIR_CACHE_TTL)
            {
                dirs = cached.directories;
                files = cached.files;
                return;
            }
            try { dirs = Directory.GetDirectories(path); } catch { dirs = new string[0]; }
            try { files = Directory.GetFiles(path); } catch { files = new string[0]; }
            dirCache[path] = new CachedDirectoryListing { timestamp = now, directories = dirs, files = files };
        }

        public void InvalidateCache(string path = null)
        {
            if (path == null) { dirCache.Clear(); fileSizeCache.Clear(); }
            else dirCache.Remove(path);
            cachedEntriesPerPanel.Clear();
        }

        #endregion

        #region File Entry & Sorting

        // Per-frame cache for GetSortedEntries to avoid redundant calls within same OnGUI
        private int cachedEntriesFrame = -1;
        private Dictionary<int, List<FileEntry>> cachedEntriesPerPanel = new Dictionary<int, List<FileEntry>>();

        /// <summary>Clear per-frame cache. Call once at start of OnGUI.</summary>
        public void BeginFrame()
        {
            int frame = Time.frameCount;
            if (frame != cachedEntriesFrame)
            {
                cachedEntriesFrame = frame;
                cachedEntriesPerPanel.Clear();
            }
        }

        public struct FileEntry
        {
            public string fullPath, name;
            public bool isDirectory;
        }

        private static readonly Dictionary<TypeFilter, HashSet<string>> typeFilterExtensions = new Dictionary<TypeFilter, HashSet<string>>
        {
            { TypeFilter.Scripts, new HashSet<string> { ".cs", ".shader", ".compute", ".cginc", ".hlsl" } },
            { TypeFilter.Textures, new HashSet<string> { ".png", ".jpg", ".jpeg", ".tga", ".psd", ".bmp", ".gif", ".tif", ".tiff" } },
            { TypeFilter.Prefabs, new HashSet<string> { ".prefab" } },
            { TypeFilter.Scenes, new HashSet<string> { ".unity" } },
            { TypeFilter.Audio, new HashSet<string> { ".mp3", ".wav", ".ogg", ".aiff", ".flac" } },
            { TypeFilter.Materials, new HashSet<string> { ".mat", ".shader" } },
        };

        public bool PassesTypeFilter(string fileName, TypeFilter filter)
        {
            if (filter == TypeFilter.All) return true;
            if (!typeFilterExtensions.TryGetValue(filter, out var exts)) return true;
            string ext = Path.GetExtension(fileName).ToLower();
            return exts.Contains(ext);
        }

        public List<FileEntry> GetSortedEntries(string browsePath, int panelIndex)
        {
            // Return cached result if already computed this frame for this panel
            if (cachedEntriesPerPanel.TryGetValue(panelIndex, out var cached))
                return cached;

            var entries = new List<FileEntry>(64);
            string filter = searchFilters[panelIndex];
            bool hasFilter = !string.IsNullOrEmpty(filter);
            string fl = hasFilter ? filter.ToLower() : "";
            TypeFilter tf = typeFilters[panelIndex];

            GetDirectoryContents(browsePath, out string[] dirs, out string[] files);

            for (int i = 0; i < dirs.Length; i++)
            {
                string n = Path.GetFileName(dirs[i]);
                if (n.StartsWith(".")) continue;
                if (hasFilter && !n.ToLower().Contains(fl)) continue;
                entries.Add(new FileEntry { fullPath = dirs[i], name = n, isDirectory = true });
            }
            for (int i = 0; i < files.Length; i++)
            {
                if (files[i].EndsWith(".meta")) continue;
                string n = Path.GetFileName(files[i]);
                if (n.StartsWith(".")) continue;
                if (hasFilter && !n.ToLower().Contains(fl)) continue;
                if (!PassesTypeFilter(n, tf)) continue;
                entries.Add(new FileEntry { fullPath = files[i], name = n, isDirectory = false });
            }

            SortMode sm = sortModes[panelIndex];
            if (sm == SortMode.DateModified)
            {
                // Use cached file write times to avoid I/O every frame
                entries.Sort((a, b) =>
                {
                    if (a.isDirectory != b.isDirectory) return a.isDirectory ? -1 : 1;
                    long ta, tb;
                    try { ta = File.GetLastWriteTimeUtc(a.fullPath).Ticks; } catch { ta = 0; }
                    try { tb = File.GetLastWriteTimeUtc(b.fullPath).Ticks; } catch { tb = 0; }
                    return tb.CompareTo(ta);
                });
            }
            else
            {
                entries.Sort((a, b) =>
                {
                    if (a.isDirectory != b.isDirectory) return a.isDirectory ? -1 : 1;
                    if (sm == SortMode.Type)
                    {
                        int ext = string.Compare(Path.GetExtension(a.name), Path.GetExtension(b.name), true);
                        return ext != 0 ? ext : string.Compare(a.name, b.name, true);
                    }
                    return string.Compare(a.name, b.name, true);
                });
            }
            cachedEntriesPerPanel[panelIndex] = entries;
            return entries;
        }

        #endregion

        #region Navigation

        /// <summary>Navigate to path, push current to back history. Returns true if navigated.</summary>
        public bool NavigateTo(int panelIndex, string path)
        {
            EnsureListSize(panelIndex);
            try { path = Path.GetFullPath(path); } catch { return false; }
            if (!Directory.Exists(path)) return false;

            string current = browsePaths[panelIndex];
            if (current == path) return false;

            navHistoryBack[panelIndex].Add(current);
            if (navHistoryBack[panelIndex].Count > NAV_HISTORY_MAX)
                navHistoryBack[panelIndex].RemoveAt(0);
            navHistoryForward[panelIndex].Clear();

            browsePaths[panelIndex] = path;
            scrollPositions[panelIndex] = Vector2.zero;
            searchFilters[panelIndex] = "";
            selectedPaths.Clear();
            renamingPath = null;
            InvalidateCache(path);
            SaveAll();
            return true;
        }

        public bool NavigateBack(int panelIndex)
        {
            EnsureListSize(panelIndex);
            var back = navHistoryBack[panelIndex];
            if (back.Count == 0) return false;

            string target = back[back.Count - 1];
            back.RemoveAt(back.Count - 1);
            navHistoryForward[panelIndex].Add(browsePaths[panelIndex]);
            if (navHistoryForward[panelIndex].Count > NAV_HISTORY_MAX)
                navHistoryForward[panelIndex].RemoveAt(0);

            browsePaths[panelIndex] = target;
            scrollPositions[panelIndex] = Vector2.zero;
            searchFilters[panelIndex] = "";
            selectedPaths.Clear();
            renamingPath = null;
            SaveAll();
            return true;
        }

        public bool NavigateForward(int panelIndex)
        {
            EnsureListSize(panelIndex);
            var fwd = navHistoryForward[panelIndex];
            if (fwd.Count == 0) return false;

            string target = fwd[fwd.Count - 1];
            fwd.RemoveAt(fwd.Count - 1);
            navHistoryBack[panelIndex].Add(browsePaths[panelIndex]);
            if (navHistoryBack[panelIndex].Count > NAV_HISTORY_MAX)
                navHistoryBack[panelIndex].RemoveAt(0);

            browsePaths[panelIndex] = target;
            scrollPositions[panelIndex] = Vector2.zero;
            searchFilters[panelIndex] = "";
            selectedPaths.Clear();
            renamingPath = null;
            SaveAll();
            return true;
        }

        #endregion

        #region Selection

        public void HandleSelection(string path, int panelIndex, bool ctrl, bool shift)
        {
            if (panelIndex >= 0) activePanelIndex = panelIndex;
            EnsureListSize(panelIndex);
            var sel = panelSelections[panelIndex];

            if (ctrl)
            {
                if (sel.Contains(path)) sel.Remove(path);
                else sel.Add(path);
            }
            else if (shift && lastClickedPath != null)
            {
                if (activePanelIndex >= 0 && activePanelIndex < browsePaths.Count)
                {
                    var entries = GetSortedEntries(browsePaths[activePanelIndex], activePanelIndex);
                    int idxA = -1, idxB = -1;
                    for (int i = 0; i < entries.Count; i++)
                    {
                        if (entries[i].fullPath == lastClickedPath) idxA = i;
                        if (entries[i].fullPath == path) idxB = i;
                    }
                    if (idxA >= 0 && idxB >= 0)
                    {
                        int from = Mathf.Min(idxA, idxB);
                        int to = Mathf.Max(idxA, idxB);
                        for (int i = from; i <= to; i++)
                            sel.Add(entries[i].fullPath);
                    }
                    else sel.Add(path);
                }
                else sel.Add(path);
            }
            else
            {
                sel.Clear();
                sel.Add(path);
            }
            lastClickedPath = path;
            selectedPaths.Clear();
            foreach (var s in sel) selectedPaths.Add(s);
            SyncUnitySelection();
        }

        // Flag to suppress external selection callback during internal selection sync
        public bool suppressExternalSelectionCallback;

        public void SyncUnitySelection()
        {
            suppressExternalSelectionCallback = true;
            var objects = new List<Object>(selectedPaths.Count);
            foreach (string sp in selectedPaths)
            {
                string ap = AssetPathFromAbsolute(sp);
                if (ap != null)
                {
                    Object o = AssetDatabase.LoadAssetAtPath<Object>(ap);
                    if (o != null) objects.Add(o);
                }
            }
            Selection.objects = objects.ToArray();
            // Delay reset so the callback fires first
            EditorApplication.delayCall += () => suppressExternalSelectionCallback = false;
        }

        public void SelectAll(int panelIndex)
        {
            if (panelIndex < 0 || panelIndex >= browsePaths.Count) return;
            EnsureListSize(panelIndex);
            var sel = panelSelections[panelIndex];
            var entries = GetSortedEntries(browsePaths[panelIndex], panelIndex);
            sel.Clear();
            for (int i = 0; i < entries.Count; i++) sel.Add(entries[i].fullPath);
            selectedPaths.Clear();
            foreach (var s in sel) selectedPaths.Add(s);
            SyncUnitySelection();
        }

        public void ClearSelection()
        {
            selectedPaths.Clear();
            if (activePanelIndex >= 0 && activePanelIndex < panelSelections.Count)
                panelSelections[activePanelIndex].Clear();
            renamingPath = null;
            Selection.objects = new Object[0];
        }

        #endregion

        #region File Operations

        public void StartRename(string path)
        {
            renamingPath = path;
            renameText = Path.GetFileName(path);
            renameFocusNeeded = true;
        }

        public bool CommitRename(string fullPath)
        {
            if (string.IsNullOrEmpty(renameText) || renameText == Path.GetFileName(fullPath))
            { renamingPath = null; return false; }

            string assetPath = AssetPathFromAbsolute(fullPath);
            if (string.IsNullOrEmpty(assetPath)) { renamingPath = null; return false; }

            string err = AssetDatabase.RenameAsset(assetPath, renameText);
            if (!string.IsNullOrEmpty(err)) SDebug.LogWarning($"[ProjectTabs] Rename failed: {err}");

            renamingPath = null;
            InvalidateCache(Directory.GetParent(fullPath)?.FullName);
            AssetDatabase.Refresh();
            return true;
        }

        public void DeleteSelected()
        {
            if (selectedPaths.Count == 0) return;
            if (!EditorUtility.DisplayDialog("Delete", $"Delete {selectedPaths.Count} selected items?", "Delete", "Cancel")) return;
            foreach (string sp in selectedPaths.ToArray())
            {
                string ap = AssetPathFromAbsolute(sp);
                if (ap != null) AssetDatabase.DeleteAsset(ap);
            }
            selectedPaths.Clear();
            InvalidateCache();
            AssetDatabase.Refresh();
        }

        public void DeleteSingle(string fullPath)
        {
            string ap = AssetPathFromAbsolute(fullPath);
            if (ap == null) return;
            if (!EditorUtility.DisplayDialog("Delete", $"Delete '{Path.GetFileName(fullPath)}'?", "Delete", "Cancel")) return;
            AssetDatabase.DeleteAsset(ap);
            InvalidateCache();
            AssetDatabase.Refresh();
        }

        public void DuplicateAsset(string fullPath)
        {
            string ap = AssetPathFromAbsolute(fullPath);
            if (ap == null) return;
            AssetDatabase.CopyAsset(ap, AssetDatabase.GenerateUniqueAssetPath(ap));
            InvalidateCache();
            AssetDatabase.Refresh();
        }

        public void ToggleFavorite(string path)
        {
            if (favoritePaths.Contains(path)) favoritePaths.Remove(path);
            else favoritePaths.Add(path);
            SaveAll();
        }

        public void PingAssetAtPath(string fp)
        {
            string ap = AssetPathFromAbsolute(fp);
            if (ap == null) return;
            Object o = AssetDatabase.LoadAssetAtPath<Object>(ap);
            if (o != null) EditorGUIUtility.PingObject(o);
        }

        public void OpenAssetAtPath(string fp)
        {
            string ap = AssetPathFromAbsolute(fp);
            if (ap == null) return;
            try
            {
                if (Path.GetExtension(fp).ToLower() == ".unity")
                {
                    if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ap);
                    return;
                }
            }
            catch { }
            Object o = AssetDatabase.LoadAssetAtPath<Object>(ap);
            if (o != null) AssetDatabase.OpenAsset(o);
        }

        public string GetScriptTemplatePath()
        {
            try
            {
                string ep = EditorApplication.applicationContentsPath;
                string tp = Path.Combine(ep, "Resources/ScriptTemplates/81-C# Script-NewBehaviourScript.cs.txt");
                if (File.Exists(tp)) return tp;
                string td = Path.Combine(ep, "Resources/ScriptTemplates");
                if (Directory.Exists(td))
                {
                    var ts = Directory.GetFiles(td, "*C# Script*");
                    if (ts.Length > 0) return ts[0];
                }
            }
            catch { }
            return "";
        }

        #endregion

        #region Panel Management

        public void AddPanel(string path)
        {
            string n;
            try { n = Path.GetFullPath(path); } catch { return; }
            rootPaths.Add(n); browsePaths.Add(n); scrollPositions.Add(Vector2.zero);
            searchFilters.Add(""); sortModes.Add(SortMode.Name); viewModes.Add(ViewMode.List);
            typeFilters.Add(TypeFilter.All);
            navHistoryBack.Add(new List<string>(NAV_HISTORY_MAX));
            navHistoryForward.Add(new List<string>(NAV_HISTORY_MAX));
            expandedFolders.Add(new HashSet<string>());
            panelSelections.Add(new HashSet<string>());
            float eq = 1f / rootPaths.Count;
            panelRatios.Clear();
            for (int i = 0; i < rootPaths.Count; i++) panelRatios.Add(eq);
            SaveAll();
        }

        public void RemovePanel(int i)
        {
            if (i < 0 || i >= rootPaths.Count) return;
            rootPaths.RemoveAt(i); browsePaths.RemoveAt(i); scrollPositions.RemoveAt(i);
            searchFilters.RemoveAt(i); panelRatios.RemoveAt(i); sortModes.RemoveAt(i);
            viewModes.RemoveAt(i); typeFilters.RemoveAt(i);
            navHistoryBack.RemoveAt(i); navHistoryForward.RemoveAt(i);
            expandedFolders.RemoveAt(i);
            panelSelections.RemoveAt(i);
            if (panelRatios.Count > 0)
            {
                float s = panelRatios.Sum();
                if (s > 0) for (int j = 0; j < panelRatios.Count; j++) panelRatios[j] /= s;
            }
            SaveAll();
        }

        public void CloseOtherTabs(int keepIndex)
        {
            string root = rootPaths[keepIndex]; string browse = browsePaths[keepIndex];
            SortMode sm = sortModes[keepIndex]; ViewMode vm = viewModes[keepIndex];
            TypeFilter tf = typeFilters[keepIndex];
            var hb = navHistoryBack[keepIndex]; var hf = navHistoryForward[keepIndex];
            var ef = expandedFolders[keepIndex];
            var ps = panelSelections[keepIndex];

            rootPaths.Clear(); browsePaths.Clear(); scrollPositions.Clear();
            searchFilters.Clear(); panelRatios.Clear(); sortModes.Clear();
            viewModes.Clear(); typeFilters.Clear(); navHistoryBack.Clear(); navHistoryForward.Clear();
            expandedFolders.Clear(); panelSelections.Clear();

            rootPaths.Add(root); browsePaths.Add(browse); scrollPositions.Add(Vector2.zero);
            searchFilters.Add(""); panelRatios.Add(1f); sortModes.Add(sm);
            viewModes.Add(vm); typeFilters.Add(tf); navHistoryBack.Add(hb); navHistoryForward.Add(hf);
            expandedFolders.Add(ef); panelSelections.Add(ps);
            SaveAll();
        }

        public void DuplicateTab(int index)
        {
            EnsureListSize(index);
            rootPaths.Add(rootPaths[index]); browsePaths.Add(browsePaths[index]);
            scrollPositions.Add(Vector2.zero); searchFilters.Add("");
            sortModes.Add(sortModes[index]); viewModes.Add(viewModes[index]);
            typeFilters.Add(typeFilters[index]);
            navHistoryBack.Add(new List<string>(navHistoryBack[index]));
            navHistoryForward.Add(new List<string>(navHistoryForward[index]));
            expandedFolders.Add(new HashSet<string>(expandedFolders[index]));
            panelSelections.Add(new HashSet<string>(panelSelections[index]));
            float eq = 1f / rootPaths.Count;
            panelRatios.Clear();
            for (int i = 0; i < rootPaths.Count; i++) panelRatios.Add(eq);
            SaveAll();
        }

        public void SwapPanels(int from, int to)
        {
            SwapL(rootPaths, from, to); SwapL(browsePaths, from, to); SwapL(scrollPositions, from, to);
            SwapL(searchFilters, from, to); SwapL(panelRatios, from, to); SwapL(sortModes, from, to);
            SwapL(viewModes, from, to); SwapL(typeFilters, from, to);
            SwapL(navHistoryBack, from, to); SwapL(navHistoryForward, from, to);
            SwapL(expandedFolders, from, to);
            SwapL(panelSelections, from, to);
            SaveAll();
        }

        private void SwapL<T>(List<T> l, int f, int t)
        {
            if (f < 0 || f >= l.Count || t < 0 || t >= l.Count) return;
            T item = l[f]; l.RemoveAt(f); l.Insert(t, item);
        }

        public void EnsureRatios()
        {
            while (panelRatios.Count < rootPaths.Count) panelRatios.Add(0);
            while (panelRatios.Count > rootPaths.Count) panelRatios.RemoveAt(panelRatios.Count - 1);
            float s = 0;
            for (int i = 0; i < panelRatios.Count; i++) s += panelRatios[i];
            if (panelRatios.Count > 0 && (s < 0.01f || Mathf.Abs(s - 1f) > 0.01f))
            { float eq = 1f / panelRatios.Count; for (int i = 0; i < panelRatios.Count; i++) panelRatios[i] = eq; }
        }

        public void EnsureListSize(int i)
        {
            while (browsePaths.Count <= i) browsePaths.Add(rootPaths[i]);
            while (scrollPositions.Count <= i) scrollPositions.Add(Vector2.zero);
            while (searchFilters.Count <= i) searchFilters.Add("");
            while (panelRatios.Count <= i) panelRatios.Add(1f / rootPaths.Count);
            while (sortModes.Count <= i) sortModes.Add(SortMode.Name);
            while (viewModes.Count <= i) viewModes.Add(ViewMode.List);
            while (typeFilters.Count <= i) typeFilters.Add(TypeFilter.All);
            while (navHistoryBack.Count <= i) navHistoryBack.Add(new List<string>(NAV_HISTORY_MAX));
            while (navHistoryForward.Count <= i) navHistoryForward.Add(new List<string>(NAV_HISTORY_MAX));
            while (expandedFolders.Count <= i) expandedFolders.Add(new HashSet<string>());
            while (panelSelections.Count <= i) panelSelections.Add(new HashSet<string>());
        }

        #endregion

        #region Persistence

        public void SaveAll()
        {
            EditorPrefs.SetString(PREFS_KEY, string.Join("|", rootPaths));
            EditorPrefs.SetString(PREFS_BROWSE, string.Join("|", browsePaths));
            EditorPrefs.SetString(PREFS_RATIOS, string.Join("|", panelRatios.Select(r => r.ToString("F4"))));
            EditorPrefs.SetString(PREFS_FAVORITES, string.Join("|", favoritePaths));
            EditorPrefs.SetString(PREFS_QUICK_ACCESS, string.Join("|", quickAccessPaths));
            // Color tags: "path=idx|path=idx|..."
            EditorPrefs.SetString(PREFS_COLOR_TAGS, string.Join("|", colorTags.Select(kv => $"{kv.Key}={kv.Value}")));
            EditorPrefs.SetBool(PREFS_LOCK_FIRST, isFirstPanelLocked);
        }

        public void LoadAll()
        {
            rootPaths.Clear(); browsePaths.Clear(); scrollPositions.Clear();
            searchFilters.Clear(); panelRatios.Clear(); sortModes.Clear();
            viewModes.Clear(); typeFilters.Clear(); navHistoryBack.Clear(); navHistoryForward.Clear();
            favoritePaths.Clear(); colorTags.Clear(); quickAccessPaths.Clear();
            expandedFolders.Clear(); panelSelections.Clear();
            isFirstPanelLocked = EditorPrefs.GetBool(PREFS_LOCK_FIRST, false);

            string sr = EditorPrefs.GetString(PREFS_KEY, "");
            if (!string.IsNullOrEmpty(sr)) rootPaths.AddRange(sr.Split('|').Where(p => !string.IsNullOrEmpty(p)));
            string sb = EditorPrefs.GetString(PREFS_BROWSE, "");
            string[] ba = string.IsNullOrEmpty(sb) ? new string[0] : sb.Split('|');
            string srt = EditorPrefs.GetString(PREFS_RATIOS, "");
            string[] ra = string.IsNullOrEmpty(srt) ? new string[0] : srt.Split('|');

            string sf = EditorPrefs.GetString(PREFS_FAVORITES, "");
            if (!string.IsNullOrEmpty(sf))
                foreach (string f in sf.Split('|'))
                    if (!string.IsNullOrEmpty(f)) favoritePaths.Add(f);

            // Load quick access
            string sq = EditorPrefs.GetString(PREFS_QUICK_ACCESS, "");
            if (!string.IsNullOrEmpty(sq))
                foreach (string q in sq.Split('|'))
                    if (!string.IsNullOrEmpty(q)) quickAccessPaths.Add(q);

            // Load color tags
            string sc = EditorPrefs.GetString(PREFS_COLOR_TAGS, "");
            if (!string.IsNullOrEmpty(sc))
            {
                foreach (string entry in sc.Split('|'))
                {
                    int eq = entry.LastIndexOf('=');
                    if (eq > 0 && int.TryParse(entry.Substring(eq + 1), out int ci))
                        colorTags[entry.Substring(0, eq)] = ci;
                }
            }

            for (int i = 0; i < rootPaths.Count; i++)
            {
                string bp = (i < ba.Length && !string.IsNullOrEmpty(ba[i]) && Directory.Exists(ba[i])) ? ba[i] : rootPaths[i];
                browsePaths.Add(bp); scrollPositions.Add(Vector2.zero); searchFilters.Add("");
                float r = 1f / Mathf.Max(rootPaths.Count, 1);
                if (i < ra.Length && float.TryParse(ra[i], out float p)) r = p;
                panelRatios.Add(r); sortModes.Add(SortMode.Name); viewModes.Add(ViewMode.List);
                typeFilters.Add(TypeFilter.All);
                navHistoryBack.Add(new List<string>(NAV_HISTORY_MAX));
                navHistoryForward.Add(new List<string>(NAV_HISTORY_MAX));
                expandedFolders.Add(new HashSet<string>());
                panelSelections.Add(new HashSet<string>());
            }
        }

        #endregion

        #region Path Utilities

        public string GetAssetRelativeLabel(string fp)
        {
            try
            {
                string dp = Path.GetFullPath(Application.dataPath);
                string n = Path.GetFullPath(fp);
                if (n == dp) return "Assets";
                if (n.StartsWith(dp)) return "Assets" + n.Substring(dp.Length).Replace('\\', '/');
                return new DirectoryInfo(fp).Name;
            }
            catch { return Path.GetFileName(fp); }
        }

        public string GetRelativePath(string bp, string fp)
        {
            try
            {
                bp = Path.GetFullPath(bp).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                fp = Path.GetFullPath(fp).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (fp == bp) return "";
                string sep1 = bp + Path.DirectorySeparatorChar;
                string sep2 = bp + Path.AltDirectorySeparatorChar;
                if (fp.StartsWith(sep1)) return fp.Substring(sep1.Length);
                if (fp.StartsWith(sep2)) return fp.Substring(sep2.Length);
                return "";
            }
            catch { return ""; }
        }

        public string AssetPathFromAbsolute(string fp)
        {
            try
            {
                string dp = Path.GetFullPath(Application.dataPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string n = Path.GetFullPath(fp).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (n == dp) return "Assets";
                if (n.StartsWith(dp + Path.DirectorySeparatorChar) || n.StartsWith(dp + Path.AltDirectorySeparatorChar))
                    return "Assets" + n.Substring(dp.Length).Replace('\\', '/');
                return null;
            }
            catch { return null; }
        }

        /// <summary>Convert asset-relative path (Assets/...) to absolute path.</summary>
        public string AbsolutePathFromAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) return null;
            try
            {
                string dataPath = Path.GetFullPath(Application.dataPath);
                // "Assets/Foo/Bar.cs" → dataPath + "/Foo/Bar.cs"
                if (assetPath == "Assets") return dataPath;
                if (assetPath.StartsWith("Assets/") || assetPath.StartsWith("Assets\\"))
                    return Path.GetFullPath(Path.Combine(dataPath, assetPath.Substring("Assets/".Length)));
                return null;
            }
            catch { return null; }
        }

        /// <summary>
        /// Navigate first panel to the folder containing the given asset path,
        /// and select the file. Used for auto-follow Unity selection.
        /// </summary>
        public bool NavigateFirstPanelToAsset(string assetPath)
        {
            if (rootPaths.Count == 0) return false;
            string absPath = AbsolutePathFromAsset(assetPath);
            if (absPath == null) return false;

            string folderPath;
            bool isFile = File.Exists(absPath);
            if (isFile)
                folderPath = Path.GetDirectoryName(absPath);
            else if (Directory.Exists(absPath))
                folderPath = absPath;
            else
                return false;

            if (string.IsNullOrEmpty(folderPath)) return false;
            folderPath = Path.GetFullPath(folderPath);

            // Ensure panel 0 root is Assets so breadcrumb can navigate up
            string assetsRoot = Path.GetFullPath(Application.dataPath);
            if (rootPaths[0] != assetsRoot)
                rootPaths[0] = assetsRoot;

            // Navigate panel 0
            EnsureListSize(0);
            string current = browsePaths[0];
            if (current != folderPath)
            {
                navHistoryBack[0].Add(current);
                if (navHistoryBack[0].Count > NAV_HISTORY_MAX)
                    navHistoryBack[0].RemoveAt(0);
                navHistoryForward[0].Clear();
                browsePaths[0] = folderPath;
                scrollPositions[0] = Vector2.zero;
                searchFilters[0] = "";
                InvalidateCache(folderPath);
            }

            // Select the file in panel 0
            if (isFile)
            {
                var sel = panelSelections[0];
                sel.Clear();
                sel.Add(absPath);
                selectedPaths.Clear();
                selectedPaths.Add(absPath);
                lastClickedPath = absPath;
                activePanelIndex = 0;
            }

            renamingPath = null;
            return true;
        }

        #endregion
    }
}
