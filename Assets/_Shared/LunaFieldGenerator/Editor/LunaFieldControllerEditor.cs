using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using UnityEditor.Compilation;
using Amanotes.Core;
using Amanotes.LunaFieldGenerator;

namespace Amanotes.LunaFieldGenerator.Editor
{
    /// <summary>
    /// Main custom editor for LunaFieldController.
    /// Coordinates tab navigation and delegates to partial class extensions.
    /// </summary>
    [CustomEditor(typeof(LunaFieldController))]
    public partial class LunaFieldControllerEditor : UnityEditor.Editor
    {
        #region Configuration & State

        // Tab Management
        internal enum EditorTab { Main, CreateField, Rename, Migrate, Settings }
        internal EditorTab currentTab = EditorTab.Main;

        // Scroll positions
        internal Vector2 scrollPosition;

        // Data
        internal List<Type> availableFieldTypes = new List<Type>(32);
        internal Dictionary<int, bool> fieldFoldouts = new Dictionary<int, bool>();
        internal Dictionary<string, string> lunaNameCache = new Dictionary<string, string>(32, StringComparer.OrdinalIgnoreCase);

        // Services
        internal EditorSettingsStore settings;
        internal DummyTextureManager dummyManager;

        // Persistent Settings (loaded from EditorSettingsStore)
        internal string customFieldPath;
        internal string dummyFolderPath;
        internal Texture2D defaultDummyTexture;
        internal bool autoRemoveDuplicates = true;

        // Section Visibility
        internal bool showStatus = true;
        internal bool showFieldList = true;
        internal bool showActions = true;
        internal bool expandAllFields;

        // Parent Transforms
        internal Transform parentTransformForGenerate;
        internal Transform parentTransformForCreate;

        // Target GameObject for attaching new field script
        internal GameObject targetGameObjectForField;

        // Create Field Settings
        internal string fieldBaseName = "NewField";
        internal FieldObjectType createFieldType = FieldObjectType.RawImage;
        internal bool createWithTexture = true;
        internal bool createWithPosition = true;
        internal bool createWithScale = true;
        internal bool createWithRotation = false;
        internal bool createAsBackground = false;
        internal DefaultAsset fieldSubFolder;
        internal DefaultAsset generateSubFolder;

        // Automation State (static to persist across compilation)
        internal static string pendingFieldClassName;
        internal static string pendingFieldControllerInstanceID;
        internal static FieldObjectType pendingFieldObjectType = FieldObjectType.RawImage;
        internal static bool pendingWithTexture = true;
        internal static string pendingParentPath;
        internal static string pendingSubFolderName;
        internal static bool pendingAttachToExistingGO = false;

        internal enum FieldObjectType { RawImage, SpriteRenderer }

        // UI Theme
        internal static class Theme
        {
            public static readonly Color Primary = new Color(0.3f, 0.6f, 0.9f);
            public static readonly Color Success = new Color(0.3f, 0.8f, 0.4f);
            public static readonly Color Warning = new Color(0.9f, 0.7f, 0.2f);
            public static readonly Color Danger = new Color(0.9f, 0.3f, 0.3f);
            public static readonly Color Info = new Color(0.5f, 0.7f, 1f);
            public static readonly Color Light = new Color(0.95f, 0.95f, 0.95f);
            public static readonly Color Dark = new Color(0.2f, 0.2f, 0.25f);
            public static readonly Color HeaderBg = new Color(0.25f, 0.45f, 0.75f);
            public static readonly Color TabActive = new Color(0.3f, 0.6f, 0.9f);
            public static readonly Color TabInactive = new Color(0.3f, 0.95f, 0.4f);
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            settings = new EditorSettingsStore();
            LoadSettings();
            dummyManager = new DummyTextureManager(dummyFolderPath, defaultDummyTexture);
            dummyManager.ReloadDefaultTexture();
            defaultDummyTexture = dummyManager.DefaultTexture;
            ScanForFieldTypes();
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            CheckPendingAutomation();
        }

        private void OnDisable()
        {
            SaveSettings();
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
        }

        private void OnCompilationFinished(object obj) { }

        #endregion

        #region Main GUI

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            LunaFieldController controller = (LunaFieldController)target;

            DrawMainHeader();
            EditorGUILayout.Space(5);
            DrawTabBar();
            EditorGUILayout.Space(10);

            switch (currentTab)
            {
                case EditorTab.Main:        DrawMainTab(controller); break;
                case EditorTab.CreateField: DrawCreateFieldTab(controller); break;
                case EditorTab.Rename:      DrawRenameTab(controller); break;
                case EditorTab.Migrate:     DrawMigrateTab(); break;
                case EditorTab.Settings:    DrawSettingsTab(); break;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawTabBar()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUIStyle tabStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                fixedHeight = 28
            };

            DrawTabButton("Main", EditorTab.Main, tabStyle);
            DrawTabButton("Create", EditorTab.CreateField, tabStyle);
            DrawTabButton("Rename", EditorTab.Rename, tabStyle);
            DrawTabButton("Migrate", EditorTab.Migrate, tabStyle);
            DrawTabButton("Settings", EditorTab.Settings, tabStyle);

            GUI.backgroundColor = Color.white;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTabButton(string label, EditorTab tab, GUIStyle style)
        {
            GUI.backgroundColor = currentTab == tab ? Theme.TabActive : Theme.TabInactive;
            if (GUILayout.Button(label, style, GUILayout.Width(120)))
                currentTab = tab;
        }

        private void DrawMainHeader()
        {
            EditorGUILayout.Space(10);
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            GUI.backgroundColor = Theme.HeaderBg;
            EditorGUILayout.BeginVertical("box");
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Luna Field Controller", headerStyle);
            GUILayout.Space(5);
            EditorGUILayout.EndVertical();
            GUI.backgroundColor = Color.white;
        }

        #endregion

        #region Shared UI Helpers

        internal void DrawSectionHeader(ref bool foldout, string title, Color color)
        {
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = color;
            EditorGUILayout.BeginHorizontal("box");
            GUI.backgroundColor = Color.white;

            GUIStyle headerStyle = new GUIStyle(EditorStyles.foldoutHeader)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 12
            };
            foldout = EditorGUILayout.Foldout(foldout, title, true, headerStyle);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        internal void DrawStatBox(string label, string value, Color color)
        {
            GUI.backgroundColor = color;
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = Color.white;

            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            GUIStyle valueStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };

            EditorGUILayout.LabelField(label, labelStyle);
            EditorGUILayout.LabelField(value, valueStyle);
            EditorGUILayout.EndVertical();
        }

        internal void DrawCenteredTitle(string title)
        {
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField(title, titleStyle);
        }

        internal void DrawStatusLabel(string label, string value, Color color)
        {
            GUI.backgroundColor = color;
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = Color.white;

            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            GUIStyle valueStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 12 };

            EditorGUILayout.LabelField(label, labelStyle);
            EditorGUILayout.LabelField(value, valueStyle);

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Type Scanning

        internal void ScanForFieldTypes()
        {
            availableFieldTypes = FieldTypeScanner.ScanAll(customFieldPath);
            lunaNameCache.Clear();
        }

        #endregion

        #region Settings Persistence

        internal void LoadSettings()
        {
            customFieldPath = settings.FieldPath;
            dummyFolderPath = settings.DummyPath;
            autoRemoveDuplicates = settings.AutoRemoveDuplicates;
            showStatus = settings.ShowStatus;
            showFieldList = settings.ShowFieldList;
            showActions = settings.ShowActions;
            currentTab = (EditorTab)settings.CurrentTab;
            if (!Enum.IsDefined(typeof(EditorTab), currentTab))
                currentTab = EditorTab.Main;
            fieldBaseName = settings.FieldBaseName;
            createFieldType = (FieldObjectType)settings.CreateFieldType;
            createWithTexture = settings.CreateWithTexture;
            createWithPosition = settings.CreateWithPosition;
            createWithScale = settings.CreateWithScale;
            createWithRotation = settings.CreateWithRotation;
            createAsBackground = settings.CreateAsBackground;

            string texturePath = settings.DefaultTexturePath;
            if (!string.IsNullOrEmpty(texturePath))
                defaultDummyTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

            // Restore folder assets
            string fieldSubFolderPath = settings.FieldSubFolderPath;
            if (!string.IsNullOrEmpty(fieldSubFolderPath))
                fieldSubFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(fieldSubFolderPath);

            string generateSubFolderPath = settings.GenerateSubFolderPath;
            if (!string.IsNullOrEmpty(generateSubFolderPath))
                generateSubFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(generateSubFolderPath);

            // Restore scene transforms by path
            string createParentPath = settings.CreateParentPath;
            if (!string.IsNullOrEmpty(createParentPath))
            {
                GameObject go = GameObject.Find(createParentPath);
                if (go != null) parentTransformForCreate = go.transform;
            }

            string generateParentPath = settings.GenerateParentPath;
            if (!string.IsNullOrEmpty(generateParentPath))
            {
                GameObject go = GameObject.Find(generateParentPath);
                if (go != null) parentTransformForGenerate = go.transform;
            }
        }

        internal void SaveSettings()
        {
            settings.FieldPath = customFieldPath;
            settings.DummyPath = dummyFolderPath;
            settings.AutoRemoveDuplicates = autoRemoveDuplicates;
            settings.ShowStatus = showStatus;
            settings.ShowFieldList = showFieldList;
            settings.ShowActions = showActions;
            settings.CurrentTab = (int)currentTab;
            settings.FieldBaseName = fieldBaseName;
            settings.CreateFieldType = (int)createFieldType;
            settings.CreateWithTexture = createWithTexture;
            settings.CreateWithPosition = createWithPosition;
            settings.CreateWithScale = createWithScale;
            settings.CreateWithRotation = createWithRotation;
            settings.CreateAsBackground = createAsBackground;

            settings.FieldSubFolderPath = fieldSubFolder != null ? AssetDatabase.GetAssetPath(fieldSubFolder) : "";
            settings.GenerateSubFolderPath = generateSubFolder != null ? AssetDatabase.GetAssetPath(generateSubFolder) : "";

            settings.CreateParentPath = (parentTransformForCreate != null)
                ? SubfolderResolver.GetGameObjectPath(parentTransformForCreate.gameObject) : "";
            settings.GenerateParentPath = (parentTransformForGenerate != null)
                ? SubfolderResolver.GetGameObjectPath(parentTransformForGenerate.gameObject) : "";

            settings.DefaultTexturePath = defaultDummyTexture != null
                ? AssetDatabase.GetAssetPath(defaultDummyTexture) : "";
        }

        #endregion

        #region Shared Utilities

        /// <summary>Get Luna category name for a FieldBase type by parsing its .cs source.</summary>
        internal string GetLunaNameForType(Type fieldType)
        {
            string className = fieldType.Name;
            if (lunaNameCache.TryGetValue(className, out string cached))
                return cached;

            string lunaName = ParseLunaNameFromSource(className);
            lunaNameCache[className] = lunaName ?? "";
            return lunaName;
        }

        private string ParseLunaNameFromSource(string className)
        {
            if (!Directory.Exists(customFieldPath)) return null;

            string[] files = Directory.GetFiles(customFieldPath, $"{className}.cs", SearchOption.AllDirectories);
            if (files.Length == 0) return null;

            string content = File.ReadAllText(files[0]);

            var sectionMatch = System.Text.RegularExpressions.Regex.Match(content,
                @"\[LunaPlaygroundSection\s*\(\s*""([^""]*)""\s*\)\]");
            if (sectionMatch.Success) return sectionMatch.Groups[1].Value;

            var legacyMatch = System.Text.RegularExpressions.Regex.Match(content,
                @"\[LunaPlayground(?:Asset|Field)\s*\(\s*""[^""]*""\s*,\s*\d+\s*,\s*""([^""]*)""\s*\)\]");
            return legacyMatch.Success ? legacyMatch.Groups[1].Value : null;
        }

        internal void OpenFolderInProject(string path)
        {
            if (!Directory.Exists(path))
            {
                EditorUtility.DisplayDialog("Folder Not Found", $"Path does not exist:\n{path}", "OK");
                return;
            }

            UnityEngine.Object folderAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (folderAsset != null)
            {
                Selection.activeObject = folderAsset;
                EditorGUIUtility.PingObject(folderAsset);
                EditorUtility.FocusProjectWindow();
            }
        }

        internal int CountAssignedTransforms(LunaFieldController controller)
        {
            FieldBase[] fields = controller.fields;
            if (fields == null) return 0;

            int count = 0;
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i] != null && fields[i].transform != null)
                    count++;
            }
            return count;
        }

        internal string GetCreateFieldSubFolderName()
        {
            if (fieldSubFolder == null) return null;
            string folderPath = AssetDatabase.GetAssetPath(fieldSubFolder);
            return AssetDatabase.IsValidFolder(folderPath) ? Path.GetFileName(folderPath) : null;
        }

        internal string GetGenerateSubFolderName()
        {
            if (generateSubFolder == null) return null;
            string folderPath = AssetDatabase.GetAssetPath(generateSubFolder);
            return AssetDatabase.IsValidFolder(folderPath) ? Path.GetFileName(folderPath) : null;
        }

        internal string GetCreateFieldScriptPath()
        {
            string subFolderName = GetCreateFieldSubFolderName();
            return !string.IsNullOrEmpty(subFolderName)
                ? Path.Combine(customFieldPath, subFolderName)
                : customFieldPath;
        }

        internal List<Type> GetFilteredFieldTypes()
        {
            if (generateSubFolder == null) return new List<Type>(availableFieldTypes);

            string folderPath = AssetDatabase.GetAssetPath(generateSubFolder);
            if (!AssetDatabase.IsValidFolder(folderPath)) return new List<Type>(availableFieldTypes);

            return FieldTypeScanner.FilterByFolder(availableFieldTypes, folderPath);
        }

        #endregion
    }
}
