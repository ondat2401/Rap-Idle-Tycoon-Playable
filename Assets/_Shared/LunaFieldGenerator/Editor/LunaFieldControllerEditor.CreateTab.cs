using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using Amanotes.LunaFieldGenerator;

namespace Amanotes.LunaFieldGenerator.Editor
{
    /// <summary>
    /// Partial: Create Field tab UI and logic.
    /// Two modes: Preset (dropdown) and Custom (free-form name).
    /// </summary>
    public partial class LunaFieldControllerEditor
    {
        #region Create Tab State

        private bool usePreset = true;
        private int selectedPresetIndex = 0;
        private string[] availablePresetNames;
        private string[] availablePresetDisplay;

        #endregion

        #region Create Field Tab

        private void DrawCreateFieldTab(LunaFieldController controller)
        {
            EditorGUILayout.BeginVertical("box");
            DrawCenteredTitle("Create New Field Class");
            EditorGUILayout.Space(10);

            // Toggle: Preset or Custom
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Use Preset:", GUILayout.Width(100));
            usePreset = EditorGUILayout.Toggle(usePreset);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            if (usePreset)
                DrawPresetSection();
            else
                DrawCustomCreateSection();

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Preset Section

        private void DrawPresetSection()
        {
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Create from Preset", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Rebuild available presets (filter out existing files)
            RebuildAvailablePresets();

            if (availablePresetNames == null || availablePresetNames.Length == 0)
            {
                EditorGUILayout.HelpBox("All preset fields have been created!", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // Dropdown
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Preset:", GUILayout.Width(100));
            selectedPresetIndex = EditorGUILayout.Popup(selectedPresetIndex, availablePresetDisplay);
            EditorGUILayout.EndHorizontal();

            if (selectedPresetIndex >= 0 && selectedPresetIndex < availablePresetNames.Length)
            {
                string selected = availablePresetNames[selectedPresetIndex];
                EditorGUILayout.HelpBox($"Will create: {selected}_LunaField.cs", MessageType.None);
            }

            EditorGUILayout.Space(10);

            // Shared settings
            DrawSharedFieldSettings();

            EditorGUILayout.Space(15);

            // Create button
            GUI.backgroundColor = Theme.Success;
            if (GUILayout.Button("Create Class", GUILayout.Height(45)))
            {
                if (selectedPresetIndex >= 0 && selectedPresetIndex < availablePresetNames.Length)
                    CreateFieldBaseClass(availablePresetNames[selectedPresetIndex]);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        private void RebuildAvailablePresets()
        {
            var presets = (LunaFieldPreset[])Enum.GetValues(typeof(LunaFieldPreset));
            var names = new List<string>(presets.Length);
            var display = new List<string>(presets.Length);

            string scriptFolder = GetCreateFieldScriptPath();

            for (int i = 0; i < presets.Length; i++)
            {
                string baseName = presets[i].ToString();
                string fullClassName = baseName + "_LunaField";
                string scriptPath = Path.Combine(scriptFolder, fullClassName + ".cs");

                if (File.Exists(scriptPath)) continue;

                names.Add(baseName);
                display.Add(baseName.Replace("_", " / "));
            }

            availablePresetNames = names.ToArray();
            availablePresetDisplay = display.ToArray();

            if (selectedPresetIndex >= availablePresetNames.Length)
                selectedPresetIndex = 0;
        }

        #endregion

        #region Custom Create Section

        private void DrawCustomCreateSection()
        {
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Custom Field Name", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Base Name
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Base Name:", GUILayout.Width(100));
            EditorGUI.BeginChangeCheck();
            fieldBaseName = EditorGUILayout.TextField(fieldBaseName);
            if (EditorGUI.EndChangeCheck()) SaveSettings();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox($"Will create: {fieldBaseName}_LunaField.cs", MessageType.None);
            EditorGUILayout.Space(10);

            // Shared settings
            DrawSharedFieldSettings();

            EditorGUILayout.Space(15);

            // Action Button
            GUI.backgroundColor = Theme.Success;
            if (GUILayout.Button("Create Class", GUILayout.Height(45)))
                CreateFieldBaseClass(fieldBaseName);
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Shared Settings UI

        private void DrawSharedFieldSettings()
        {
            // Subfolder
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Script Folder:", GUILayout.Width(100));
            EditorGUI.BeginChangeCheck();
            fieldSubFolder = (DefaultAsset)EditorGUILayout.ObjectField(fieldSubFolder, typeof(DefaultAsset), false);
            if (EditorGUI.EndChangeCheck()) SaveSettings();
            EditorGUILayout.EndHorizontal();

            if (fieldSubFolder != null)
            {
                string folderPath = AssetDatabase.GetAssetPath(fieldSubFolder);
                if (AssetDatabase.IsValidFolder(folderPath))
                    EditorGUILayout.HelpBox($"[v] Script path: Samples/{Path.GetFileName(folderPath)}/", MessageType.None);
                else
                {
                    EditorGUILayout.HelpBox("Please drag a folder, not a file.", MessageType.Warning);
                    fieldSubFolder = null;
                }
            }

            EditorGUILayout.Space(5);

            // Field Properties
            EditorGUILayout.LabelField("Field Properties", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Has Texture:", GUILayout.Width(100));
            EditorGUI.BeginChangeCheck();
            createWithTexture = EditorGUILayout.Toggle(createWithTexture);
            if (EditorGUI.EndChangeCheck()) SaveSettings();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Is Background:", GUILayout.Width(100));
            EditorGUI.BeginChangeCheck();
            createAsBackground = EditorGUILayout.Toggle(createAsBackground);
            if (EditorGUI.EndChangeCheck()) SaveSettings();
            EditorGUILayout.EndHorizontal();

            if (createAsBackground)
                EditorGUILayout.HelpBox("Background mode: 2 textures (Portrait + Landscape), auto aspect-fill.", MessageType.None);

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Transform Properties", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Has Position:", GUILayout.Width(100));
            createWithPosition = EditorGUILayout.Toggle(createWithPosition);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Has Scale:", GUILayout.Width(100));
            createWithScale = EditorGUILayout.Toggle(createWithScale);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Has Rotation:", GUILayout.Width(100));
            createWithRotation = EditorGUILayout.Toggle(createWithRotation);
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck()) SaveSettings();

            if (!createWithPosition && !createWithScale && !createWithRotation)
                EditorGUILayout.HelpBox("At least one transform property is recommended.", MessageType.Warning);
        }

        #endregion

        #region Create Field Logic

        private void CreateFieldBaseClass(string className)
        {
            if (string.IsNullOrWhiteSpace(className))
            {
                EditorUtility.DisplayDialog("Error", "Class name cannot be empty.", "OK");
                return;
            }

            string targetPath = GetCreateFieldScriptPath();
            if (!Directory.Exists(targetPath))
                Directory.CreateDirectory(targetPath);

            string fullPath = Path.Combine(targetPath, className + "_LunaField.cs");
            if (File.Exists(fullPath))
            {
                EditorUtility.DisplayDialog("Error", $"Class '{className}_LunaField' already exists!", "OK");
                return;
            }

            string content = createAsBackground
                ? ScriptCodeGenerator.GenerateBackgroundClass(className)
                : ScriptCodeGenerator.GenerateFieldClass(className, createWithTexture, createWithPosition, createWithScale, createWithRotation);

            File.WriteAllText(fullPath, content);
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success",
                $"Class created: {className}_LunaField.cs\nPath: {fullPath}", "OK");

            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<MonoScript>(fullPath));
        }

        #endregion
    }
}
