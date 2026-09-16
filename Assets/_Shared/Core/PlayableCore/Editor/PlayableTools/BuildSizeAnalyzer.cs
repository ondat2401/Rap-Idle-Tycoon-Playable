using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amanotes.Core;

namespace Amanotes.Core.Editor
{
    /// <summary>
    /// Analyzes and displays build size breakdown by asset type.
    /// Helps identify what's consuming the most space in the 5MB Luna Playable budget.
    /// </summary>
    public class BuildSizeAnalyzer : EditorWindow
    {
        #region Nested Types

        public enum AssetCategory
        {
            Texture,
            Audio,
            Mesh,
            Material,
            Shader,
            Font,
            Animation,
            Prefab,
            ScriptableObject,
            Scene,
            Other
        }

        public class AssetSizeEntry
        {
            public string assetPath;
            public string assetName;
            public AssetCategory category;
            public long fileSizeBytes;
            public long runtimeSizeBytes;
            public string details;
        }

        public class CategorySummary
        {
            public AssetCategory category;
            public long totalBytes;
            public long totalRuntimeBytes;
            public int count;
            public float percentage;
        }

        #endregion

        #region Fields

        private List<AssetSizeEntry> allEntries = new List<AssetSizeEntry>();
        private List<AssetSizeEntry> filteredEntries = new List<AssetSizeEntry>();
        private List<CategorySummary> summaries = new List<CategorySummary>();
        private long totalSizeBytes;
        private long totalRuntimeSizeBytes;

        private string searchText = "";
        private AssetCategory? categoryFilter = null;
        private Vector2 summaryScrollPos;
        private Vector2 detailScrollPos;
        private bool showDetails = true;

        private const long LUNA_BUDGET_BYTES = 5 * 1024 * 1024; // 5MB

        #endregion

        #region Window Setup

        [MenuItem("Tools/Playable Standard Pipeline/Playable Tools/Build Size Analyzer")]
        public static void ShowWindow()
        {
            var window = GetWindow<BuildSizeAnalyzer>("Build Size Analyzer");
            window.minSize = new Vector2(650, 500);
        }

        #endregion

        #region OnGUI

        private void OnGUI()
        {
            DrawToolbar();

            if (allEntries.Count == 0)
            {
                EditorGUILayout.HelpBox("Click 'Analyze Project' to scan all assets and calculate size breakdown.", MessageType.Info);
                return;
            }

            DrawBudgetBar();
            DrawCategorySummary();

            showDetails = EditorGUILayout.Foldout(showDetails, $"Asset Details ({filteredEntries.Count}/{allEntries.Count})");
            if (showDetails)
            {
                DrawFilterBar();
                DrawAssetList();
            }
        }

        #endregion

        #region Toolbar

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Analyze Project", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                AnalyzeProject();
            }

            if (GUILayout.Button("Analyze Scene Only", EditorStyles.toolbarButton, GUILayout.Width(120)))
            {
                AnalyzeSceneOnly();
            }

            GUILayout.FlexibleSpace();

            if (allEntries.Count > 0 && GUILayout.Button("Export CSV", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                ExportCSV();
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Budget Bar

        private void DrawBudgetBar()
        {
            EditorGUILayout.Space(4);
            var rect = EditorGUILayout.GetControlRect(false, 28);

            float ratio = Mathf.Clamp01((float)totalSizeBytes / LUNA_BUDGET_BYTES);
            Color barColor = ratio < 0.7f ? new Color(0.2f, 0.8f, 0.3f) :
                             ratio < 0.9f ? new Color(0.9f, 0.7f, 0.1f) :
                             new Color(0.9f, 0.2f, 0.2f);

            // Background
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));

            // Fill
            var fillRect = new Rect(rect.x, rect.y, rect.width * ratio, rect.height);
            EditorGUI.DrawRect(fillRect, barColor);

            // Text
            string sizeText = FormatSize(totalSizeBytes);
            string label = $"Disk: {sizeText} / 5.00 MB ({ratio * 100:F1}%)";
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            EditorGUI.LabelField(rect, label, style);

            // Runtime total below
            EditorGUILayout.LabelField(
                $"Total Disk: {FormatSize(totalSizeBytes)}  |  Total Runtime: {FormatSize(totalRuntimeSizeBytes)}",
                EditorStyles.miniLabel);

            EditorGUILayout.Space(2);
        }

        #endregion

        #region Category Summary

        private void DrawCategorySummary()
        {
            EditorGUILayout.LabelField("Category Breakdown", EditorStyles.boldLabel);

            summaryScrollPos = EditorGUILayout.BeginScrollView(summaryScrollPos, GUILayout.MaxHeight(180));

            foreach (var s in summaries)
            {
                EditorGUILayout.BeginHorizontal();

                // Category name as button (click to filter)
                bool isSelected = categoryFilter.HasValue && categoryFilter.Value == s.category;
                var btnStyle = isSelected ? EditorStyles.toolbarButton : EditorStyles.miniButton;
                if (GUILayout.Button(s.category.ToString(), btnStyle, GUILayout.Width(120)))
                {
                    if (isSelected)
                        categoryFilter = null;
                    else
                        categoryFilter = s.category;
                    ApplyFilter();
                }

                // Bar
                var barRect = EditorGUILayout.GetControlRect(false, 16, GUILayout.MinWidth(200));
                float barRatio = totalSizeBytes > 0 ? (float)s.totalBytes / totalSizeBytes : 0;
                EditorGUI.DrawRect(barRect, new Color(0.2f, 0.2f, 0.2f));
                var barFill = new Rect(barRect.x, barRect.y, barRect.width * barRatio, barRect.height);
                EditorGUI.DrawRect(barFill, GetCategoryColor(s.category));

                // Info
                EditorGUILayout.LabelField($"Disk: {FormatSize(s.totalBytes)}", GUILayout.Width(100));
                EditorGUILayout.LabelField($"Rt: {FormatSize(s.totalRuntimeBytes)}", GUILayout.Width(100));
                EditorGUILayout.LabelField($"({s.count})", GUILayout.Width(35));
                EditorGUILayout.LabelField($"{s.percentage:F1}%", GUILayout.Width(50));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(4);
        }

        private Color GetCategoryColor(AssetCategory cat)
        {
            switch (cat)
            {
                case AssetCategory.Texture: return new Color(0.3f, 0.6f, 0.9f);
                case AssetCategory.Audio: return new Color(0.9f, 0.5f, 0.2f);
                case AssetCategory.Mesh: return new Color(0.4f, 0.8f, 0.4f);
                case AssetCategory.Material: return new Color(0.7f, 0.4f, 0.8f);
                case AssetCategory.Shader: return new Color(0.9f, 0.9f, 0.3f);
                case AssetCategory.Font: return new Color(0.6f, 0.6f, 0.6f);
                case AssetCategory.Animation: return new Color(0.2f, 0.8f, 0.8f);
                case AssetCategory.Prefab: return new Color(0.5f, 0.5f, 0.9f);
                case AssetCategory.ScriptableObject: return new Color(0.8f, 0.6f, 0.4f);
                case AssetCategory.Scene: return new Color(0.9f, 0.3f, 0.5f);
                default: return new Color(0.5f, 0.5f, 0.5f);
            }
        }

        #endregion

        #region Filter & Asset List

        private void DrawFilterBar()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            var newSearch = EditorGUILayout.TextField(searchText);
            if (newSearch != searchText)
            {
                searchText = newSearch;
                ApplyFilter();
            }

            if (categoryFilter.HasValue)
            {
                if (GUILayout.Button($"Clear filter: {categoryFilter.Value}", EditorStyles.miniButton, GUILayout.Width(150)))
                {
                    categoryFilter = null;
                    ApplyFilter();
                }
            }
            GUILayout.Space(13); // scrollbar compensation
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAssetList()
        {
            // Header row
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Category", EditorStyles.boldLabel, GUILayout.Width(90));
            EditorGUILayout.LabelField("Name", EditorStyles.boldLabel, GUILayout.MinWidth(200));
            EditorGUILayout.LabelField("Disk", EditorStyles.boldLabel, GUILayout.Width(75));
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel, GUILayout.Width(75));
            EditorGUILayout.LabelField("Details", EditorStyles.boldLabel, GUILayout.Width(200));
            GUILayout.Space(13);
            EditorGUILayout.EndHorizontal();

            detailScrollPos = EditorGUILayout.BeginScrollView(detailScrollPos);

            foreach (var entry in filteredEntries)
            {
                EditorGUILayout.BeginHorizontal("box");

                // Category tag
                var tagRect = EditorGUILayout.GetControlRect(false, 16, GUILayout.Width(90));
                EditorGUI.DrawRect(tagRect, GetCategoryColor(entry.category));
                var tagStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
                EditorGUI.LabelField(tagRect, entry.category.ToString(), tagStyle);

                // Name (clickable)
                if (GUILayout.Button(entry.assetName, EditorStyles.linkLabel, GUILayout.MinWidth(200)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(entry.assetPath);
                    if (obj != null) EditorGUIUtility.PingObject(obj);
                }

                // Disk Size
                EditorGUILayout.LabelField(FormatSize(entry.fileSizeBytes), GUILayout.Width(75));

                // Runtime Size
                EditorGUILayout.LabelField(FormatSize(entry.runtimeSizeBytes), GUILayout.Width(75));

                // Details
                if (!string.IsNullOrEmpty(entry.details))
                {
                    EditorGUILayout.LabelField(entry.details, EditorStyles.miniLabel, GUILayout.Width(200));
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region Analysis Logic

        private void AnalyzeProject()
        {
            allEntries.Clear();
            var guids = AssetDatabase.FindAssets("", new[] { "Assets" });

            EditorUtility.DisplayProgressBar("Build Size Analyzer", "Scanning assets...", 0);

            for (int i = 0; i < guids.Length; i++)
            {
                if (i % 100 == 0)
                    EditorUtility.DisplayProgressBar("Build Size Analyzer", $"Scanning... {i}/{guids.Length}", (float)i / guids.Length);

                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (AssetDatabase.IsValidFolder(path)) continue;
                if (path.Contains("/Editor/")) continue; // skip editor-only assets

                AddAssetEntry(path);
            }

            EditorUtility.ClearProgressBar();
            FinalizeAnalysis();
        }

        private void AnalyzeSceneOnly()
        {
            allEntries.Clear();
            var dependencies = AssetDatabase.GetDependencies(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().path, true);

            foreach (var dep in dependencies)
            {
                if (AssetDatabase.IsValidFolder(dep)) continue;
                AddAssetEntry(dep);
            }

            FinalizeAnalysis();
        }

        private void AddAssetEntry(string path)
        {
            long size = GetFileSize(path);
            if (size <= 0) return;

            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            long runtimeSize = 0;
            if (asset != null)
                runtimeSize = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(asset);

            var entry = new AssetSizeEntry
            {
                assetPath = path,
                assetName = Path.GetFileName(path),
                fileSizeBytes = size,
                runtimeSizeBytes = runtimeSize,
                category = CategorizeAsset(path),
                details = GetAssetDetails(path)
            };

            allEntries.Add(entry);
        }

        private void FinalizeAnalysis()
        {
            // Sort by size descending
            allEntries.Sort((a, b) => b.fileSizeBytes.CompareTo(a.fileSizeBytes));

            totalSizeBytes = allEntries.Sum(e => e.fileSizeBytes);
            totalRuntimeSizeBytes = allEntries.Sum(e => e.runtimeSizeBytes);

            // Build summaries
            summaries = allEntries
                .GroupBy(e => e.category)
                .Select(g => new CategorySummary
                {
                    category = g.Key,
                    totalBytes = g.Sum(e => e.fileSizeBytes),
                    totalRuntimeBytes = g.Sum(e => e.runtimeSizeBytes),
                    count = g.Count(),
                    percentage = totalSizeBytes > 0 ? (float)g.Sum(e => e.fileSizeBytes) / totalSizeBytes * 100f : 0
                })
                .OrderByDescending(s => s.totalBytes)
                .ToList();

            ApplyFilter();
            Repaint();
        }

        private AssetCategory CategorizeAsset(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            switch (ext)
            {
                case ".png": case ".jpg": case ".jpeg": case ".tga": case ".psd":
                case ".bmp": case ".gif": case ".tif": case ".tiff": case ".exr":
                case ".hdr":
                    return AssetCategory.Texture;

                case ".mp3": case ".ogg": case ".wav": case ".aiff": case ".aif":
                case ".flac":
                    return AssetCategory.Audio;

                case ".fbx": case ".obj": case ".blend": case ".dae": case ".3ds":
                case ".max": case ".mesh":
                    return AssetCategory.Mesh;

                case ".mat":
                    return AssetCategory.Material;

                case ".shader": case ".cginc": case ".hlsl": case ".glsl":
                case ".shadergraph": case ".shadersubgraph":
                    return AssetCategory.Shader;

                case ".ttf": case ".otf": case ".fontsettings":
                    return AssetCategory.Font;

                case ".anim": case ".controller": case ".overridecontroller":
                    return AssetCategory.Animation;

                case ".prefab":
                    return AssetCategory.Prefab;

                case ".asset":
                    return AssetCategory.ScriptableObject;

                case ".unity":
                    return AssetCategory.Scene;

                default:
                    return AssetCategory.Other;
            }
        }

        private string GetAssetDetails(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();

            if (ext == ".png" || ext == ".jpg" || ext == ".tga" || ext == ".psd")
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (tex != null)
                        return $"{tex.width}x{tex.height} {tex.format}";
                }
            }

            if (ext == ".mp3" || ext == ".ogg" || ext == ".wav")
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                    return $"{clip.length:F1}s {clip.frequency}Hz {(clip.channels == 1 ? "Mono" : "Stereo")}";
            }

            return "";
        }

        #endregion

        #region Filter

        private void ApplyFilter()
        {
            filteredEntries = allEntries.Where(e =>
            {
                if (categoryFilter.HasValue && e.category != categoryFilter.Value)
                    return false;
                if (!string.IsNullOrEmpty(searchText) &&
                    e.assetName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
                return true;
            }).ToList();
        }

        #endregion

        #region Utilities

        private long GetFileSize(string assetPath)
        {
            try
            {
                string fullPath = Path.Combine(Application.dataPath, "..", assetPath);
                var fi = new FileInfo(fullPath);
                return fi.Exists ? fi.Length : 0;
            }
            catch { return 0; }
        }

        private string FormatSize(long bytes)
        {
            if (bytes >= 1048576) return $"{bytes / 1048576f:F2} MB";
            if (bytes >= 1024) return $"{bytes / 1024f:F1} KB";
            return $"{bytes} B";
        }

        #endregion

        #region CSV Export

        private void ExportCSV()
        {
            string path = EditorUtility.SaveFilePanel("Export Build Size Report", "", "BuildSizeReport", "csv");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                using (var writer = new StreamWriter(path))
                {
                    writer.WriteLine("Name,Path,Category,Size(KB),Details");
                    foreach (var e in filteredEntries)
                    {
                        float sizeKB = e.fileSizeBytes / 1024f;
                        writer.WriteLine($"\"{e.assetName}\",\"{e.assetPath}\",\"{e.category}\",{sizeKB:F1},\"{e.details}\"");
                    }
                }
                EditorUtility.DisplayDialog("Export CSV", $"Exported to:\n{path}", "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Export CSV", $"Failed:\n{ex.Message}", "OK");
            }
        }

        #endregion
    }
}
