using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Amanotes.LunaFieldGenerator;

namespace Amanotes.LunaFieldGenerator.Editor
{
    /// <summary>
    /// Partial: Migrate tab — scan and recompile all FieldBase scripts to new format.
    /// Preserves class name and LunaPlaygroundSection name.
    /// </summary>
    public partial class LunaFieldControllerEditor
    {
        #region Migrate State

        private List<FieldMigrateInfo> migrateResults = new List<FieldMigrateInfo>(32);
        private Vector2 migrateScrollPosition;
        private bool migrateScanDone = false;

        private class FieldMigrateInfo
        {
            public string filePath;
            public string className;
            public string sectionName;
            public bool hasTexture;
            public bool hasPosition;
            public bool hasScale;
            public bool hasRotation;
            public bool isBackground;
            public bool needsMigration;
            public bool selected = true;
        }

        #endregion

        #region Migrate Tab UI

        private void DrawMigrateTab()
        {
            EditorGUILayout.BeginVertical("box");
            DrawCenteredTitle("Migrate Fields to New Format");
            EditorGUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Scan all *_LunaField.cs files and recompile them using the new code template.\n" +
                "Preserves: class name, section name, detected properties (texture/position/scale/rotation).\n" +
                "Rewrites: namespace, usings, code structure, capture methods.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            // Scan button
            GUI.backgroundColor = Theme.Info;
            if (GUILayout.Button("Scan Field Scripts", GUILayout.Height(35)))
                ScanFieldsForMigration();
            GUI.backgroundColor = Color.white;

            if (!migrateScanDone)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.Space(10);

            // Results
            if (migrateResults.Count == 0)
            {
                EditorGUILayout.HelpBox("No *_LunaField.cs files found in field path.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            int needsMigrationCount = 0;
            int selectedCount = 0;
            for (int i = 0; i < migrateResults.Count; i++)
            {
                if (migrateResults[i].needsMigration) needsMigrationCount++;
                if (migrateResults[i].selected) selectedCount++;
            }

            EditorGUILayout.LabelField($"Found {migrateResults.Count} file(s), {needsMigrationCount} need migration", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Select all / none
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Select All", GUILayout.Width(80)))
                for (int i = 0; i < migrateResults.Count; i++) migrateResults[i].selected = true;
            if (GUILayout.Button("Select None", GUILayout.Width(90)))
                for (int i = 0; i < migrateResults.Count; i++) migrateResults[i].selected = false;
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField($"Selected: {selectedCount}/{migrateResults.Count}", GUILayout.Width(120));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // File list
            migrateScrollPosition = EditorGUILayout.BeginScrollView(migrateScrollPosition, GUILayout.MaxHeight(350));

            for (int i = 0; i < migrateResults.Count; i++)
                DrawMigrateItem(migrateResults[i]);

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            // Migrate button
            EditorGUI.BeginDisabledGroup(selectedCount == 0);
            GUI.backgroundColor = Theme.Warning;
            if (GUILayout.Button($"Migrate Selected ({selectedCount} files)", GUILayout.Height(40)))
                ExecuteMigration();
            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
        }

        private void DrawMigrateItem(FieldMigrateInfo info)
        {
            Color bgColor = info.needsMigration ? new Color(0.4f, 0.3f, 0.2f) : new Color(0.2f, 0.35f, 0.2f);
            GUI.backgroundColor = bgColor;
            EditorGUILayout.BeginHorizontal("box");
            GUI.backgroundColor = Color.white;

            info.selected = EditorGUILayout.Toggle(info.selected, GUILayout.Width(20));
            EditorGUILayout.LabelField(info.className, EditorStyles.boldLabel, GUILayout.Width(200));

            // Properties badges
            string props = "";
            if (info.isBackground) props = "BG";
            else
            {
                if (info.hasTexture) props += "T ";
                if (info.hasPosition) props += "P ";
                if (info.hasScale) props += "S ";
                if (info.hasRotation) props += "R ";
            }

            GUIStyle propStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Theme.Info }
            };
            EditorGUILayout.LabelField(props.Trim(), propStyle, GUILayout.Width(60));

            // Section name
            GUIStyle sectionStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Theme.Warning }
            };
            EditorGUILayout.LabelField($"[{info.sectionName}]", sectionStyle, GUILayout.Width(150));

            // Status
            GUIStyle statusStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = info.needsMigration ? Theme.Warning : Theme.Success }
            };
            EditorGUILayout.LabelField(info.needsMigration ? "Needs migration" : "[v] OK", statusStyle);

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Scan Logic

        private void ScanFieldsForMigration()
        {
            migrateResults.Clear();
            migrateScanDone = true;

            if (!Directory.Exists(customFieldPath))
            {
                EditorUtility.DisplayDialog("Error", $"Field path not found:\n{customFieldPath}", "OK");
                return;
            }

            string[] files = Directory.GetFiles(customFieldPath, "*_LunaField.cs", SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                string filePath = files[i].Replace("\\", "/");
                string content = File.ReadAllText(filePath);

                FieldMigrateInfo info = ParseFieldFile(filePath, content);
                if (info != null)
                    migrateResults.Add(info);
            }

            migrateResults.Sort((a, b) => string.Compare(a.className, b.className, StringComparison.Ordinal));
        }

        private FieldMigrateInfo ParseFieldFile(string filePath, string content)
        {
            var info = new FieldMigrateInfo { filePath = filePath };

            // Extract class name
            Match classMatch = Regex.Match(content, @"public\s+class\s+(\w+_LunaField)\s*:\s*FieldBase");
            if (!classMatch.Success) return null;
            info.className = classMatch.Groups[1].Value;

            // Extract section name
            Match sectionMatch = Regex.Match(content, @"\[LunaPlaygroundSection\s*\(\s*""([^""]*)""\s*\)\]");
            if (sectionMatch.Success)
                info.sectionName = sectionMatch.Groups[1].Value;
            else
                info.sectionName = info.className.Replace("_LunaField", "");

            // Detect properties
            info.hasTexture = Regex.IsMatch(content, @"public\s+Texture2D\s+tex\s*=");
            info.hasPosition = Regex.IsMatch(content, @"public\s+Vector2\s+phone_Position_PT");
            info.hasScale = Regex.IsMatch(content, @"public\s+Vector2\s+phone_Scale_PT");
            info.hasRotation = Regex.IsMatch(content, @"public\s+float\s+phone_Rotation_PT");

            // Detect background
            info.isBackground = Regex.IsMatch(content, @"public\s+Texture2D\s+texPortrait")
                             && Regex.IsMatch(content, @"public\s+Texture2D\s+texLandscape");

            // Check if already in new format
            bool hasNewNamespace = Regex.IsMatch(content, @"namespace\s+LunaField\b");
            bool hasUsingLunaFieldGen = content.Contains("using Amanotes.LunaFieldGenerator;");
            bool hasCaptureMethod = content.Contains("CaptureSlotFrom");
            bool hasGetStored = content.Contains("GetStoredPosition") || content.Contains("GetStoredScale");
            bool hasClassLevelSection = Regex.IsMatch(content, @"\[LunaPlaygroundSection\s*\(");
            bool hasPerFieldSection = Regex.IsMatch(content, @"\[LunaPlayground(?:Asset|Field)\s*\(\s*""[^""]*""\s*,\s*\d+\s*,\s*""[^""]*""\s*\)\]");

            info.needsMigration = !hasNewNamespace || !hasUsingLunaFieldGen || !hasCaptureMethod
                || !hasGetStored || hasClassLevelSection || !hasPerFieldSection;

            return info;
        }

        #endregion

        #region Migration Logic

        private void ExecuteMigration()
        {
            var toMigrate = new List<FieldMigrateInfo>();
            for (int i = 0; i < migrateResults.Count; i++)
            {
                if (migrateResults[i].selected)
                    toMigrate.Add(migrateResults[i]);
            }

            if (toMigrate.Count == 0) return;

            if (!EditorUtility.DisplayDialog("Confirm Migration",
                $"This will OVERWRITE {toMigrate.Count} file(s) with the new format.\n\n" +
                "• Namespace → LunaField\n" +
                "• Adds proper usings\n" +
                "• Regenerates code structure\n" +
                "• Preserves class name + section name + detected properties\n\n" +
                "Make sure you have version control or backup!",
                "Migrate", "Cancel"))
                return;

            int success = 0;
            int failed = 0;
            var errors = new System.Text.StringBuilder();

            for (int i = 0; i < toMigrate.Count; i++)
            {
                FieldMigrateInfo info = toMigrate[i];
                EditorUtility.DisplayProgressBar("Migrating...",
                    $"{info.className} ({i + 1}/{toMigrate.Count})",
                    (float)i / toMigrate.Count);

                try
                {
                    string newContent = GenerateMigratedContent(info);
                    File.WriteAllText(info.filePath, newContent);
                    info.needsMigration = false;
                    success++;
                }
                catch (Exception e)
                {
                    failed++;
                    errors.AppendLine($"• {info.className}: {e.Message}");
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            string message = $"Migrated: {success}\nFailed: {failed}";
            if (errors.Length > 0)
                message += $"\n\nErrors:\n{errors}";

            EditorUtility.DisplayDialog("Migration Complete", message, "OK");
        }

        private string GenerateMigratedContent(FieldMigrateInfo info)
        {
            string baseName = info.className.Replace("_LunaField", "");

            if (info.isBackground)
                return ScriptCodeGenerator.GenerateBackgroundClass(baseName, info.sectionName);

            return ScriptCodeGenerator.GenerateFieldClass(
                baseName,
                info.hasTexture,
                info.hasPosition,
                info.hasScale,
                info.hasRotation,
                info.sectionName);
        }

        #endregion
    }
}
