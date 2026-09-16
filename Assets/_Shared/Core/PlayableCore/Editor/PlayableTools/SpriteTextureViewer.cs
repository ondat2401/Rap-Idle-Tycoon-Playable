using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amanotes.Core;

namespace Amanotes.Core.Editor
{
    public class SpriteTextureViewer : EditorWindow
    {
        #region Nested Types

        [Flags]
        public enum SourceTypeFlags
        {
            None = 0,
            SpriteRenderer = 1 << 0,
            Image = 1 << 1,
            RawImage = 1 << 2,
            Material = 1 << 3,
            ParticleSystem = 1 << 4,
            All = ~0
        }

        public enum SortMode
        {
            NameAsc,
            NameDesc,
            FileSizeDesc,
            FileSizeAsc,
            PixelSizeDesc,
            UsageCountDesc
        }

        public class TextureEntry
        {
            public Texture texture;
            public string assetName;
            public string assetPath;
            public int width;
            public int height;
            public string format;
            public long fileSizeBytes;
            public long runtimeSizeBytes;
            public string compression;
            public SourceTypeFlags sourceTypes;
            public List<GameObject> usedByGameObjects = new List<GameObject>();
        }

        #endregion

        #region Fields

        private List<TextureEntry> allEntries = new List<TextureEntry>();
        private List<TextureEntry> filteredEntries = new List<TextureEntry>();

        private string searchText = "";
        private SourceTypeFlags sourceFilter = SourceTypeFlags.All;
        private SortMode sortMode = SortMode.FileSizeDesc;
        private Vector2 scrollPos;

        // Styles cached
        private GUIStyle headerStyle;
        private GUIStyle statsStyle;
        private bool stylesInitialized;

        #endregion

        #region Window Setup

        [MenuItem("Tools/Playable Standard Pipeline/Playable Tools/Sprite Texture Viewer")]
        public static void ShowWindow()
        {
            var window = GetWindow<SpriteTextureViewer>("Sprite/Texture Viewer");
            window.minSize = new Vector2(600, 450);
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;
            headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
            statsStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(8, 8, 4, 4) };
            stylesInitialized = true;
        }

        #endregion

        #region OnGUI

        private void OnGUI()
        {
            InitStyles();
            DrawToolbar();
            DrawStatistics();
            DrawFilterBar();

            if (allEntries.Count == 0)
            {
                EditorGUILayout.HelpBox("No sprites or textures found in current scene.\nClick 'Scan Scene' to start.", MessageType.Info);
                return;
            }

            if (filteredEntries.Count == 0)
            {
                EditorGUILayout.HelpBox("No results match current filter.", MessageType.Info);
                return;
            }

            // Showing X/Y
            EditorGUILayout.LabelField($"Showing {filteredEntries.Count}/{allEntries.Count}", EditorStyles.miniLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            foreach (var entry in filteredEntries)
            {
                DrawEntry(entry);
            }
            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region Toolbar

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Scan Scene", EditorStyles.toolbarButton, GUILayout.Width(90)))
            {
                ScanScene();
            }

            GUILayout.FlexibleSpace();

            // Sort dropdown
            EditorGUILayout.LabelField("Sort:", GUILayout.Width(32));
            var newSort = (SortMode)EditorGUILayout.EnumPopup(sortMode, EditorStyles.toolbarPopup, GUILayout.Width(130));
            if (newSort != sortMode)
            {
                sortMode = newSort;
                ApplyFilterAndSort();
            }

            if (GUILayout.Button("Export CSV", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                ExportCSV();
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Statistics

        private void DrawStatistics()
        {
            if (allEntries.Count == 0) return;

            EditorGUILayout.BeginVertical(statsStyle);

            long totalDiskSize = 0;
            long totalRuntimeSize = 0;
            int sprCount = 0, imgCount = 0, rawCount = 0, matCount = 0, psCount = 0;

            foreach (var e in filteredEntries)
            {
                totalDiskSize += e.fileSizeBytes;
                totalRuntimeSize += e.runtimeSizeBytes;
                if ((e.sourceTypes & SourceTypeFlags.SpriteRenderer) != 0) sprCount++;
                if ((e.sourceTypes & SourceTypeFlags.Image) != 0) imgCount++;
                if ((e.sourceTypes & SourceTypeFlags.RawImage) != 0) rawCount++;
                if ((e.sourceTypes & SourceTypeFlags.Material) != 0) matCount++;
                if ((e.sourceTypes & SourceTypeFlags.ParticleSystem) != 0) psCount++;
            }

            EditorGUILayout.LabelField(
                $"Unique: {filteredEntries.Count}  |  Disk: {FormatSize(totalDiskSize)}  |  Runtime: {FormatSize(totalRuntimeSize)}  |  " +
                $"Spr:{sprCount}  Img:{imgCount}  Raw:{rawCount}  Mat:{matCount}  PS:{psCount}",
                EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Filter Bar

        private void DrawFilterBar()
        {
            EditorGUILayout.BeginHorizontal();

            // Search field
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            var newSearch = EditorGUILayout.TextField(searchText, GUILayout.MinWidth(120));
            if (newSearch != searchText)
            {
                searchText = newSearch;
                ApplyFilterAndSort();
            }

            GUILayout.Space(10);

            // Source type toggles
            DrawSourceToggle("Spr", SourceTypeFlags.SpriteRenderer);
            DrawSourceToggle("Img", SourceTypeFlags.Image);
            DrawSourceToggle("Raw", SourceTypeFlags.RawImage);
            DrawSourceToggle("Mat", SourceTypeFlags.Material);
            DrawSourceToggle("PS", SourceTypeFlags.ParticleSystem);

            GUILayout.Space(13); // scrollbar compensation
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSourceToggle(string label, SourceTypeFlags flag)
        {
            bool isOn = (sourceFilter & flag) != 0;
            bool newOn = GUILayout.Toggle(isOn, label, EditorStyles.toolbarButton, GUILayout.Width(36));
            if (newOn != isOn)
            {
                if (newOn)
                    sourceFilter |= flag;
                else
                    sourceFilter &= ~flag;
                ApplyFilterAndSort();
            }
        }

        #endregion

        #region Draw Entry

        private void DrawEntry(TextureEntry entry)
        {
            EditorGUILayout.BeginHorizontal("box");

            // Preview thumbnail 64x64
            var preview = AssetPreview.GetAssetPreview(entry.texture)
                          ?? AssetPreview.GetMiniThumbnail(entry.texture);
            if (preview != null)
            {
                if (GUILayout.Button(preview, GUIStyle.none, GUILayout.Width(64), GUILayout.Height(64)))
                {
                    EditorGUIUtility.PingObject(entry.texture);
                }
            }
            else
            {
                GUILayout.Space(64);
            }

            // Info column
            EditorGUILayout.BeginVertical();

            // Row 1: Name (clickable to ping asset)
            if (GUILayout.Button(entry.assetName, EditorStyles.linkLabel))
            {
                EditorGUIUtility.PingObject(entry.texture);
            }

            // Row 2: Dimensions, format, disk size, runtime size, compression
            string diskStr = FormatSize(entry.fileSizeBytes);
            string runtimeStr = FormatSize(entry.runtimeSizeBytes);

            EditorGUILayout.LabelField(
                $"{entry.width}x{entry.height}  |  {entry.format}  |  Disk: {diskStr}  |  Runtime: {runtimeStr}  |  {entry.compression}",
                EditorStyles.miniLabel);

            // Row 3: Source types
            EditorGUILayout.LabelField($"Source: {FormatSourceTypes(entry.sourceTypes)}", EditorStyles.miniLabel);

            // Row 4: GameObjects with individual Ping buttons
            if (entry.usedByGameObjects.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Used by:", GUILayout.Width(50));

                foreach (var go in entry.usedByGameObjects)
                {
                    if (go == null) continue;
                    if (GUILayout.Button(go.name, EditorStyles.miniButton, GUILayout.MaxWidth(140)))
                    {
                        Selection.activeGameObject = go;
                        EditorGUIUtility.PingObject(go);
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private string FormatSourceTypes(SourceTypeFlags flags)
        {
            var parts = new List<string>();
            if ((flags & SourceTypeFlags.SpriteRenderer) != 0) parts.Add("SpriteRenderer");
            if ((flags & SourceTypeFlags.Image) != 0) parts.Add("Image");
            if ((flags & SourceTypeFlags.RawImage) != 0) parts.Add("RawImage");
            if ((flags & SourceTypeFlags.Material) != 0) parts.Add("Material");
            if ((flags & SourceTypeFlags.ParticleSystem) != 0) parts.Add("ParticleSystem");
            return string.Join(", ", parts);
        }

        private string FormatSize(long bytes)
        {
            if (bytes >= 1048576) return $"{bytes / 1048576f:F2} MB";
            if (bytes >= 1024) return $"{bytes / 1024f:F1} KB";
            return $"{bytes} B";
        }

        #endregion

        #region Scene Scanner

        private void ScanScene()
        {
            var entryMap = new Dictionary<string, TextureEntry>();

            // Scan SpriteRenderers
            foreach (var sr in FindAllInScene<SpriteRenderer>())
            {
                if (sr.sprite == null || sr.sprite.texture == null) continue;
                AddTexture(entryMap, sr.sprite.texture, sr.gameObject, SourceTypeFlags.SpriteRenderer);
            }

            // Scan UI Images
            foreach (var img in FindAllInScene<Image>())
            {
                if (img.sprite == null || img.sprite.texture == null) continue;
                AddTexture(entryMap, img.sprite.texture, img.gameObject, SourceTypeFlags.Image);
            }

            // Scan RawImages
            foreach (var raw in FindAllInScene<RawImage>())
            {
                if (raw.texture == null) continue;
                AddTexture(entryMap, raw.texture, raw.gameObject, SourceTypeFlags.RawImage);
            }

            // Scan Renderers (material textures)
            foreach (var renderer in FindAllInScene<Renderer>())
            {
                if (renderer is SpriteRenderer) continue; // already handled
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat == null) continue;
                    foreach (var propName in mat.GetTexturePropertyNames())
                    {
                        var tex = mat.GetTexture(propName);
                        if (tex == null) continue;
                        AddTexture(entryMap, tex, renderer.gameObject, SourceTypeFlags.Material);
                    }
                }
            }

            // Scan ParticleSystemRenderers
            foreach (var psr in FindAllInScene<ParticleSystemRenderer>())
            {
                if (psr.sharedMaterial == null || psr.sharedMaterial.mainTexture == null) continue;
                AddTexture(entryMap, psr.sharedMaterial.mainTexture, psr.gameObject, SourceTypeFlags.ParticleSystem);
            }

            allEntries = new List<TextureEntry>(entryMap.Values);
            ApplyFilterAndSort();
            Repaint();
        }

        private T[] FindAllInScene<T>() where T : Component
        {
            return Resources.FindObjectsOfTypeAll<T>()
                .Where(c => c.gameObject.scene.isLoaded && !EditorUtility.IsPersistent(c))
                .ToArray();
        }

        private void AddTexture(Dictionary<string, TextureEntry> map, Texture tex, GameObject go, SourceTypeFlags source)
        {
            string path = AssetDatabase.GetAssetPath(tex);
            if (string.IsNullOrEmpty(path)) return; // skip runtime-created textures

            if (map.TryGetValue(path, out var existing))
            {
                existing.sourceTypes |= source;
                if (!existing.usedByGameObjects.Contains(go))
                    existing.usedByGameObjects.Add(go);
            }
            else
            {
                var entry = new TextureEntry
                {
                    texture = tex,
                    assetName = Path.GetFileName(path),
                    assetPath = path,
                    width = tex.width,
                    height = tex.height,
                    format = GetTextureFormat(tex),
                    fileSizeBytes = GetFileSize(path),
                    runtimeSizeBytes = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(tex),
                    compression = GetCompression(path),
                    sourceTypes = source,
                    usedByGameObjects = new List<GameObject> { go }
                };
                map[path] = entry;
            }
        }

        private string GetTextureFormat(Texture tex)
        {
            var tex2d = tex as Texture2D;
            if (tex2d != null) return tex2d.format.ToString();
            return tex.GetType().Name;
        }

        private long GetFileSize(string assetPath)
        {
            try
            {
                var fullPath = Path.Combine(Application.dataPath, "..", assetPath);
                var fi = new FileInfo(fullPath);
                return fi.Exists ? fi.Length : 0;
            }
            catch
            {
                return 0;
            }
        }

        private string GetCompression(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return "Unknown";

            if (importer.textureCompression == TextureImporterCompression.Uncompressed)
                return "None";

            if (importer.crunchedCompression)
                return "Crunched";

            return "Compressed";
        }

        #endregion

        #region Filter and Sort

        private void ApplyFilterAndSort()
        {
            filteredEntries = allEntries.Where(e =>
            {
                // Text search filter
                if (!string.IsNullOrEmpty(searchText) &&
                    e.assetName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;

                // Source type filter
                if ((e.sourceTypes & sourceFilter) == 0)
                    return false;

                return true;
            }).ToList();

            // Sort
            switch (sortMode)
            {
                case SortMode.NameAsc:
                    filteredEntries.Sort((a, b) => string.Compare(a.assetName, b.assetName, StringComparison.OrdinalIgnoreCase));
                    break;
                case SortMode.NameDesc:
                    filteredEntries.Sort((a, b) => string.Compare(b.assetName, a.assetName, StringComparison.OrdinalIgnoreCase));
                    break;
                case SortMode.FileSizeDesc:
                    filteredEntries.Sort((a, b) => b.fileSizeBytes.CompareTo(a.fileSizeBytes));
                    break;
                case SortMode.FileSizeAsc:
                    filteredEntries.Sort((a, b) => a.fileSizeBytes.CompareTo(b.fileSizeBytes));
                    break;
                case SortMode.PixelSizeDesc:
                    filteredEntries.Sort((a, b) => (b.width * b.height).CompareTo(a.width * a.height));
                    break;
                case SortMode.UsageCountDesc:
                    filteredEntries.Sort((a, b) => b.usedByGameObjects.Count.CompareTo(a.usedByGameObjects.Count));
                    break;
            }
        }

        #endregion

        #region CSV Export

        private void ExportCSV()
        {
            if (filteredEntries.Count == 0)
            {
                EditorUtility.DisplayDialog("Export CSV", "No data to export. Scan scene first.", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanel("Export Sprite/Texture Report", "", "SpriteTextureReport", "csv");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                using (var writer = new StreamWriter(path))
                {
                    writer.WriteLine("Name,Width,Height,Format,FileSize(KB),Compression,SourceType,GameObjects");

                    foreach (var e in filteredEntries)
                    {
                        string goNames = string.Join(";",
                            e.usedByGameObjects.Where(g => g != null).Select(g => g.name));
                        string sources = FormatSourceTypes(e.sourceTypes);
                        float sizeKB = e.fileSizeBytes / 1024f;

                        writer.WriteLine($"\"{e.assetName}\",{e.width},{e.height},\"{e.format}\",{sizeKB:F1},\"{e.compression}\",\"{sources}\",\"{goNames}\"");
                    }
                }

                EditorUtility.DisplayDialog("Export CSV", $"Exported successfully to:\n{path}", "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Export CSV", $"Export failed:\n{ex.Message}", "OK");
            }
        }

        #endregion
    }
}
