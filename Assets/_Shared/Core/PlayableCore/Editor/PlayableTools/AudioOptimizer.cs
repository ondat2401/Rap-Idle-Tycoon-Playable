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
    /// Displays all audio clips in the scene/project with detailed info and batch optimization tools.
    /// Helps reduce audio footprint for the 5MB Luna Playable budget.
    /// </summary>
    public class AudioOptimizer : EditorWindow
    {
        #region Nested Types

        public enum AudioScanScope { Scene, Project }

        public class AudioEntry
        {
            public AudioClip clip;
            public string assetPath;
            public string assetName;
            public float duration;
            public int frequency;
            public int channels;
            public long fileSizeBytes;
            public long runtimeSizeBytes;
            public string loadType;
            public string compressionFormat;
            public float quality;
            public bool selected;
        }

        #endregion

        #region Fields

        private List<AudioEntry> allEntries = new List<AudioEntry>();
        private List<AudioEntry> filteredEntries = new List<AudioEntry>();
        private Vector2 scrollPos;
        private string searchText = "";
        private AudioScanScope scanScope = AudioScanScope.Scene;
        private bool showStereoOnly = false;
        private bool selectAll = false;

        // Batch settings
        private bool showBatchPanel = false;
        private bool batchForceToMono = true;
        private int batchSampleRate = 22050;
        private AudioCompressionFormat batchCompression = AudioCompressionFormat.Vorbis;
        private float batchQuality = 0.5f;

        #endregion

        #region Window Setup

        [MenuItem("Tools/Playable Standard Pipeline/Playable Tools/Audio Optimizer")]
        public static void ShowWindow()
        {
            var window = GetWindow<AudioOptimizer>("Audio Optimizer");
            window.minSize = new Vector2(650, 500);
        }

        #endregion

        #region OnGUI

        private void OnGUI()
        {
            DrawToolbar();

            if (allEntries.Count == 0)
            {
                EditorGUILayout.HelpBox("Click 'Scan' to find all audio clips and analyze their settings.", MessageType.Info);
                return;
            }

            DrawSummary();
            DrawFilterBar();
            DrawAudioList();

            if (showBatchPanel)
            {
                DrawBatchPanel();
            }
        }

        #endregion

        #region Toolbar

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Scope selector
            var newScope = (AudioScanScope)EditorGUILayout.EnumPopup(scanScope, EditorStyles.toolbarPopup, GUILayout.Width(80));
            if (newScope != scanScope) scanScope = newScope;

            if (GUILayout.Button("Scan", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                ScanAudio();
            }

            GUILayout.FlexibleSpace();

            if (allEntries.Count > 0)
            {
                var batchLabel = showBatchPanel ? "Hide Batch" : "Batch Optimize";
                if (GUILayout.Button(batchLabel, EditorStyles.toolbarButton, GUILayout.Width(100)))
                {
                    showBatchPanel = !showBatchPanel;
                }

                if (GUILayout.Button("Export CSV", EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    ExportCSV();
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Summary

        private void DrawSummary()
        {
            long totalSize = allEntries.Sum(e => e.fileSizeBytes);
            float totalDuration = allEntries.Sum(e => e.duration);
            int stereoCount = allEntries.Count(e => e.channels > 1);

            string sizeStr = totalSize >= 1048576
                ? $"{totalSize / 1048576f:F2} MB"
                : $"{totalSize / 1024f:F1} KB";

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                $"Clips: {allEntries.Count}  |  Total: {sizeStr}  |  Duration: {totalDuration:F1}s  |  Stereo: {stereoCount}",
                EditorStyles.miniLabel);
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

            var newStereo = GUILayout.Toggle(showStereoOnly, "Stereo Only", EditorStyles.toolbarButton, GUILayout.Width(80));
            if (newStereo != showStereoOnly) { showStereoOnly = newStereo; ApplyFilter(); }

            GUILayout.Space(13); // scrollbar compensation
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Audio List

        private void DrawAudioList()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // Header inside scroll to guarantee alignment
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            var newSelectAll = EditorGUILayout.Toggle(selectAll, GUILayout.Width(20));
            if (newSelectAll != selectAll)
            {
                selectAll = newSelectAll;
                foreach (var e in filteredEntries) e.selected = selectAll;
            }
            EditorGUILayout.LabelField("Name", EditorStyles.boldLabel, GUILayout.MinWidth(150));
            EditorGUILayout.LabelField("Duration", EditorStyles.boldLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("Rate", EditorStyles.boldLabel, GUILayout.Width(55));
            EditorGUILayout.LabelField("Ch", EditorStyles.boldLabel, GUILayout.Width(30));
            EditorGUILayout.LabelField("Format", EditorStyles.boldLabel, GUILayout.Width(70));
            EditorGUILayout.LabelField("Quality", EditorStyles.boldLabel, GUILayout.Width(50));
            EditorGUILayout.LabelField("Disk", EditorStyles.boldLabel, GUILayout.Width(70));
            EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel, GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();

            foreach (var entry in filteredEntries)
            {
                EditorGUILayout.BeginHorizontal();

                entry.selected = EditorGUILayout.Toggle(entry.selected, GUILayout.Width(20));

                // Name (clickable)
                if (GUILayout.Button(entry.assetName, EditorStyles.linkLabel, GUILayout.MinWidth(150)))
                {
                    EditorGUIUtility.PingObject(entry.clip);
                }

                EditorGUILayout.LabelField($"{entry.duration:F1}s", GUILayout.Width(60));
                EditorGUILayout.LabelField($"{entry.frequency}Hz", EditorStyles.miniLabel, GUILayout.Width(55));

                // Highlight stereo
                if (entry.channels > 1)
                {
                    var origColor = GUI.color;
                    GUI.color = new Color(0.9f, 0.7f, 0.1f);
                    EditorGUILayout.LabelField($"{entry.channels}", GUILayout.Width(30));
                    GUI.color = origColor;
                }
                else
                {
                    EditorGUILayout.LabelField($"{entry.channels}", GUILayout.Width(30));
                }

                EditorGUILayout.LabelField(entry.compressionFormat, EditorStyles.miniLabel, GUILayout.Width(70));
                EditorGUILayout.LabelField($"{entry.quality:F0}%", EditorStyles.miniLabel, GUILayout.Width(50));

                string diskStr = entry.fileSizeBytes >= 1048576
                    ? $"{entry.fileSizeBytes / 1048576f:F2} MB"
                    : $"{entry.fileSizeBytes / 1024f:F1} KB";
                EditorGUILayout.LabelField(diskStr, GUILayout.Width(70));

                string rtStr = entry.runtimeSizeBytes >= 1048576
                    ? $"{entry.runtimeSizeBytes / 1048576f:F2} MB"
                    : $"{entry.runtimeSizeBytes / 1024f:F1} KB";
                EditorGUILayout.LabelField(rtStr, GUILayout.Width(70));

                EditorGUILayout.EndHorizontal();

                // Separator line
                var lineRect = EditorGUILayout.GetControlRect(false, 1);
                EditorGUI.DrawRect(lineRect, new Color(0.3f, 0.3f, 0.3f));
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region Batch Panel

        private void DrawBatchPanel()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Batch Optimize Selected Clips", EditorStyles.boldLabel);

            batchForceToMono = EditorGUILayout.Toggle("Force to Mono", batchForceToMono);
            batchSampleRate = EditorGUILayout.IntPopup("Sample Rate",
                batchSampleRate,
                new[] { "8000 Hz", "11025 Hz", "22050 Hz", "44100 Hz" },
                new[] { 8000, 11025, 22050, 44100 });
            batchCompression = (AudioCompressionFormat)EditorGUILayout.EnumPopup("Compression", batchCompression);
            batchQuality = EditorGUILayout.Slider("Quality", batchQuality, 0f, 1f);

            int selectedCount = filteredEntries.Count(e => e.selected);
            EditorGUILayout.Space(4);

            EditorGUI.BeginDisabledGroup(selectedCount == 0);
            if (GUILayout.Button($"Apply to {selectedCount} selected clip(s)"))
            {
                ApplyBatchOptimize();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
        }

        private void ApplyBatchOptimize()
        {
            var selected = filteredEntries.Where(e => e.selected).ToList();
            if (selected.Count == 0) return;

            if (!EditorUtility.DisplayDialog("Audio Optimizer",
                $"Apply optimization to {selected.Count} clip(s)?\n\n" +
                $"Mono: {batchForceToMono}\n" +
                $"Sample Rate: {batchSampleRate} Hz\n" +
                $"Compression: {batchCompression}\n" +
                $"Quality: {batchQuality:F2}",
                "Apply", "Cancel"))
                return;

            foreach (var entry in selected)
            {
                var importer = AssetImporter.GetAtPath(entry.assetPath) as AudioImporter;
                if (importer == null) continue;

                importer.forceToMono = batchForceToMono;

                var settings = importer.defaultSampleSettings;
                settings.compressionFormat = batchCompression;
                settings.quality = batchQuality;
                settings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
                settings.sampleRateOverride = (uint)batchSampleRate;
                importer.defaultSampleSettings = settings;

                importer.SaveAndReimport();
            }

            EditorUtility.DisplayDialog("Audio Optimizer", $"Optimized {selected.Count} clip(s). Reimporting...", "OK");
            ScanAudio(); // rescan
        }

        #endregion

        #region Scan Logic

        private void ScanAudio()
        {
            allEntries.Clear();

            string[] paths;
            if (scanScope == AudioScanScope.Scene)
            {
                string scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
                if (string.IsNullOrEmpty(scenePath))
                {
                    EditorUtility.DisplayDialog("Audio Optimizer", "Please save the scene first.", "OK");
                    return;
                }
                paths = AssetDatabase.GetDependencies(scenePath, true)
                    .Where(IsAudioFile).ToArray();
            }
            else
            {
                paths = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets" })
                    .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            }

            foreach (var path in paths)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) continue;

                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                var settings = importer != null ? importer.defaultSampleSettings : new AudioImporterSampleSettings();

                allEntries.Add(new AudioEntry
                {
                    clip = clip,
                    assetPath = path,
                    assetName = Path.GetFileName(path),
                    duration = clip.length,
                    frequency = clip.frequency,
                    channels = clip.channels,
                    fileSizeBytes = GetFileSize(path),
                    runtimeSizeBytes = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(clip),
                    loadType = importer != null ? importer.loadInBackground ? "Background" : "Decompress" : "Unknown",
                    compressionFormat = settings.compressionFormat.ToString(),
                    quality = settings.quality * 100f
                });
            }

            allEntries.Sort((a, b) => b.fileSizeBytes.CompareTo(a.fileSizeBytes));
            ApplyFilter();
            Repaint();
        }

        private bool IsAudioFile(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".mp3" || ext == ".ogg" || ext == ".wav" || ext == ".aiff" || ext == ".flac";
        }

        #endregion

        #region Utilities

        private void ApplyFilter()
        {
            filteredEntries = allEntries.Where(e =>
            {
                if (showStereoOnly && e.channels <= 1) return false;
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

        #endregion

        #region CSV Export

        private void ExportCSV()
        {
            string path = EditorUtility.SaveFilePanel("Export Audio Report", "", "AudioReport", "csv");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                using (var writer = new StreamWriter(path))
                {
                    writer.WriteLine("Name,Duration(s),Frequency(Hz),Channels,Format,Quality(%),Size(KB)");
                    foreach (var e in filteredEntries)
                    {
                        float sizeKB = e.fileSizeBytes / 1024f;
                        writer.WriteLine($"\"{e.assetName}\",{e.duration:F1},{e.frequency},{e.channels},\"{e.compressionFormat}\",{e.quality:F0},{sizeKB:F1}");
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
