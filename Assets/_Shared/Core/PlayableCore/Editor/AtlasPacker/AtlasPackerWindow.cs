using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using Amanotes.Core;

namespace Amanotes.Core.Editor
{
    /// <summary>
    /// Atlas Packer Tool - Pack textures into sprite atlas and auto-replace references
    /// Place in Editor folder
    /// </summary>
    public class AtlasPackerWindow : EditorWindow
    {
        #region Fields & Properties

        // ===== Core Settings =====
        private Vector2 mainScrollPos; // Tab content scroll    
        private Vector2 textureListScroll;
        private Vector2 replacementScrollPos;
        // Replacement list scrol
        private List<Texture2D> textures = new List<Texture2D>();
        private Vector2 scrollPos;
        private int padding = 2;
        private int maxSize = 2048;
        private string atlasName = "texture_atlas";
        private string saveFolder = "Assets";

        // ===== Advanced Settings =====
        private bool showAdvancedSettings = false;
        private TextureFormat atlasFormat = TextureFormat.RGBA32;
        private FilterMode filterMode = FilterMode.Bilinear;
        private int anisoLevel = 1;
        private bool generateMipMaps = false;
        private TextureImporterCompression compressionType = TextureImporterCompression.Compressed;

        // ===== Sprite Replacement Settings =====
        private bool autoReplaceSpriteReferences = true;
        private bool searchInScenes = true;
        private bool searchInPrefabs = true;
        private bool searchInAnimations = false;
        private bool searchCustomComponents = false;
        private bool createBackup = true;
        private bool showReplacementPreview = true;

        // ===== Preview =====
        private bool showPreview = false;
        private Texture2D previewTexture;

        // ===== Export Options =====
        private bool exportMetadata = false;

        // ===== Replacement Data =====
        private List<SpriteReplacement> pendingReplacements = new List<SpriteReplacement>();

        // ===== Reorderable List =====
        private ReorderableList reorderableList;

        // ===== EditorPrefs Keys =====
        private const string PREFS_PADDING = "AtlasPacker_Padding";
        private const string PREFS_MAX_SIZE = "AtlasPacker_MaxSize";
        private const string PREFS_ATLAS_NAME = "AtlasPacker_AtlasName";
        private const string PREFS_SAVE_FOLDER = "AtlasPacker_SaveFolder";
        private const string PREFS_AUTO_REPLACE = "AtlasPacker_AutoReplace";
        private const string PREFS_SEARCH_SCENES = "AtlasPacker_SearchScenes";
        private const string PREFS_SEARCH_PREFABS = "AtlasPacker_SearchPrefabs";


        private int currentTab = 0;
        private string[] tabNames = { "Settings", "Textures", "Preview", "Replace" };
        #endregion

        #region Nested Classes

        [Serializable]
        private class SpriteReplacement
        {
            public UnityEngine.Object targetObject;
            public Component targetComponent;
            public string componentType;
            public Sprite oldSprite;
            public Sprite newSprite;
            public string propertyPath;
            public bool isInScene;
            public string scenePath;
            public int arrayIndex = -1; // For array elements
        }

        [Serializable]
        public class AtlasMetadata
        {
            public string atlasName;
            public int width;
            public int height;
            public SpriteData[] sprites;
        }

        [Serializable]
        public class SpriteData
        {
            public string name;
            public int x, y, width, height;
        }

        #endregion

        #region Window Setup

        [MenuItem("Tools/Playable Standard Pipeline/Atlas Packer/Atlas Packer")]
        public static void ShowWindow()
        {
            AtlasPackerWindow window = GetWindow<AtlasPackerWindow>("Atlas Packer");
            window.minSize = new Vector2(450, 500);
            window.Show();
        }

        void OnEnable()
        {
            LoadPreferences();
            SetupReorderableList();
        }

        void OnDisable()
        {
            SavePreferences();
            CleanupPreview();
        }

        void OnDestroy()
        {
            CleanupPreview();
        }

        #endregion

        #region GUI
        void OnGUI()
        {
            // ===== TAB HEADER (FIXED) =====
            EditorGUILayout.LabelField("Texture Atlas Packer", EditorStyles.boldLabel);

            currentTab = GUILayout.Toolbar(currentTab, tabNames, GUILayout.Height(25));

            EditorGUILayout.Space(5);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));
            EditorGUILayout.Space(5);

            // ===== TAB CONTENT (SCROLLABLE) =====
            mainScrollPos = EditorGUILayout.BeginScrollView(mainScrollPos);

            switch (currentTab)
            {
                case 0: // Settings
                    DrawSettingsTab();
                    break;

                case 1: // Textures
                    DrawTexturesTab();
                    break;

                case 2: // Preview
                    DrawPreviewTab();
                    break;

                case 3: // Replace
                    DrawReplaceTab();
                    break;
            }

            EditorGUILayout.EndScrollView();

            // ===== ACTION BUTTONS (FIXED AT BOTTOM) =====
            EditorGUILayout.Space(5);
            GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));
            DrawActionButtons();
        }

        // ============================================
        // TAB CONTENT METHODS
        // ============================================

        void DrawSettingsTab()
        {
            DrawBasicSettings();
            EditorGUILayout.Space(10);
            DrawAdvancedSettings();
            EditorGUILayout.Space(10);
            DrawReplacementSettings();
            EditorGUILayout.Space(10);
            DrawHelpBox();
        }
        void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();
            string validationError = ValidateBeforePacking();
            EditorGUI.BeginDisabledGroup(!string.IsNullOrEmpty(validationError));
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("📦 Pack Atlas", GUILayout.Height(40)))
            {
                PackAndSaveAtlas();
                currentTab = 3;
                // Switch to Replace tab        
            }
            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            if (!string.IsNullOrEmpty(validationError))
            { EditorGUILayout.HelpBox(validationError, MessageType.Warning); }
        }
        void DrawTexturesTab()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Selected Textures", GUILayout.Height(25)))
            {
                AddSelectionTextures();
            }
            if (GUILayout.Button("Clear List", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Clear List", "Remove all textures from the list?", "Yes", "No"))
                {
                    textures.Clear();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Drag & Drop Area
            Rect dropArea = GUILayoutUtility.GetRect(0f, 50f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag & Drop Textures Here", EditorStyles.helpBox);
            HandleDragAndDrop(dropArea);

            EditorGUILayout.Space(10);

            // Texture statistics
            if (textures.Count > 0)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Statistics:", EditorStyles.boldLabel);

                int totalPixels = textures.Where(t => t != null).Sum(t => t.width * t.height);
                int estimatedSize = Mathf.NextPowerOfTwo((int)Mathf.Sqrt(totalPixels * 1.2f));

                EditorGUILayout.LabelField($"Total textures: {textures.Count}");
                EditorGUILayout.LabelField($"Estimated atlas: {estimatedSize}x{estimatedSize}");

                if (estimatedSize > maxSize)
                {
                    EditorGUILayout.HelpBox($"⚠️ May not fit in {maxSize}x{maxSize}!", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox($"✓ Will fit in {maxSize}x{maxSize}", MessageType.Info);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(10);

            // Reorderable List
            if (reorderableList != null)
            {
                reorderableList.DoLayoutList();
            }
        }

        void DrawPreviewTab()
        {
            EditorGUILayout.HelpBox("Generate a preview to see how your atlas will look before packing.", MessageType.Info);

            EditorGUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(textures.Count == 0);

            if (GUILayout.Button("Generate Preview", GUILayout.Height(35)))
            {
                GeneratePreview();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(10);

            if (previewTexture != null)
            {
                EditorGUILayout.LabelField($"Preview Size: {previewTexture.width} x {previewTexture.height}", EditorStyles.boldLabel);

                EditorGUILayout.Space(5);

                // Calculate aspect ratio
                float aspect = (float)previewTexture.width / previewTexture.height;
                float maxPreviewWidth = position.width - 40;
                float maxPreviewHeight = 400f;

                float previewWidth = maxPreviewWidth;
                float previewHeight = previewWidth / aspect;

                if (previewHeight > maxPreviewHeight)
                {
                    previewHeight = maxPreviewHeight;
                    previewWidth = previewHeight * aspect;
                }

                Rect previewRect = GUILayoutUtility.GetRect(previewWidth, previewHeight);
                EditorGUI.DrawPreviewTexture(previewRect, previewTexture, null, ScaleMode.ScaleToFit);

                EditorGUILayout.Space(10);

                if (GUILayout.Button("Clear Preview"))
                {
                    CleanupPreview();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No preview generated yet.", MessageType.Info);
            }
        }

        void DrawReplaceTab()
        {
            if (pendingReplacements.Count > 0)
            {
                DrawReplacementPreview();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Sprite replacements will appear here after packing.\n\n" +
                    "Enable 'Auto-Replace Sprite References' in Settings tab to use this feature.",
                    MessageType.Info);

                EditorGUILayout.Space(10);

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("How it works:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("1. Pack your atlas", EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField("2. Tool finds all references to original sprites", EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField("3. Review replacements in this tab", EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField("4. Apply to update your project", EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndVertical();
            }


            void DrawActionButtons()
            {
                EditorGUILayout.BeginHorizontal();

                string validationError = ValidateBeforePacking();

                EditorGUI.BeginDisabledGroup(!string.IsNullOrEmpty(validationError));

                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("📦 Pack Atlas", GUILayout.Height(40)))
                {
                    PackAndSaveAtlas();
                    currentTab = 3; // Switch to Replace tab
                }
                GUI.backgroundColor = Color.white;

                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();

                if (!string.IsNullOrEmpty(validationError))
                {
                    EditorGUILayout.HelpBox(validationError, MessageType.Warning);
                }
            }

            // ... rest of the existing methods ...
        }

        void DrawBasicSettings()
        {
            EditorGUILayout.LabelField("Basic Settings", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            atlasName = EditorGUILayout.TextField("Atlas Filename:", atlasName);
            if (GUILayout.Button("Choose Folder", GUILayout.MaxWidth(120)))
            {
                string path = EditorUtility.OpenFolderPanel("Select save folder", Application.dataPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                        saveFolder = "Assets" + path.Substring(Application.dataPath.Length);
                    else
                        EditorUtility.DisplayDialog("Invalid folder", "Please choose a subfolder inside this project's Assets folder.", "OK");
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Save to: " + saveFolder, EditorStyles.miniLabel);

            padding = EditorGUILayout.IntSlider("Padding (px)", padding, 0, 10);
            maxSize = EditorGUILayout.IntPopup("Max Atlas Size", maxSize,
                new string[] { "512", "1024", "2048", "4096", "8192" },
                new int[] { 512, 1024, 2048, 4096, 8192 });

            exportMetadata = EditorGUILayout.Toggle("Export JSON Metadata", exportMetadata);
        }

        void DrawAdvancedSettings()
        {
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings", true);

            if (showAdvancedSettings)
            {
                EditorGUI.indentLevel++;

                atlasFormat = (TextureFormat)EditorGUILayout.EnumPopup("Texture Format", atlasFormat);
                filterMode = (FilterMode)EditorGUILayout.EnumPopup("Filter Mode", filterMode);
                anisoLevel = EditorGUILayout.IntSlider("Aniso Level", anisoLevel, 0, 16);
                generateMipMaps = EditorGUILayout.Toggle("Generate Mipmaps", generateMipMaps);
                compressionType = (TextureImporterCompression)EditorGUILayout.EnumPopup("Compression", compressionType);

                EditorGUI.indentLevel--;
            }
        }

        void DrawReplacementSettings()
        {
            EditorGUILayout.LabelField("Sprite Replacement Options", EditorStyles.boldLabel);

            autoReplaceSpriteReferences = EditorGUILayout.Toggle("Auto-Replace Sprite References", autoReplaceSpriteReferences);

            if (autoReplaceSpriteReferences)
            {
                EditorGUI.indentLevel++;

                searchInScenes = EditorGUILayout.Toggle("Search in Open Scenes", searchInScenes);
                searchInPrefabs = EditorGUILayout.Toggle("Search in Prefabs", searchInPrefabs);
                searchInAnimations = EditorGUILayout.Toggle("Search in Animation Clips", searchInAnimations);
                searchCustomComponents = EditorGUILayout.Toggle("Search Custom Components (Reflection)", searchCustomComponents);

                EditorGUILayout.Space(5);

                createBackup = EditorGUILayout.Toggle("Create Backup Before Replace", createBackup);
                showReplacementPreview = EditorGUILayout.Toggle("Show Preview Before Apply", showReplacementPreview);

                EditorGUI.indentLevel--;

                EditorGUILayout.HelpBox(
                    "After packing, this tool will automatically find and replace all references to the original textures with the new atlas sprites in:\n" +
                    "• SpriteRenderer components\n" +
                    "• UI Image components\n" +
                    "• Animation clips (if enabled)\n" +
                    "• Custom script fields (if enabled)",
                    MessageType.Info);
            }
        }

        void DrawTextureList()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Selected Textures", GUILayout.Height(25)))
            {
                AddSelectionTextures();
            }
            if (GUILayout.Button("Clear List", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Clear List", "Remove all textures from the list?", "Yes", "No"))
                {
                    textures.Clear();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Drag & Drop Area
            Rect dropArea = GUILayoutUtility.GetRect(0f, 50f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag & Drop Textures Here", EditorStyles.helpBox);
            HandleDragAndDrop(dropArea);

            EditorGUILayout.Space(5);

            // Reorderable list
            if (reorderableList != null)
            {
                reorderableList.DoLayoutList();
            }
        }

        void DrawPreviewSection()
        {
            showPreview = EditorGUILayout.Toggle("Show Atlas Preview", showPreview);

            if (showPreview && textures.Count > 0)
            {
                if (GUILayout.Button("Generate Preview", GUILayout.Height(25)))
                {
                    GeneratePreview();
                }

                if (previewTexture != null)
                {
                    EditorGUILayout.Space(5);

                    float aspect = (float)previewTexture.width / previewTexture.height;
                    float height = Mathf.Min(200f, 200f / aspect);

                    Rect previewRect = GUILayoutUtility.GetRect(200f, height);
                    EditorGUI.DrawPreviewTexture(previewRect, previewTexture, null, ScaleMode.ScaleToFit);

                    EditorGUILayout.LabelField($"Preview Size: {previewTexture.width} x {previewTexture.height}", EditorStyles.miniLabel);
                }
            }
        }

        void DrawPackButton()
        {
            string validationError = ValidateBeforePacking();

            EditorGUI.BeginDisabledGroup(!string.IsNullOrEmpty(validationError));

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Pack Atlas", GUILayout.Height(40)))
            {
                PackAndSaveAtlas();
            }
            GUI.backgroundColor = Color.white;

            EditorGUI.EndDisabledGroup();

            if (!string.IsNullOrEmpty(validationError))
            {
                EditorGUILayout.HelpBox(validationError, MessageType.Warning);
            }
        }

        void DrawReplacementPreview()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField($"Found {pendingReplacements.Count} Sprite References to Replace", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox("Review the replacements below. Uncheck items you don't want to replace.", MessageType.Info);

            replacementScrollPos = EditorGUILayout.BeginScrollView(replacementScrollPos, GUILayout.Height(250));

            for (int i = 0; i < pendingReplacements.Count; i++)
            {
                var replacement = pendingReplacements[i];

                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();

                // Checkbox
                bool include = EditorGUILayout.Toggle(true, GUILayout.Width(20));
                if (!include)
                {
                    GUI.enabled = false;
                }

                // Icon
                Texture2D icon = replacement.isInScene ?
                    EditorGUIUtility.IconContent("SceneAsset Icon").image as Texture2D :
                    EditorGUIUtility.IconContent("Prefab Icon").image as Texture2D;
                GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));

                // Info
                EditorGUILayout.BeginVertical();

                EditorGUILayout.LabelField($"{replacement.componentType} in {replacement.targetObject.name}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"📁 {replacement.scenePath}", EditorStyles.miniLabel);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Old:", GUILayout.Width(40));
                EditorGUILayout.ObjectField(replacement.oldSprite, typeof(Sprite), false, GUILayout.Width(120));
                GUILayout.Label("→", GUILayout.Width(20));
                EditorGUILayout.LabelField("New:", GUILayout.Width(40));
                EditorGUILayout.ObjectField(replacement.newSprite, typeof(Sprite), false, GUILayout.Width(120));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();

                GUI.enabled = true;

                // Remove button
                if (GUILayout.Button("✗", GUILayout.Width(25), GUILayout.Height(40)))
                {
                    pendingReplacements.RemoveAt(i);
                    i--;
                    continue;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();

                if (!include)
                {
                    pendingReplacements.RemoveAt(i);
                    i--;
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button($"✓ Apply All ({pendingReplacements.Count})", GUILayout.Height(35)))
            {
                ApplyReplacements();
            }

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("✗ Cancel", GUILayout.Height(35)))
            {
                pendingReplacements.Clear();
            }

            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        void DrawHelpBox()
        {
            EditorGUILayout.HelpBox(
                "NOTES:\n" +
                "• Textures will be read via GPU copy, so source textures don't need to be readable\n" +
                "• Resulting atlas is saved as PNG and reimported as multiple sprites\n" +
                "• Each sprite is named after the source texture\n" +
                "• Auto-replacement finds all usages in scenes, prefabs, and animations",
                MessageType.Info);
        }

        #endregion

        #region Reorderable List Setup

        void SetupReorderableList()
        {
            reorderableList = new ReorderableList(textures, typeof(Texture2D), true, true, true, true);

            reorderableList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, $"Textures to Pack ({textures.Count})");
            };

            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                rect.y += 2;
                rect.height = EditorGUIUtility.singleLineHeight;

                Texture2D texture = textures[index];

                // Texture field
                Rect textureRect = new Rect(rect.x, rect.y, rect.width - 80, rect.height);
                textures[index] = (Texture2D)EditorGUI.ObjectField(textureRect, texture, typeof(Texture2D), false);

                // Size info
                if (texture != null)
                {
                    Rect sizeRect = new Rect(rect.x + rect.width - 75, rect.y, 75, rect.height);
                    EditorGUI.LabelField(sizeRect, $"{texture.width}x{texture.height}", EditorStyles.miniLabel);
                }
            };

            reorderableList.onAddCallback = (ReorderableList list) =>
            {
                textures.Add(null);
            };
        }

        #endregion

        #region Drag & Drop

        void HandleDragAndDrop(Rect dropArea)
        {
            Event evt = Event.current;

            if (!dropArea.Contains(evt.mousePosition))
                return;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        int addedCount = 0;
                        foreach (UnityEngine.Object draggedObj in DragAndDrop.objectReferences)
                        {
                            if (draggedObj is Texture2D tex && !textures.Contains(tex))
                            {
                                textures.Add(tex);
                                addedCount++;
                            }
                        }

                        if (addedCount > 0)
                        {
                            Debug.Log($"[AtlasPacker] Added {addedCount} textures");
                        }
                    }
                    evt.Use();
                    break;
            }
        }

        #endregion

        #region Texture Selection

        void AddSelectionTextures()
        {
            UnityEngine.Object[] sel = Selection.GetFiltered(typeof(Texture2D), SelectionMode.Assets);
            int added = 0;

            foreach (UnityEngine.Object o in sel)
            {
                Texture2D t = o as Texture2D;
                if (t != null && !textures.Contains(t))
                {
                    textures.Add(t);
                    added++;
                }
            }

            if (added == 0)
                EditorUtility.DisplayDialog("No textures found", "Select textures in the Project window then click 'Add Selected Textures'.", "OK");
            else
                Debug.Log($"[AtlasPacker] Added {added} textures");
        }

        #endregion

        #region Preview

        void GeneratePreview()
        {
            CleanupPreview();

            if (textures.Count == 0)
                return;

            Texture2D[] copies = null;

            try
            {
                EditorUtility.DisplayProgressBar("Generating Preview", "Creating readable copies...", 0f);

                copies = new Texture2D[textures.Count];
                for (int i = 0; i < textures.Count; i++)
                {
                    if (textures[i] == null) continue;
                    copies[i] = MakeTextureReadableCopy(textures[i]);
                }

                EditorUtility.DisplayProgressBar("Generating Preview", "Packing...", 0.5f);

                previewTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                previewTexture.PackTextures(copies.Where(c => c != null).ToArray(), padding, maxSize);

                Debug.Log($"[AtlasPacker] Preview generated: {previewTexture.width}x{previewTexture.height}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AtlasPacker] Preview generation failed: {ex.Message}");
            }
            finally
            {
                if (copies != null)
                {
                    foreach (var copy in copies)
                    {
                        if (copy != null) DestroyImmediate(copy);
                    }
                }
                EditorUtility.ClearProgressBar();
            }
        }

        void CleanupPreview()
        {
            if (previewTexture != null)
            {
                DestroyImmediate(previewTexture);
                previewTexture = null;
            }
        }

        #endregion

        #region Validation

        string ValidateBeforePacking()
        {
            if (textures.Count == 0)
                return "No textures added. Add at least one texture.";

            if (string.IsNullOrEmpty(atlasName))
                return "Atlas name cannot be empty.";

            if (textures.Any(t => t == null))
                return "List contains null textures. Remove them first.";

            // Check for duplicate names
            var duplicates = textures.GroupBy(t => t.name).Where(g => g.Count() > 1);
            if (duplicates.Any())
                return $"Duplicate texture names found: {string.Join(", ", duplicates.Select(g => g.Key))}";

            // Estimate atlas size
            int totalArea = textures.Sum(t => t.width * t.height);
            int estimatedSize = Mathf.NextPowerOfTwo((int)Mathf.Sqrt(totalArea * 1.2f)); // 20% overhead

            if (estimatedSize > maxSize)
                return $"Textures may not fit in {maxSize}x{maxSize}. Estimated: {estimatedSize}x{estimatedSize}. Try increasing max size.";

            return null;
        }

        #endregion

        #region Packing

        void PackAndSaveAtlas()
        {
            Texture2D[] copies = null;
            Texture2D atlas = null;

            try
            {
                EditorUtility.DisplayProgressBar("Atlas Packer", "Creating readable copies...", 0.1f);

                // Make readable copies
                copies = new Texture2D[textures.Count];
                for (int i = 0; i < textures.Count; i++)
                {
                    EditorUtility.DisplayProgressBar("Atlas Packer",
                        $"Processing {textures[i].name}...",
                        0.1f + (0.3f * i / textures.Count));

                    copies[i] = MakeTextureReadableCopy(textures[i]);
                }

                EditorUtility.DisplayProgressBar("Atlas Packer", "Packing atlas...", 0.5f);

                // Pack atlas
                atlas = new Texture2D(2, 2, atlasFormat, false);
                Rect[] rects = atlas.PackTextures(copies, padding, maxSize);

                if (rects == null || rects.Length == 0)
                {
                    EditorUtility.DisplayDialog("Packing Failed", "Failed to pack textures. Try reducing texture count or increasing max size.", "OK");
                    return;
                }

                EditorUtility.DisplayProgressBar("Atlas Packer", "Encoding PNG...", 0.7f);

                // Save PNG
                byte[] png = atlas.EncodeToPNG();
                string path = saveFolder.TrimEnd('/') + "/" + atlasName + ".png";
                File.WriteAllBytes(path, png);

                EditorUtility.DisplayProgressBar("Atlas Packer", "Importing asset...", 0.8f);

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

                // Configure texture importer
                ConfigureTextureImporter(path, atlas.width, atlas.height, rects);

                EditorUtility.DisplayProgressBar("Atlas Packer", "Finalizing...", 0.9f);

                AssetDatabase.Refresh();

                // Export metadata if requested
                if (exportMetadata)
                {
                    ExportMetadataJSON(path, atlas.width, atlas.height, rects);
                }

                Debug.Log($"[AtlasPacker] Atlas created: {path} ({atlas.width}x{atlas.height})");

                // Auto-replace sprites
                if (autoReplaceSpriteReferences)
                {
                    EditorUtility.DisplayProgressBar("Atlas Packer", "Finding sprite references...", 0.95f);

                    // Load newly created sprites
                    UnityEngine.Object[] atlasAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                    Sprite[] newSprites = atlasAssets.OfType<Sprite>().ToArray();

                    // Create mapping
                    Dictionary<string, Sprite> spriteMap = new Dictionary<string, Sprite>();
                    foreach (var sprite in newSprites)
                    {
                        spriteMap[sprite.name] = sprite;
                    }

                    // Find references
                    pendingReplacements = FindAllSpriteReferences(spriteMap);

                    EditorUtility.ClearProgressBar();

                    if (showReplacementPreview && pendingReplacements.Count > 0)
                    {
                        // User will review manually
                        EditorUtility.DisplayDialog("Replacements Found",
                            $"Found {pendingReplacements.Count} sprite references to replace.\n\nReview them in the window below.", "OK");
                    }
                    else if (pendingReplacements.Count > 0)
                    {
                        // Auto-apply
                        ApplyReplacements();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Atlas Created",
                            $"Atlas saved to: {path}\nSprites: {newSprites.Length}\n\nNo sprite references found to replace.", "OK");
                    }
                }
                else
                {
                    EditorUtility.ClearProgressBar();
                    EditorUtility.DisplayDialog("Atlas Created",
                        $"Atlas saved to: {path}\nSize: {atlas.width}x{atlas.height}\nSprites: {rects.Length}", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Error", "An error occurred during atlas packing:\n" + ex.Message, "OK");
            }
            finally
            {
                // CLEANUP MEMORY
                if (copies != null)
                {
                    foreach (var copy in copies)
                    {
                        if (copy != null) DestroyImmediate(copy);
                    }
                }

                if (atlas != null)
                {
                    DestroyImmediate(atlas);
                }

                EditorUtility.ClearProgressBar();
            }
        }

        void ConfigureTextureImporter(string assetPath, int atlasW, int atlasH, Rect[] rects)
        {
            TextureImporter ti = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (ti == null)
            {
                Debug.LogError($"[AtlasPacker] Failed to get TextureImporter for {assetPath}");
                return;
            }

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Multiple;
            ti.filterMode = filterMode;
            ti.anisoLevel = anisoLevel;
            ti.mipmapEnabled = generateMipMaps;
            ti.textureCompression = compressionType;

            // Force synchronous import to get dimensions
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);

            // Create sprite metadata
            SpriteMetaData[] metas = new SpriteMetaData[rects.Length];
            for (int i = 0; i < rects.Length; i++)
            {
                Rect r = rects[i];
                int x = Mathf.RoundToInt(r.x * atlasW);
                int y = Mathf.RoundToInt(r.y * atlasH);
                int w = Mathf.RoundToInt(r.width * atlasW);
                int h = Mathf.RoundToInt(r.height * atlasH);

                // Flip Y coordinate (Unity uses bottom-left origin)
                y = atlasH - y - h;

                SpriteMetaData meta = new SpriteMetaData();
                meta.name = textures[i].name;
                meta.rect = new Rect(x, y, w, h);
                meta.pivot = new Vector2(0.5f, 0.5f);
                meta.alignment = (int)SpriteAlignment.Center;
                metas[i] = meta;
            }

            ti.spritesheet = metas;
            ti.SaveAndReimport();
        }

        Texture2D MakeTextureReadableCopy(Texture2D src)
        {
            if (src == null) return null;

            int w = src.width;
            int h = src.height;

            RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(src, rt);

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D readable = new Texture2D(w, h, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            readable.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            return readable;
        }

        void ExportMetadataJSON(string atlasPath, int width, int height, Rect[] rects)
        {
            try
            {
                AtlasMetadata metadata = new AtlasMetadata
                {
                    atlasName = atlasName,
                    width = width,
                    height = height,
                    sprites = new SpriteData[rects.Length]
                };

                for (int i = 0; i < rects.Length; i++)
                {
                    Rect r = rects[i];
                    int x = Mathf.RoundToInt(r.x * width);
                    int y = Mathf.RoundToInt(r.y * height);
                    int w = Mathf.RoundToInt(r.width * width);
                    int h = Mathf.RoundToInt(r.height * height);

                    y = height - y - h; // Flip Y

                    metadata.sprites[i] = new SpriteData
                    {
                        name = textures[i].name,
                        x = x,
                        y = y,
                        width = w,
                        height = h
                    };
                }

                string jsonPath = atlasPath.Replace(".png", ".json");
                string json = JsonUtility.ToJson(metadata, true);
                File.WriteAllText(jsonPath, json);

                Debug.Log($"[AtlasPacker] Metadata exported to: {jsonPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AtlasPacker] Failed to export metadata: {ex.Message}");
            }
        }

        #endregion

        #region Sprite Reference Finding

        List<SpriteReplacement> FindAllSpriteReferences(Dictionary<string, Sprite> spriteMap)
        {
            List<SpriteReplacement> replacements = new List<SpriteReplacement>();

            // 1. Search in scenes
            if (searchInScenes)
            {
                var sceneReplacements = FindReferencesInScenes(spriteMap);
                replacements.AddRange(sceneReplacements);
                Debug.Log($"[AtlasPacker] Found {sceneReplacements.Count} references in scenes");
            }

            // 2. Search in prefabs
            if (searchInPrefabs)
            {
                var prefabReplacements = FindReferencesInPrefabs(spriteMap);
                replacements.AddRange(prefabReplacements);
                Debug.Log($"[AtlasPacker] Found {prefabReplacements.Count} references in prefabs");
            }

            // 3. Search in animation clips
            if (searchInAnimations)
            {
                var animReplacements = FindReferencesInAnimationClips(spriteMap);
                replacements.AddRange(animReplacements);
                Debug.Log($"[AtlasPacker] Found {animReplacements.Count} references in animations");
            }

            return replacements;
        }

        // ===== SCENE SEARCH =====
        List<SpriteReplacement> FindReferencesInScenes(Dictionary<string, Sprite> spriteMap)
        {
            List<SpriteReplacement> replacements = new List<SpriteReplacement>();

            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                GameObject[] rootObjects = scene.GetRootGameObjects();

                foreach (var root in rootObjects)
                {
                    FindSpritesInGameObject(root, spriteMap, replacements, true, scene.path);
                }
            }

            return replacements;
        }

        // ===== PREFAB SEARCH =====
        List<SpriteReplacement> FindReferencesInPrefabs(Dictionary<string, Sprite> spriteMap)
        {
            List<SpriteReplacement> replacements = new List<SpriteReplacement>();

            string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab");

            for (int i = 0; i < prefabGUIDs.Length; i++)
            {
                string guid = prefabGUIDs[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (i % 10 == 0)
                {
                    EditorUtility.DisplayProgressBar("Searching Prefabs",
                        $"Checking {Path.GetFileName(path)}...", (float)i / prefabGUIDs.Length);
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    FindSpritesInGameObject(prefab, spriteMap, replacements, false, path);
                }
            }

            EditorUtility.ClearProgressBar();
            return replacements;
        }

        // ===== ANIMATION CLIP SEARCH =====
        List<SpriteReplacement> FindReferencesInAnimationClips(Dictionary<string, Sprite> spriteMap)
        {
            List<SpriteReplacement> replacements = new List<SpriteReplacement>();

            string[] animationGUIDs = AssetDatabase.FindAssets("t:AnimationClip");

            foreach (string guid in animationGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

                if (clip == null) continue;

                // Get all curve bindings
                var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);

                foreach (var binding in bindings)
                {
                    if (binding.type == typeof(SpriteRenderer) && binding.propertyName == "m_Sprite")
                    {
                        var keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);

                        foreach (var keyframe in keyframes)
                        {
                            Sprite oldSprite = keyframe.value as Sprite;
                            if (oldSprite != null && spriteMap.ContainsKey(oldSprite.name))
                            {
                                replacements.Add(new SpriteReplacement
                                {
                                    targetObject = clip,
                                    targetComponent = null,
                                    componentType = $"AnimationClip ({binding.path})",
                                    oldSprite = oldSprite,
                                    newSprite = spriteMap[oldSprite.name],
                                    propertyPath = $"{binding.path}/{binding.propertyName}",
                                    isInScene = false,
                                    scenePath = path
                                });
                            }
                        }
                    }
                    else if (binding.type == typeof(Image) && binding.propertyName == "m_Sprite")
                    {
                        var keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);

                        foreach (var keyframe in keyframes)
                        {
                            Sprite oldSprite = keyframe.value as Sprite;
                            if (oldSprite != null && spriteMap.ContainsKey(oldSprite.name))
                            {
                                replacements.Add(new SpriteReplacement
                                {
                                    targetObject = clip,
                                    targetComponent = null,
                                    componentType = $"AnimationClip UI ({binding.path})",
                                    oldSprite = oldSprite,
                                    newSprite = spriteMap[oldSprite.name],
                                    propertyPath = $"{binding.path}/{binding.propertyName}",
                                    isInScene = false,
                                    scenePath = path
                                });
                            }
                        }
                    }
                }
            }

            return replacements;
        }

        // ===== RECURSIVE GAMEOBJECT SEARCH =====
        void FindSpritesInGameObject(GameObject go, Dictionary<string, Sprite> spriteMap,
            List<SpriteReplacement> replacements, bool isInScene, string scenePath)
        {
            // Check SpriteRenderer
            var spriteRenderer = go.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                string spriteName = spriteRenderer.sprite.name;
                if (spriteMap.ContainsKey(spriteName))
                {
                    replacements.Add(new SpriteReplacement
                    {
                        targetObject = go,
                        targetComponent = spriteRenderer,
                        componentType = "SpriteRenderer",
                        oldSprite = spriteRenderer.sprite,
                        newSprite = spriteMap[spriteName],
                        propertyPath = "m_Sprite",
                        isInScene = isInScene,
                        scenePath = scenePath
                    });
                }
            }

            // Check UI Image
            var image = go.GetComponent<Image>();
            if (image != null && image.sprite != null)
            {
                string spriteName = image.sprite.name;
                if (spriteMap.ContainsKey(spriteName))
                {
                    replacements.Add(new SpriteReplacement
                    {
                        targetObject = go,
                        targetComponent = image,
                        componentType = "UI.Image",
                        oldSprite = image.sprite,
                        newSprite = spriteMap[spriteName],
                        propertyPath = "m_Sprite",
                        isInScene = isInScene,
                        scenePath = scenePath
                    });
                }
            }

            // Search custom components if enabled
            if (searchCustomComponents)
            {
                FindSpritesInCustomComponents(go, spriteMap, replacements, isInScene, scenePath);
            }

            // Recursively check children
            foreach (Transform child in go.transform)
            {
                FindSpritesInGameObject(child.gameObject, spriteMap, replacements, isInScene, scenePath);
            }
        }

        // ===== CUSTOM COMPONENT SEARCH (REFLECTION) =====
        void FindSpritesInCustomComponents(GameObject go, Dictionary<string, Sprite> spriteMap,
            List<SpriteReplacement> replacements, bool isInScene, string scenePath)
        {
            Component[] allComponents = go.GetComponents<Component>();

            foreach (var component in allComponents)
            {
                if (component == null) continue;

                // Skip built-in components we already handle
                if (component is SpriteRenderer || component is Image || component is Transform)
                    continue;

                Type type = component.GetType();

                // Check all fields
                var fields = type.GetFields(System.Reflection.BindingFlags.Public |
                                           System.Reflection.BindingFlags.NonPublic |
                                           System.Reflection.BindingFlags.Instance);

                foreach (var field in fields)
                {
                    // Single Sprite field
                    if (field.FieldType == typeof(Sprite))
                    {
                        Sprite sprite = field.GetValue(component) as Sprite;
                        if (sprite != null && spriteMap.ContainsKey(sprite.name))
                        {
                            replacements.Add(new SpriteReplacement
                            {
                                targetObject = go,
                                targetComponent = component,
                                componentType = $"{type.Name}.{field.Name}",
                                oldSprite = sprite,
                                newSprite = spriteMap[sprite.name],
                                propertyPath = field.Name,
                                isInScene = isInScene,
                                scenePath = scenePath
                            });
                        }
                    }
                    // Sprite array field
                    else if (field.FieldType == typeof(Sprite[]))
                    {
                        Sprite[] sprites = field.GetValue(component) as Sprite[];
                        if (sprites != null)
                        {
                            for (int i = 0; i < sprites.Length; i++)
                            {
                                if (sprites[i] != null && spriteMap.ContainsKey(sprites[i].name))
                                {
                                    replacements.Add(new SpriteReplacement
                                    {
                                        targetObject = go,
                                        targetComponent = component,
                                        componentType = $"{type.Name}.{field.Name}[{i}]",
                                        oldSprite = sprites[i],
                                        newSprite = spriteMap[sprites[i].name],
                                        propertyPath = field.Name,
                                        isInScene = isInScene,
                                        scenePath = scenePath,
                                        arrayIndex = i
                                    });
                                }
                            }
                        }
                    }
                    // List<Sprite> field
                    else if (field.FieldType == typeof(List<Sprite>))
                    {
                        List<Sprite> sprites = field.GetValue(component) as List<Sprite>;
                        if (sprites != null)
                        {
                            for (int i = 0; i < sprites.Count; i++)
                            {
                                if (sprites[i] != null && spriteMap.ContainsKey(sprites[i].name))
                                {
                                    replacements.Add(new SpriteReplacement
                                    {
                                        targetObject = go,
                                        targetComponent = component,
                                        componentType = $"{type.Name}.{field.Name}[{i}]",
                                        oldSprite = sprites[i],
                                        newSprite = spriteMap[sprites[i].name],
                                        propertyPath = field.Name,
                                        isInScene = isInScene,
                                        scenePath = scenePath,
                                        arrayIndex = i
                                    });
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Replacement Application

        void ApplyReplacements()
        {
            if (pendingReplacements.Count == 0)
            {
                EditorUtility.DisplayDialog("No Replacements", "No replacements to apply.", "OK");
                return;
            }

            // Create backup if requested
            if (createBackup)
            {
                CreateBackup();
            }

            int appliedCount = 0;
            int failedCount = 0;

            try
            {
                // Group replacements by type
                var sceneReplacements = pendingReplacements.Where(r => r.isInScene && r.targetComponent != null).ToList();
                var prefabReplacements = pendingReplacements.Where(r => !r.isInScene && r.targetComponent != null).ToList();
                var animationReplacements = pendingReplacements.Where(r => r.targetObject is AnimationClip).ToList();

                // Apply scene replacements
                if (sceneReplacements.Count > 0)
                {
                    EditorUtility.DisplayProgressBar("Applying Replacements", "Updating scenes...", 0.3f);

                    foreach (var replacement in sceneReplacements)
                    {
                        if (ApplySingleReplacement(replacement))
                            appliedCount++;
                        else
                            failedCount++;
                    }

                    // Mark scenes dirty
                    var dirtyScenes = sceneReplacements.Select(r => r.scenePath).Distinct();
                    foreach (var scenePath in dirtyScenes)
                    {
                        var scene = EditorSceneManager.GetSceneByPath(scenePath);
                        if (scene.isLoaded)
                        {
                            EditorSceneManager.MarkSceneDirty(scene);
                        }
                    }
                }

                // Apply prefab replacements
                if (prefabReplacements.Count > 0)
                {
                    EditorUtility.DisplayProgressBar("Applying Replacements", "Updating prefabs...", 0.6f);

                    var prefabGroups = prefabReplacements.GroupBy(r => r.scenePath);

                    foreach (var group in prefabGroups)
                    {
                        string prefabPath = group.Key;
                        GameObject prefabRoot = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                        if (prefabRoot != null)
                        {
                            foreach (var replacement in group)
                            {
                                if (ApplySingleReplacement(replacement))
                                    appliedCount++;
                                else
                                    failedCount++;
                            }

                            // Save prefab
                            PrefabUtility.SavePrefabAsset(prefabRoot);
                            EditorUtility.SetDirty(prefabRoot);
                        }
                    }
                }

                // Apply animation replacements
                if (animationReplacements.Count > 0)
                {
                    EditorUtility.DisplayProgressBar("Applying Replacements", "Updating animations...", 0.9f);

                    var animGroups = animationReplacements.GroupBy(r => r.targetObject as AnimationClip);

                    foreach (var group in animGroups)
                    {
                        AnimationClip clip = group.Key;
                        if (clip == null) continue;

                        foreach (var replacement in group)
                        {
                            if (ApplyAnimationReplacement(replacement, clip))
                                appliedCount++;
                            else
                                failedCount++;
                        }

                        EditorUtility.SetDirty(clip);
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.ClearProgressBar();

                string message = $"Successfully replaced {appliedCount} sprite references!";
                if (failedCount > 0)
                {
                    message += $"\n\nFailed: {failedCount}";
                }

                EditorUtility.DisplayDialog("Replacements Applied", message, "OK");

                Debug.Log($"[AtlasPacker] Applied {appliedCount} replacements, {failedCount} failed");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                EditorUtility.DisplayDialog("Error", "An error occurred during replacement:\n" + ex.Message, "OK");
            }
            finally
            {
                pendingReplacements.Clear();
                EditorUtility.ClearProgressBar();
            }
        }

        bool ApplySingleReplacement(SpriteReplacement replacement)
        {
            try
            {
                if (replacement.targetComponent == null)
                    return false;

                // Register undo
                Undo.RecordObject(replacement.targetComponent, "Replace Sprite with Atlas");

                if (replacement.componentType == "SpriteRenderer")
                {
                    var sr = replacement.targetComponent as SpriteRenderer;
                    if (sr != null)
                    {
                        sr.sprite = replacement.newSprite;
                        EditorUtility.SetDirty(sr);
                        return true;
                    }
                }
                else if (replacement.componentType == "UI.Image")
                {
                    var img = replacement.targetComponent as Image;
                    if (img != null)
                    {
                        img.sprite = replacement.newSprite;
                        EditorUtility.SetDirty(img);
                        return true;
                    }
                }
                else if (searchCustomComponents)
                {
                    // Use reflection for custom components
                    Type type = replacement.targetComponent.GetType();
                    var field = type.GetField(replacement.propertyPath,
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Instance);

                    if (field != null)
                    {
                        if (replacement.arrayIndex >= 0)
                        {
                            // Array or List
                            if (field.FieldType == typeof(Sprite[]))
                            {
                                Sprite[] array = field.GetValue(replacement.targetComponent) as Sprite[];
                                if (array != null && replacement.arrayIndex < array.Length)
                                {
                                    array[replacement.arrayIndex] = replacement.newSprite;
                                    field.SetValue(replacement.targetComponent, array);
                                    EditorUtility.SetDirty(replacement.targetComponent);
                                    return true;
                                }
                            }
                            else if (field.FieldType == typeof(List<Sprite>))
                            {
                                List<Sprite> list = field.GetValue(replacement.targetComponent) as List<Sprite>;
                                if (list != null && replacement.arrayIndex < list.Count)
                                {
                                    list[replacement.arrayIndex] = replacement.newSprite;
                                    field.SetValue(replacement.targetComponent, list);
                                    EditorUtility.SetDirty(replacement.targetComponent);
                                    return true;
                                }
                            }
                        }
                        else
                        {
                            // Single value
                            field.SetValue(replacement.targetComponent, replacement.newSprite);
                            EditorUtility.SetDirty(replacement.targetComponent);
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AtlasPacker] Failed to replace sprite in {replacement.targetObject.name}: {ex.Message}");
                return false;
            }
        }

        bool ApplyAnimationReplacement(SpriteReplacement replacement, AnimationClip clip)
        {
            try
            {
                // Parse property path: "path/to/object/m_Sprite"
                string[] parts = replacement.propertyPath.Split('/');
                string path = string.Join("/", parts, 0, parts.Length - 1);
                string propertyName = parts[parts.Length - 1];

                // Determine component type
                Type componentType = replacement.componentType.Contains("UI") ? typeof(Image) : typeof(SpriteRenderer);

                // Get binding
                EditorCurveBinding binding = new EditorCurveBinding
                {
                    path = path,
                    type = componentType,
                    propertyName = propertyName
                };

                // Get current keyframes
                ObjectReferenceKeyframe[] keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);

                if (keyframes == null || keyframes.Length == 0)
                    return false;

                // Replace sprites in keyframes
                bool modified = false;
                for (int i = 0; i < keyframes.Length; i++)
                {
                    if (keyframes[i].value == replacement.oldSprite)
                    {
                        keyframes[i].value = replacement.newSprite;
                        modified = true;
                    }
                }

                if (modified)
                {
                    AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AtlasPacker] Failed to replace sprite in animation: {ex.Message}");
                return false;
            }
        }

        void CreateBackup()
        {
            try
            {
                string backupFolder = "Assets/AtlasPacker_Backups";
                if (!AssetDatabase.IsValidFolder(backupFolder))
                {
                    string[] folders = backupFolder.Split('/');
                    string currentPath = folders[0];

                    for (int i = 1; i < folders.Length; i++)
                    {
                        string newFolder = currentPath + "/" + folders[i];
                        if (!AssetDatabase.IsValidFolder(newFolder))
                        {
                            AssetDatabase.CreateFolder(currentPath, folders[i]);
                        }
                        currentPath = newFolder;
                    }
                }

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupPath = $"{backupFolder}/backup_{atlasName}_{timestamp}.txt";

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("==============================================");
                sb.AppendLine($"Atlas Packer Backup - {DateTime.Now}");
                sb.AppendLine($"Atlas: {atlasName}");
                sb.AppendLine($"Total replacements: {pendingReplacements.Count}");
                sb.AppendLine("==============================================");
                sb.AppendLine();

                foreach (var r in pendingReplacements)
                {
                    sb.AppendLine($"Object: {r.targetObject?.name ?? "N/A"}");
                    sb.AppendLine($"Component: {r.componentType}");
                    sb.AppendLine($"Location: {r.scenePath}");
                    sb.AppendLine($"Old Sprite: {r.oldSprite?.name ?? "N/A"}");
                    sb.AppendLine($"New Sprite: {r.newSprite?.name ?? "N/A"}");
                    sb.AppendLine($"Property: {r.propertyPath}");
                    if (r.arrayIndex >= 0)
                        sb.AppendLine($"Array Index: {r.arrayIndex}");
                    sb.AppendLine("---");
                }

                File.WriteAllText(backupPath, sb.ToString());
                AssetDatabase.Refresh();

                Debug.Log($"[AtlasPacker] Backup created at: {backupPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AtlasPacker] Failed to create backup: {ex.Message}");
            }
        }

        #endregion

        #region Preferences

        void LoadPreferences()
        {
            padding = EditorPrefs.GetInt(PREFS_PADDING, 2);
            maxSize = EditorPrefs.GetInt(PREFS_MAX_SIZE, 2048);
            atlasName = EditorPrefs.GetString(PREFS_ATLAS_NAME, "texture_atlas");
            saveFolder = EditorPrefs.GetString(PREFS_SAVE_FOLDER, "Assets");
            autoReplaceSpriteReferences = EditorPrefs.GetBool(PREFS_AUTO_REPLACE, true);
            searchInScenes = EditorPrefs.GetBool(PREFS_SEARCH_SCENES, true);
            searchInPrefabs = EditorPrefs.GetBool(PREFS_SEARCH_PREFABS, true);
        }

        void SavePreferences()
        {
            EditorPrefs.SetInt(PREFS_PADDING, padding);
            EditorPrefs.SetInt(PREFS_MAX_SIZE, maxSize);
            EditorPrefs.SetString(PREFS_ATLAS_NAME, atlasName);
            EditorPrefs.SetString(PREFS_SAVE_FOLDER, saveFolder);
            EditorPrefs.SetBool(PREFS_AUTO_REPLACE, autoReplaceSpriteReferences);
            EditorPrefs.SetBool(PREFS_SEARCH_SCENES, searchInScenes);
            EditorPrefs.SetBool(PREFS_SEARCH_PREFABS, searchInPrefabs);
        }

        #endregion
    }
}
