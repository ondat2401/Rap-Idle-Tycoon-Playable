using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amanotes.Core;

namespace Amanotes.Core.Editor
{
    /// <summary>
    /// Validates scene dependencies and warns about unnecessary or suspicious assets
    /// that could bloat the Luna Playable build.
    /// </summary>
    public class SceneDependencyValidator : EditorWindow
    {
        #region Nested Types

        public enum Severity { Info, Warning, Error }

        public class DependencyEntry
        {
            public string assetPath;
            public string assetName;
            public string assetType;
            public long fileSizeBytes;
            public long runtimeSizeBytes;
            public Severity severity;
            public string message;
            public List<string> referencedBy = new List<string>();
        }

        #endregion

        #region Fields

        private List<DependencyEntry> allEntries = new List<DependencyEntry>();
        private List<DependencyEntry> filteredEntries = new List<DependencyEntry>();
        private Vector2 scrollPos;
        private string searchText = "";
        private bool showInfo = true;
        private bool showWarnings = true;
        private bool showErrors = true;
        private int errorCount, warningCount, infoCount;
        private long totalSizeBytes;

        // Configurable thresholds
        private const long LARGE_TEXTURE_THRESHOLD = 512 * 1024; // 512KB
        private const long LARGE_AUDIO_THRESHOLD = 256 * 1024;   // 256KB
        private const int MAX_TEXTURE_DIMENSION = 2048;

        #endregion

        #region Window Setup

        [MenuItem("Tools/Playable Standard Pipeline/Playable Tools/Scene Dependency Validator")]
        public static void ShowWindow()
        {
            var window = GetWindow<SceneDependencyValidator>("Scene Dependency Validator");
            window.minSize = new Vector2(650, 450);
        }

        #endregion

        #region OnGUI

        private void OnGUI()
        {
            DrawToolbar();

            if (allEntries.Count == 0)
            {
                EditorGUILayout.HelpBox("Click 'Validate Scene' to scan all dependencies of the current scene.", MessageType.Info);
                return;
            }

            DrawSummary();
            DrawFilterBar();
            DrawEntryList();
        }

        #endregion

        #region Toolbar

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Validate Scene", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                ValidateScene();
            }

            GUILayout.FlexibleSpace();

            if (allEntries.Count > 0 && GUILayout.Button("Export CSV", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                ExportCSV();
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Summary

        private void DrawSummary()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            string sizeStr = totalSizeBytes >= 1048576
                ? $"{totalSizeBytes / 1048576f:F2} MB"
                : $"{totalSizeBytes / 1024f:F1} KB";

            EditorGUILayout.LabelField(
                $"Dependencies: {allEntries.Count}  |  Total: {sizeStr}  |  ",
                EditorStyles.miniLabel, GUILayout.Width(280));

            // Colored counts
            var origColor = GUI.color;

            GUI.color = new Color(0.9f, 0.2f, 0.2f);
            EditorGUILayout.LabelField($"Errors: {errorCount}", EditorStyles.miniLabel, GUILayout.Width(70));

            GUI.color = new Color(0.9f, 0.7f, 0.1f);
            EditorGUILayout.LabelField($"Warnings: {warningCount}", EditorStyles.miniLabel, GUILayout.Width(90));

            GUI.color = new Color(0.4f, 0.7f, 0.9f);
            EditorGUILayout.LabelField($"Info: {infoCount}", EditorStyles.miniLabel, GUILayout.Width(60));

            GUI.color = origColor;

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Filter

        private void DrawFilterBar()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            var newSearch = EditorGUILayout.TextField(searchText, GUILayout.MinWidth(120));
            if (newSearch != searchText) { searchText = newSearch; ApplyFilter(); }

            GUILayout.Space(10);

            var newErr = GUILayout.Toggle(showErrors, "Errors", EditorStyles.toolbarButton, GUILayout.Width(55));
            var newWarn = GUILayout.Toggle(showWarnings, "Warn", EditorStyles.toolbarButton, GUILayout.Width(45));
            var newInfo = GUILayout.Toggle(showInfo, "Info", EditorStyles.toolbarButton, GUILayout.Width(40));

            if (newErr != showErrors || newWarn != showWarnings || newInfo != showInfo)
            {
                showErrors = newErr;
                showWarnings = newWarn;
                showInfo = newInfo;
                ApplyFilter();
            }

            GUILayout.Space(13); // scrollbar compensation
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Entry List

        private void DrawEntryList()
        {
            // Header row
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.Width(20)); // severity icon
            EditorGUILayout.LabelField("Name", EditorStyles.boldLabel, GUILayout.MinWidth(180));
            EditorGUILayout.LabelField("Type", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("Disk", EditorStyles.boldLabel, GUILayout.Width(70));
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel, GUILayout.Width(70));
            EditorGUILayout.LabelField("Message", EditorStyles.boldLabel);
            GUILayout.Space(13);
            EditorGUILayout.EndHorizontal();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            foreach (var entry in filteredEntries)
            {
                EditorGUILayout.BeginHorizontal("box");

                // Severity icon
                var iconRect = EditorGUILayout.GetControlRect(false, 16, GUILayout.Width(20));
                Color iconColor;
                switch (entry.severity)
                {
                    case Severity.Error: iconColor = new Color(0.9f, 0.2f, 0.2f); break;
                    case Severity.Warning: iconColor = new Color(0.9f, 0.7f, 0.1f); break;
                    default: iconColor = new Color(0.4f, 0.7f, 0.9f); break;
                }
                EditorGUI.DrawRect(iconRect, iconColor);

                // Asset name (clickable)
                if (GUILayout.Button(entry.assetName, EditorStyles.linkLabel, GUILayout.MinWidth(180)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(entry.assetPath);
                    if (obj != null) EditorGUIUtility.PingObject(obj);
                }

                // Type
                EditorGUILayout.LabelField(entry.assetType, EditorStyles.miniLabel, GUILayout.Width(80));

                // Disk Size
                string diskStr = entry.fileSizeBytes >= 1048576
                    ? $"{entry.fileSizeBytes / 1048576f:F2} MB"
                    : $"{entry.fileSizeBytes / 1024f:F1} KB";
                EditorGUILayout.LabelField(diskStr, GUILayout.Width(70));

                // Runtime Size
                string rtStr = entry.runtimeSizeBytes >= 1048576
                    ? $"{entry.runtimeSizeBytes / 1048576f:F2} MB"
                    : $"{entry.runtimeSizeBytes / 1024f:F1} KB";
                EditorGUILayout.LabelField(rtStr, GUILayout.Width(70));

                // Message
                EditorGUILayout.LabelField(entry.message, EditorStyles.miniLabel);

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region Validation Logic

        private void ValidateScene()
        {
            allEntries.Clear();

            string scenePath = SceneManager.GetActiveScene().path;
            if (string.IsNullOrEmpty(scenePath))
            {
                EditorUtility.DisplayDialog("Scene Dependency Validator", "Please save the scene first.", "OK");
                return;
            }

            string[] dependencies = AssetDatabase.GetDependencies(scenePath, true);

            EditorUtility.DisplayProgressBar("Scene Dependency Validator", "Analyzing...", 0);

            for (int i = 0; i < dependencies.Length; i++)
            {
                if (i % 50 == 0)
                    EditorUtility.DisplayProgressBar("Scene Dependency Validator", $"Analyzing {i}/{dependencies.Length}", (float)i / dependencies.Length);

                string dep = dependencies[i];
                if (AssetDatabase.IsValidFolder(dep)) continue;
                if (dep.EndsWith(".cs")) continue; // skip scripts

                AnalyzeDependency(dep);
            }

            EditorUtility.ClearProgressBar();

            // Sort: errors first, then warnings, then info, then by size desc
            allEntries.Sort((a, b) =>
            {
                int sevCompare = a.severity.CompareTo(b.severity);
                if (sevCompare != 0) return sevCompare;
                return b.fileSizeBytes.CompareTo(a.fileSizeBytes);
            });

            totalSizeBytes = allEntries.Sum(e => e.fileSizeBytes);
            errorCount = allEntries.Count(e => e.severity == Severity.Error);
            warningCount = allEntries.Count(e => e.severity == Severity.Warning);
            infoCount = allEntries.Count(e => e.severity == Severity.Info);

            ApplyFilter();
            Repaint();
        }

        private void AnalyzeDependency(string path)
        {
            long size = GetFileSize(path);
            string ext = Path.GetExtension(path).ToLowerInvariant();
            string assetType = GetAssetType(ext);

            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            long runtimeSize = 0;
            if (asset != null)
                runtimeSize = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(asset);

            var entry = new DependencyEntry
            {
                assetPath = path,
                assetName = Path.GetFileName(path),
                assetType = assetType,
                fileSizeBytes = size,
                runtimeSizeBytes = runtimeSize,
                severity = Severity.Info,
                message = ""
            };

            // Run validation rules
            ValidateTexture(entry, path, ext);
            ValidateAudio(entry, path, ext);
            ValidateFont(entry, path, ext);
            ValidateSuspiciousAsset(entry, path, ext);

            if (entry.severity == Severity.Info && size > 0)
            {
                entry.message = "OK";
            }

            allEntries.Add(entry);
        }

        private void ValidateTexture(DependencyEntry entry, string path, string ext)
        {
            if (ext != ".png" && ext != ".jpg" && ext != ".tga" && ext != ".psd") return;

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) return;

            // Check oversized textures
            if (tex.width > MAX_TEXTURE_DIMENSION || tex.height > MAX_TEXTURE_DIMENSION)
            {
                entry.severity = Severity.Warning;
                entry.message = $"Large dimensions: {tex.width}x{tex.height}. Consider resizing for playable.";
            }

            // Check uncompressed
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureCompression == TextureImporterCompression.Uncompressed)
            {
                entry.severity = Severity.Warning;
                entry.message = "Uncompressed texture. Enable compression to reduce size.";
            }

            // Check large file
            if (entry.fileSizeBytes > LARGE_TEXTURE_THRESHOLD && entry.severity == Severity.Info)
            {
                entry.severity = Severity.Warning;
                entry.message = $"Large texture ({entry.fileSizeBytes / 1024f:F0} KB). Consider optimizing.";
            }

            // PSD files should not be in build
            if (ext == ".psd")
            {
                entry.severity = Severity.Error;
                entry.message = "PSD file in build. Convert to PNG/JPG to save space.";
            }
        }

        private void ValidateAudio(DependencyEntry entry, string path, string ext)
        {
            if (ext != ".mp3" && ext != ".ogg" && ext != ".wav" && ext != ".aiff") return;

            // WAV/AIFF are uncompressed
            if (ext == ".wav" || ext == ".aiff")
            {
                entry.severity = Severity.Warning;
                entry.message = "Uncompressed audio format. Convert to OGG/MP3 for smaller build.";
            }

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null)
            {
                // Stereo warning
                if (clip.channels > 1)
                {
                    entry.severity = Severity.Warning;
                    entry.message = $"Stereo audio ({clip.channels}ch). Consider mono for playable.";
                }

                // Long audio
                if (clip.length > 30f && entry.severity == Severity.Info)
                {
                    entry.severity = Severity.Warning;
                    entry.message = $"Long audio ({clip.length:F1}s). Playable usually needs <30s.";
                }
            }

            if (entry.fileSizeBytes > LARGE_AUDIO_THRESHOLD && entry.severity == Severity.Info)
            {
                entry.severity = Severity.Warning;
                entry.message = $"Large audio ({entry.fileSizeBytes / 1024f:F0} KB). Consider compressing.";
            }
        }

        private void ValidateFont(DependencyEntry entry, string path, string ext)
        {
            if (ext != ".ttf" && ext != ".otf") return;

            if (entry.fileSizeBytes > 256 * 1024)
            {
                entry.severity = Severity.Warning;
                entry.message = $"Large font file ({entry.fileSizeBytes / 1024f:F0} KB). Consider subset or lighter font.";
            }
        }

        private void ValidateSuspiciousAsset(DependencyEntry entry, string path, string ext)
        {
            // Check for common SDK/analytics paths
            string lowerPath = path.ToLowerInvariant();
            string[] suspiciousKeywords = {
                "admob", "ironsource", "vungle", "appsflyer", "firebase",
                "gameanalytics", "facebook", "applovin", "chartboost",
                "srdebugger", "odin"
            };

            foreach (var keyword in suspiciousKeywords)
            {
                if (lowerPath.Contains(keyword))
                {
                    entry.severity = Severity.Error;
                    entry.message = $"SDK/plugin asset detected ({keyword}). Should be excluded from playable build.";
                    return;
                }
            }
        }

        #endregion

        #region Utilities

        private void ApplyFilter()
        {
            filteredEntries = allEntries.Where(e =>
            {
                if (e.severity == Severity.Error && !showErrors) return false;
                if (e.severity == Severity.Warning && !showWarnings) return false;
                if (e.severity == Severity.Info && !showInfo) return false;
                if (!string.IsNullOrEmpty(searchText) &&
                    e.assetName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
                return true;
            }).ToList();
        }

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

        private string GetAssetType(string ext)
        {
            switch (ext)
            {
                case ".png": case ".jpg": case ".tga": case ".psd": return "Texture";
                case ".mp3": case ".ogg": case ".wav": case ".aiff": return "Audio";
                case ".fbx": case ".obj": case ".mesh": return "Mesh";
                case ".mat": return "Material";
                case ".shader": case ".shadergraph": return "Shader";
                case ".ttf": case ".otf": return "Font";
                case ".anim": case ".controller": return "Animation";
                case ".prefab": return "Prefab";
                case ".asset": return "Asset";
                case ".unity": return "Scene";
                default: return ext;
            }
        }

        #endregion

        #region CSV Export

        private void ExportCSV()
        {
            string path = EditorUtility.SaveFilePanel("Export Dependency Report", "", "SceneDependencyReport", "csv");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                using (var writer = new StreamWriter(path))
                {
                    writer.WriteLine("Severity,Name,Type,Size(KB),Path,Message");
                    foreach (var e in filteredEntries)
                    {
                        float sizeKB = e.fileSizeBytes / 1024f;
                        writer.WriteLine($"\"{e.severity}\",\"{e.assetName}\",\"{e.assetType}\",{sizeKB:F1},\"{e.assetPath}\",\"{e.message}\"");
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
