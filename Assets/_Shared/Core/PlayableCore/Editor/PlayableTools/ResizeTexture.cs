using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

namespace Amanotes.Core.Editor
{
    /// <summary>
    /// Clean texture resize/optimize tool.
    /// - Scan folder for textures, show accurate size review
    /// - Resize (pad to multiple of 4) + crunch compress
    /// - Allow resize even if size won't decrease
    /// - Cache all settings via EditorPrefs
    /// </summary>
    public class ResizeTexture : EditorWindow
    {
        #region Data

        private struct Entry
        {
            public Texture2D texture;
            public string path;
            public int width, height;
            public int newWidth, newHeight;
            public long originalBytes;
            public long estimatedBytes;
            public bool selected;
        }

        #endregion

        #region EditorPrefs Keys

        private const string PP = "ResizeTex_";
        private const string PP_FOLDER = PP + "Folder";
        private const string PP_QUALITY = PP + "Quality";
        private const string PP_BACKUP = PP + "Backup";

        #endregion

        #region Fields

        private List<Entry> entries = new List<Entry>();
        private Vector2 scroll;
        private string scanFolder = "Assets";
        private int compressionQuality = 100;
        private bool autoBackup = true;
        private bool settingsLoaded = false;
        private bool selectAll = false;

        #endregion

        #region Window

        [MenuItem("Tools/Playable Standard Pipeline/Resize Texture")]
        public static void ShowWindow()
        {
            var w = GetWindow<ResizeTexture>("Resize Texture");
            w.minSize = new Vector2(520, 400);
        }

        private void OnEnable() { LoadSettings(); }
        private void OnDisable() { SaveSettings(); }

        private void LoadSettings()
        {
            scanFolder = EditorPrefs.GetString(PP_FOLDER, "Assets");
            compressionQuality = EditorPrefs.GetInt(PP_QUALITY, 100);
            autoBackup = EditorPrefs.GetBool(PP_BACKUP, true);
            settingsLoaded = true;
        }

        private void SaveSettings()
        {
            EditorPrefs.SetString(PP_FOLDER, scanFolder);
            EditorPrefs.SetInt(PP_QUALITY, compressionQuality);
            EditorPrefs.SetBool(PP_BACKUP, autoBackup);
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            if (!settingsLoaded) LoadSettings();

            DrawSettings();
            GUILayout.Space(4);
            DrawSummary();
            GUILayout.Space(2);
            DrawList();
            GUILayout.Space(4);
            DrawActions();
        }

        #endregion

        #region Settings Panel

        private void DrawSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Row 1: Folder
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Folder", GUILayout.Width(45));
            var newFolder = EditorGUILayout.TextField(scanFolder);
            if (newFolder != scanFolder) { scanFolder = newFolder; SaveSettings(); }
            if (GUILayout.Button("...", GUILayout.Width(28)))
            {
                string sel = EditorUtility.OpenFolderPanel("Scan Folder", scanFolder, "");
                if (!string.IsNullOrEmpty(sel))
                {
                    string dp = Application.dataPath;
                    scanFolder = sel.StartsWith(dp) ? "Assets" + sel.Substring(dp.Length) : sel;
                    SaveSettings();
                }
            }
            if (GUILayout.Button("Scan", GUILayout.Width(50)))
            {
                ScanTextures();
            }
            EditorGUILayout.EndHorizontal();

            // Row 2: Quality + Backup
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Quality", GUILayout.Width(45));
            int newQ = EditorGUILayout.IntSlider(compressionQuality, 0, 100);
            if (newQ != compressionQuality) { compressionQuality = newQ; SaveSettings(); RecalcEstimates(); }
            GUILayout.Space(8);
            bool newBk = EditorGUILayout.ToggleLeft("Backup", autoBackup, GUILayout.Width(62));
            if (newBk != autoBackup) { autoBackup = newBk; SaveSettings(); }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Summary

        private void DrawSummary()
        {
            if (entries.Count == 0) return;

            int selCount = 0;
            long selOriginal = 0, selEstimated = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].selected)
                {
                    selCount++;
                    selOriginal += entries[i].originalBytes;
                    selEstimated += entries[i].estimatedBytes;
                }
            }
            long diff = selOriginal - selEstimated;

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField(
                string.Format("Total: {0}  |  Selected: {1}  |  Before: {2}  |  After: {3}  |  Save: {4}",
                    entries.Count, selCount, FmtSize(selOriginal), FmtSize(selEstimated),
                    diff >= 0 ? "-" + FmtSize(diff) : "+" + FmtSize(-diff)),
                EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region List

        private void DrawList()
        {
            if (entries.Count == 0)
            {
                EditorGUILayout.HelpBox("Click 'Scan' to find textures, or drag & drop below.", MessageType.Info);
                DrawDropZone();
                return;
            }

            // Header
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            bool newAll = EditorGUILayout.Toggle(selectAll, GUILayout.Width(18));
            if (newAll != selectAll) { selectAll = newAll; SetAllSelected(selectAll); }
            EditorGUILayout.LabelField("Texture", EditorStyles.miniBoldLabel, GUILayout.MinWidth(120));
            EditorGUILayout.LabelField("Size", EditorStyles.miniBoldLabel, GUILayout.Width(70));
            EditorGUILayout.LabelField("→ New", EditorStyles.miniBoldLabel, GUILayout.Width(70));
            EditorGUILayout.LabelField("Original", EditorStyles.miniBoldLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("Est.", EditorStyles.miniBoldLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("Diff", EditorStyles.miniBoldLabel, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            scroll = EditorGUILayout.BeginScrollView(scroll);

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e.texture == null) continue;

                EditorGUILayout.BeginHorizontal();

                // Select toggle
                bool sel = EditorGUILayout.Toggle(e.selected, GUILayout.Width(18));
                if (sel != e.selected) { e.selected = sel; entries[i] = e; }

                // Texture name (click to ping)
                if (GUILayout.Button(e.texture.name, EditorStyles.linkLabel, GUILayout.MinWidth(120)))
                    EditorGUIUtility.PingObject(e.texture);

                // Dimensions
                EditorGUILayout.LabelField($"{e.width}x{e.height}", EditorStyles.miniLabel, GUILayout.Width(70));
                EditorGUILayout.LabelField($"{e.newWidth}x{e.newHeight}", EditorStyles.miniLabel, GUILayout.Width(70));

                // Size columns
                EditorGUILayout.LabelField(FmtSize(e.originalBytes), EditorStyles.miniLabel, GUILayout.Width(60));
                EditorGUILayout.LabelField(FmtSize(e.estimatedBytes), EditorStyles.miniLabel, GUILayout.Width(60));

                // Diff with color
                long d = e.estimatedBytes - e.originalBytes;
                var oc = GUI.color;
                GUI.color = d < 0 ? new Color(0.3f, 0.9f, 0.3f) : (d > 0 ? new Color(1f, 0.6f, 0.2f) : Color.gray);
                string ds = d <= 0 ? FmtSize(d) : "+" + FmtSize(d);
                EditorGUILayout.LabelField(ds, EditorStyles.miniLabel, GUILayout.Width(60));
                GUI.color = oc;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            DrawDropZone();
        }

        private void DrawDropZone()
        {
            GUILayout.Space(2);
            Rect dropRect = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "Drop Textures Here", EditorStyles.helpBox);
            HandleDrop(dropRect);
        }

        #endregion

        #region Actions

        private void DrawActions()
        {
            int selCount = entries.Count(e => e.selected);

            EditorGUILayout.BeginHorizontal();

            // Optimize button
            GUI.enabled = selCount > 0;
            if (GUILayout.Button($"Optimize {selCount} Textures", GUILayout.Height(28)))
            {
                long totalOrig = entries.Where(e => e.selected).Sum(e => e.originalBytes);
                long totalEst = entries.Where(e => e.selected).Sum(e => e.estimatedBytes);
                long saved = totalOrig - totalEst;
                string msg = string.Format(
                    "Resize + Crunch {0} textures?\nEstimated save: {1}\nBackup: {2}",
                    selCount, saved >= 0 ? FmtSize(saved) : "+" + FmtSize(-saved),
                    autoBackup ? "ON" : "OFF");
                if (EditorUtility.DisplayDialog("Resize Texture", msg, "Go", "Cancel"))
                    DoOptimize();
            }
            GUI.enabled = true;

            // Restore button
            if (GUILayout.Button("Restore Backup", GUILayout.Width(105), GUILayout.Height(28)))
                RestoreBackup();

            // Clear
            if (GUILayout.Button("Clear", GUILayout.Width(50), GUILayout.Height(28)))
            {
                entries.Clear();
                selectAll = false;
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Scan

        private void ScanTextures()
        {
            entries.Clear();
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { scanFolder });

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (i % 50 == 0)
                    EditorUtility.DisplayProgressBar("Scanning", path, (float)i / guids.Length);

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex == null) continue;

                AddEntry(tex, path);
            }

            EditorUtility.ClearProgressBar();
            entries.Sort((a, b) => b.originalBytes.CompareTo(a.originalBytes));
            Repaint();
        }

        private void AddEntry(Texture2D tex, string path)
        {
            int nw = tex.width, nh = tex.height;
            while (nw % 4 != 0) nw++;
            while (nh % 4 != 0) nh++;

            long origBytes = GetFileSize(path);
            long estBytes = EstimateSize(tex.width, tex.height, nw, nh, origBytes);

            entries.Add(new Entry
            {
                texture = tex,
                path = path,
                width = tex.width,
                height = tex.height,
                newWidth = nw,
                newHeight = nh,
                originalBytes = origBytes,
                estimatedBytes = estBytes,
                selected = true
            });
        }

        #endregion

        #region Size Estimation

        /// <summary>
        /// Estimate compressed file size after resize + crunch.
        /// Uses pixel ratio and compression quality to estimate.
        /// </summary>
        private long EstimateSize(int origW, int origH, int newW, int newH, long currentFileSize)
        {
            // Base: bytes per pixel from current file
            long origPixels = (long)origW * origH;
            long newPixels = (long)newW * newH;

            if (origPixels <= 0 || currentFileSize <= 0)
                return 1024L;

            float currentBpp = (float)currentFileSize / origPixels;

            // Crunched compression ratio: quality 0=very small, 100=larger
            float crunchFactor = Mathf.Lerp(0.12f, 0.55f, compressionQuality / 100f);

            // New estimated size = newPixels * currentBpp * crunchFactor
            long estimated = (long)(newPixels * currentBpp * crunchFactor);

            // Floor
            return System.Math.Max(estimated, 512L);
        }

        private void RecalcEstimates()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                e.estimatedBytes = EstimateSize(e.width, e.height, e.newWidth, e.newHeight, e.originalBytes);
                entries[i] = e;
            }
            Repaint();
        }

        #endregion

        #region Optimize

        private void DoOptimize()
        {
            var selected = entries.Where(e => e.selected).ToList();
            int success = 0;
            long beforeTotal = 0, afterTotal = 0;

            for (int i = 0; i < selected.Count; i++)
            {
                var e = selected[i];
                EditorUtility.DisplayProgressBar("Optimizing",
                    string.Format("{0} ({1}/{2})", e.texture.name, i + 1, selected.Count),
                    (float)i / selected.Count);

                try
                {
                    var importer = AssetImporter.GetAtPath(e.path) as TextureImporter;
                    if (importer == null) continue;

                    FileInfo fi = new FileInfo(e.path);
                    beforeTotal += fi.Length;

                    // Ensure readable
                    if (!importer.isReadable)
                    {
                        importer.isReadable = true;
                        AssetDatabase.ImportAsset(e.path, ImportAssetOptions.ForceUpdate);
                    }

                    // Resize to multiple of 4
                    Texture2D resized = new Texture2D(e.newWidth, e.newHeight, TextureFormat.ARGB32, false);
                    CopyPixels(e.texture, resized);

                    // Backup
                    if (autoBackup) BackupFile(e.path);

                    // Write PNG
                    File.WriteAllBytes(e.path, resized.EncodeToPNG());
                    DestroyImmediate(resized);

                    // Apply compression
                    importer.textureType = TextureImporterType.Sprite;
                    importer.textureCompression = TextureImporterCompression.Compressed;
                    importer.crunchedCompression = true;
                    importer.compressionQuality = compressionQuality;
                    importer.SaveAndReimport();

                    fi = new FileInfo(e.path);
                    afterTotal += fi.Length;
                    success++;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[ResizeTexture] {e.texture.name}: {ex.Message}");
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            float savedMB = (beforeTotal - afterTotal) / (1024f * 1024f);
            EditorUtility.DisplayDialog("Done",
                string.Format("Optimized {0}/{1} textures.\nSaved: {2:F2} MB", success, selected.Count, savedMB),
                "OK");

            // Rescan to refresh sizes
            ScanTextures();
        }

        private void CopyPixels(Texture2D src, Texture2D dst)
        {
            Color32[] srcPx = src.GetPixels32();
            Color32[] dstPx = new Color32[dst.width * dst.height];
            Color32 clear = new Color32(0, 0, 0, 0);
            int sw = src.width, sh = src.height, dw = dst.width;

            for (int y = 0; y < dst.height; y++)
            {
                for (int x = 0; x < dst.width; x++)
                {
                    dstPx[y * dw + x] = (x < sw && y < sh) ? srcPx[y * sw + x] : clear;
                }
            }
            dst.SetPixels32(dstPx);
            dst.Apply();
        }

        #endregion

        #region Backup & Restore

        private void BackupFile(string assetPath)
        {
            string backupDir = "Assets/BackupTexture";
            if (!Directory.Exists(backupDir))
                Directory.CreateDirectory(backupDir);

            string fileName = Path.GetFileName(assetPath);
            string dest = Path.Combine(backupDir, fileName);

            if (File.Exists(dest))
            {
                string name = Path.GetFileNameWithoutExtension(fileName);
                string ext = Path.GetExtension(fileName);
                int v = 1;
                while (File.Exists(Path.Combine(backupDir, $"{name}_v{v}{ext}"))) v++;
                dest = Path.Combine(backupDir, $"{name}_v{v}{ext}");
            }
            AssetDatabase.CopyAsset(assetPath, dest);
        }

        private void RestoreBackup()
        {
            string backupDir = "Assets/BackupTexture";
            if (!Directory.Exists(backupDir))
            {
                EditorUtility.DisplayDialog("Restore", "No backup folder found.", "OK");
                return;
            }

            string[] files = Directory.GetFiles(backupDir)
                .Where(f => !f.EndsWith(".meta")).ToArray();

            if (files.Length == 0)
            {
                EditorUtility.DisplayDialog("Restore", "No backup files found.", "OK");
                return;
            }

            if (EditorUtility.DisplayDialog("Restore",
                $"Found {files.Length} backup file(s).\nRestore all to original locations?",
                "Restore", "Cancel"))
            {
                int restored = 0;
                foreach (string backup in files)
                {
                    string fileName = Path.GetFileName(backup);
                    // Find matching asset in project
                    string[] matches = AssetDatabase.FindAssets(
                        Path.GetFileNameWithoutExtension(fileName) + " t:Texture2D");
                    if (matches.Length > 0)
                    {
                        string targetPath = AssetDatabase.GUIDToAssetPath(matches[0]);
                        File.Copy(backup, targetPath, true);
                        AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);
                        restored++;
                    }
                }
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Restore", $"Restored {restored} file(s).", "OK");
                if (entries.Count > 0) ScanTextures();
            }
        }

        #endregion
    

        #region Drag & Drop

        private void HandleDrop(Rect area)
        {
            Event evt = Event.current;
            if (!area.Contains(evt.mousePosition)) return;

            if (evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    foreach (Object obj in DragAndDrop.objectReferences)
                    {
                        var tex = obj as Texture2D;
                        if (tex == null) continue;
                        // Skip duplicates
                        bool exists = false;
                        for (int i = 0; i < entries.Count; i++)
                            if (entries[i].texture == tex) { exists = true; break; }
                        if (exists) continue;

                        string path = AssetDatabase.GetAssetPath(tex);
                        if (!string.IsNullOrEmpty(path))
                            AddEntry(tex, path);
                    }
                    Repaint();
                }
                evt.Use();
            }
        }

        #endregion

        #region Utilities

        private void SetAllSelected(bool val)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                e.selected = val;
                entries[i] = e;
            }
        }

        private long GetFileSize(string assetPath)
        {
            try
            {
                string full = Path.Combine(Application.dataPath, "..", assetPath);
                var fi = new FileInfo(full);
                return fi.Exists ? fi.Length : 0;
            }
            catch { return 0; }
        }

        private string FmtSize(long bytes)
        {
            if (bytes < 0) return "-" + FmtSize(-bytes);
            if (bytes >= 1048576) return string.Format("{0:F1}MB", bytes / 1048576f);
            if (bytes >= 1024) return string.Format("{0:F0}KB", bytes / 1024f);
            return string.Format("{0}B", bytes);
        }

        #endregion
    }
}
