using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amanotes.Core;

namespace Amanotes.Core.Editor
{
    /// <summary>
    /// Lists all shaders and their variants used in the current scene/project.
    /// Helps identify shader bloat for Luna Playable builds.
    /// </summary>
    public class ShaderVariantStripper : EditorWindow
    {
        #region Nested Types

        public class ShaderEntry
        {
            public Shader shader;
            public string shaderName;
            public string assetPath;
            public int passCount;
            public int variantCount; // estimated
            public long fileSizeBytes;
            public List<string> keywords = new List<string>();
            public List<string> usedByMaterials = new List<string>();
            public bool isBuiltIn;
        }

        #endregion

        #region Fields

        private List<ShaderEntry> allEntries = new List<ShaderEntry>();
        private List<ShaderEntry> filteredEntries = new List<ShaderEntry>();
        private Vector2 scrollPos;
        private string searchText = "";
        private bool showBuiltIn = false;
        private bool showKeywords = false;
        private int totalVariants;

        #endregion

        #region Window Setup

        [MenuItem("Tools/Playable Standard Pipeline/Playable Tools/Shader Variant Stripper")]
        public static void ShowWindow()
        {
            var window = GetWindow<ShaderVariantStripper>("Shader Variant Stripper");
            window.minSize = new Vector2(650, 450);
        }

        #endregion

        #region OnGUI

        private void OnGUI()
        {
            DrawToolbar();

            if (allEntries.Count == 0)
            {
                EditorGUILayout.HelpBox("Click 'Scan Scene Shaders' or 'Scan All Shaders' to analyze shader variants.", MessageType.Info);
                return;
            }

            DrawSummary();
            DrawFilterBar();
            DrawShaderList();
        }

        #endregion

        #region Toolbar

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Scan Scene Shaders", EditorStyles.toolbarButton, GUILayout.Width(130)))
            {
                ScanSceneShaders();
            }

            if (GUILayout.Button("Scan All Shaders", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                ScanAllShaders();
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

            int customCount = allEntries.Count(e => !e.isBuiltIn);
            int builtInCount = allEntries.Count(e => e.isBuiltIn);
            long totalSize = allEntries.Where(e => !e.isBuiltIn).Sum(e => e.fileSizeBytes);
            string sizeStr = totalSize >= 1048576 ? $"{totalSize / 1048576f:F2} MB" : $"{totalSize / 1024f:F1} KB";

            EditorGUILayout.LabelField(
                $"Shaders: {allEntries.Count} (Custom: {customCount}, Built-in: {builtInCount})  |  " +
                $"Est. Variants: {totalVariants}  |  Custom Size: {sizeStr}",
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

            var newBuiltIn = GUILayout.Toggle(showBuiltIn, "Show Built-in", EditorStyles.toolbarButton, GUILayout.Width(90));
            if (newBuiltIn != showBuiltIn) { showBuiltIn = newBuiltIn; ApplyFilter(); }

            showKeywords = GUILayout.Toggle(showKeywords, "Show Keywords", EditorStyles.toolbarButton, GUILayout.Width(100));

            GUILayout.Space(13); // scrollbar compensation
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Shader List

        private void DrawShaderList()
        {
            EditorGUILayout.LabelField($"Showing {filteredEntries.Count}/{allEntries.Count}", EditorStyles.miniLabel);

            // Header row
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("", GUILayout.Width(50)); // built-in tag
            EditorGUILayout.LabelField("Shader Name", EditorStyles.boldLabel, GUILayout.MinWidth(200));
            EditorGUILayout.LabelField("Passes", EditorStyles.boldLabel, GUILayout.Width(70));
            EditorGUILayout.LabelField("Variants", EditorStyles.boldLabel, GUILayout.Width(90));
            EditorGUILayout.LabelField("Mats", EditorStyles.boldLabel, GUILayout.Width(55));
            EditorGUILayout.LabelField("Size", EditorStyles.boldLabel, GUILayout.Width(70));
            GUILayout.Space(13);
            EditorGUILayout.EndHorizontal();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            foreach (var entry in filteredEntries)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();

                // Built-in indicator
                if (entry.isBuiltIn)
                {
                    var tagRect = EditorGUILayout.GetControlRect(false, 16, GUILayout.Width(50));
                    EditorGUI.DrawRect(tagRect, new Color(0.4f, 0.4f, 0.4f));
                    EditorGUI.LabelField(tagRect, "Built-in", new GUIStyle(EditorStyles.miniLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        normal = { textColor = Color.white }
                    });
                }

                // Shader name (clickable)
                if (GUILayout.Button(entry.shaderName, EditorStyles.linkLabel, GUILayout.MinWidth(200)))
                {
                    if (entry.shader != null)
                        EditorGUIUtility.PingObject(entry.shader);
                }

                // Pass count
                EditorGUILayout.LabelField($"Passes: {entry.passCount}", EditorStyles.miniLabel, GUILayout.Width(70));

                // Variant estimate
                EditorGUILayout.LabelField($"~{entry.variantCount} variants", EditorStyles.miniLabel, GUILayout.Width(90));

                // Materials using it
                EditorGUILayout.LabelField($"{entry.usedByMaterials.Count} mats", EditorStyles.miniLabel, GUILayout.Width(55));

                // Size
                if (!entry.isBuiltIn && entry.fileSizeBytes > 0)
                {
                    string sizeStr = entry.fileSizeBytes >= 1024
                        ? $"{entry.fileSizeBytes / 1024f:F1} KB"
                        : $"{entry.fileSizeBytes} B";
                    EditorGUILayout.LabelField(sizeStr, GUILayout.Width(70));
                }

                EditorGUILayout.EndHorizontal();

                // Keywords (collapsible)
                if (showKeywords && entry.keywords.Count > 0)
                {
                    string kwStr = string.Join(", ", entry.keywords);
                    EditorGUILayout.LabelField($"  Keywords: {kwStr}", EditorStyles.miniLabel);
                }

                // Materials list
                if (entry.usedByMaterials.Count > 0 && entry.usedByMaterials.Count <= 5)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    EditorGUILayout.LabelField("Materials:", GUILayout.Width(60));
                    foreach (var matPath in entry.usedByMaterials)
                    {
                        string matName = Path.GetFileNameWithoutExtension(matPath);
                        if (GUILayout.Button(matName, EditorStyles.miniButton, GUILayout.MaxWidth(120)))
                        {
                            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                            if (mat != null) EditorGUIUtility.PingObject(mat);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region Scan Logic

        private void ScanSceneShaders()
        {
            allEntries.Clear();
            var shaderMap = new Dictionary<string, ShaderEntry>();

            string scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            if (string.IsNullOrEmpty(scenePath))
            {
                EditorUtility.DisplayDialog("Shader Variant Stripper", "Please save the scene first.", "OK");
                return;
            }

            string[] deps = AssetDatabase.GetDependencies(scenePath, true);

            // Find all materials in scene dependencies
            foreach (var dep in deps)
            {
                if (!dep.EndsWith(".mat")) continue;
                var mat = AssetDatabase.LoadAssetAtPath<Material>(dep);
                if (mat == null || mat.shader == null) continue;

                AddShader(shaderMap, mat.shader, dep);
            }

            FinalizeEntries(shaderMap);
        }

        private void ScanAllShaders()
        {
            allEntries.Clear();
            var shaderMap = new Dictionary<string, ShaderEntry>();

            var matGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });

            EditorUtility.DisplayProgressBar("Shader Variant Stripper", "Scanning materials...", 0);

            for (int i = 0; i < matGuids.Length; i++)
            {
                if (i % 50 == 0)
                    EditorUtility.DisplayProgressBar("Shader Variant Stripper", $"Scanning {i}/{matGuids.Length}", (float)i / matGuids.Length);

                string matPath = AssetDatabase.GUIDToAssetPath(matGuids[i]);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null || mat.shader == null) continue;

                AddShader(shaderMap, mat.shader, matPath);
            }

            EditorUtility.ClearProgressBar();
            FinalizeEntries(shaderMap);
        }

        private void AddShader(Dictionary<string, ShaderEntry> map, Shader shader, string materialPath)
        {
            string key = shader.name;

            if (map.TryGetValue(key, out var existing))
            {
                if (!existing.usedByMaterials.Contains(materialPath))
                    existing.usedByMaterials.Add(materialPath);
                return;
            }

            string shaderPath = AssetDatabase.GetAssetPath(shader);
            bool isBuiltIn = string.IsNullOrEmpty(shaderPath) || shaderPath.StartsWith("Resources/") || !shaderPath.StartsWith("Assets/");

            var entry = new ShaderEntry
            {
                shader = shader,
                shaderName = shader.name,
                assetPath = shaderPath,
                passCount = shader.passCount,
                isBuiltIn = isBuiltIn,
                fileSizeBytes = isBuiltIn ? 0 : GetFileSize(shaderPath),
                usedByMaterials = new List<string> { materialPath }
            };

            // Collect keywords from shader
            CollectShaderKeywords(entry, shader);

            // Estimate variant count: 2^keywords * passes (rough estimate)
            int kwCount = entry.keywords.Count;
            entry.variantCount = entry.passCount * (kwCount > 0 ? (int)Math.Pow(2, Math.Min(kwCount, 10)) : 1);

            map[key] = entry;
        }

        private void CollectShaderKeywords(ShaderEntry entry, Shader shader)
        {
            // Use SerializedObject to read shader keywords
            try
            {
                var so = new SerializedObject(shader);
                var keywordsProp = so.FindProperty("m_CompileInfo");
                if (keywordsProp != null)
                {
                    // Fallback: read from shader source if available
                }

                // Alternative: get global keywords from ShaderUtil
                var localKeywords = shader.keywordSpace.keywordNames;
                if (localKeywords != null)
                {
                    entry.keywords.AddRange(localKeywords.Where(k => !string.IsNullOrEmpty(k)));
                }
            }
            catch
            {
                // Silently ignore - some shaders may not expose keywords
            }
        }

        private void FinalizeEntries(Dictionary<string, ShaderEntry> map)
        {
            allEntries = map.Values
                .OrderByDescending(e => e.variantCount)
                .ThenByDescending(e => e.usedByMaterials.Count)
                .ToList();

            totalVariants = allEntries.Sum(e => e.variantCount);
            ApplyFilter();
            Repaint();
        }

        #endregion

        #region Utilities

        private void ApplyFilter()
        {
            filteredEntries = allEntries.Where(e =>
            {
                if (!showBuiltIn && e.isBuiltIn) return false;
                if (!string.IsNullOrEmpty(searchText) &&
                    e.shaderName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) < 0)
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
            string path = EditorUtility.SaveFilePanel("Export Shader Report", "", "ShaderVariantReport", "csv");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                using (var writer = new StreamWriter(path))
                {
                    writer.WriteLine("ShaderName,IsBuiltIn,Passes,EstVariants,MaterialCount,Size(KB),Keywords");
                    foreach (var e in filteredEntries)
                    {
                        float sizeKB = e.fileSizeBytes / 1024f;
                        string kw = string.Join(";", e.keywords);
                        writer.WriteLine($"\"{e.shaderName}\",{e.isBuiltIn},{e.passCount},{e.variantCount},{e.usedByMaterials.Count},{sizeKB:F1},\"{kw}\"");
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
