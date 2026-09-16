using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Amanotes.Core;
using Amanotes.LunaFieldGenerator;

namespace Amanotes.LunaFieldGenerator.Editor
{
    /// <summary>
    /// Partial: Rename tab — scan and batch-rename Luna display names in .cs source files.
    /// </summary>
    public partial class LunaFieldControllerEditor
    {
        #region Rename Tab State

        private Vector2 renameScrollPosition;
        private string renameNewName = "";
        private List<ScriptFieldEntry> scriptFieldEntries = new List<ScriptFieldEntry>(32);
        private bool renameScanned;
        private int selectedEntryIndex = -1;

        private static readonly Regex SectionAttrRegex = new Regex(
            @"\[LunaPlaygroundSection\s*\(\s*""([^""]*)""\s*\)\]",
            RegexOptions.Compiled);

        private static readonly Regex LegacyAttrRegex = new Regex(
            @"\[LunaPlayground(?:Asset|Field)\s*\(\s*""[^""]*""\s*,\s*\d+\s*,\s*""([^""]*)""\s*\)\]",
            RegexOptions.Compiled);

        private static readonly Regex AllLunaAttrRegex = new Regex(
            @"\[LunaPlayground(?:Section|Asset|Field)\s*\(",
            RegexOptions.Compiled);

        private class ScriptFieldEntry
        {
            public string className;
            public string lunaName;
            public string scriptPath;
            public int attributeCount;
            public FieldBase sceneField;
            public bool usesSection;
        }

        #endregion

        #region Rename Tab GUI

        private void DrawRenameTab(LunaFieldController controller)
        {
            EditorGUILayout.BeginVertical("box");
            DrawCenteredTitle("Rename Luna Field Names");
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "Scan all FieldBase scripts for LunaPlaygroundSection / LunaPlaygroundAsset / LunaPlaygroundField,\n" +
                "then batch-rename the Luna display name.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            GUI.backgroundColor = Theme.Info;
            if (GUILayout.Button("Scan Scripts for Luna Attributes", GUILayout.Height(35)))
                ScanScriptsForLunaAttributes();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);

            if (!renameScanned || scriptFieldEntries.Count == 0)
            {
                if (renameScanned)
                    EditorGUILayout.HelpBox("No scripts with Luna attributes found.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.LabelField($"Found {scriptFieldEntries.Count} script(s):", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            renameScrollPosition = EditorGUILayout.BeginScrollView(renameScrollPosition, GUILayout.MaxHeight(300));
            for (int i = 0; i < scriptFieldEntries.Count; i++)
                DrawRenameEntry(i);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(15);
            DrawRenameActionSection();

            EditorGUILayout.EndVertical();
        }

        private void DrawRenameEntry(int index)
        {
            ScriptFieldEntry entry = scriptFieldEntries[index];
            bool isSelected = selectedEntryIndex == index;

            Color bgColor = isSelected
                ? new Color(0.25f, 0.45f, 0.7f)
                : (index % 2 == 0 ? Theme.Dark : new Color(0.22f, 0.22f, 0.27f));
            GUI.backgroundColor = bgColor;
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = Color.white;

            EditorGUILayout.BeginHorizontal();

            bool newSelected = GUILayout.Toggle(isSelected, "", GUILayout.Width(20));
            if (newSelected && !isSelected) selectedEntryIndex = index;
            else if (!newSelected && isSelected) selectedEntryIndex = -1;

            GUIStyle nameStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = Theme.Info }
            };
            EditorGUILayout.LabelField(entry.className, nameStyle);

            GUILayout.FlexibleSpace();

            GUIStyle badgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = Theme.Warning },
                fontStyle = FontStyle.Bold
            };
            EditorGUILayout.LabelField($"Luna: \"{entry.lunaName}\"", badgeStyle, GUILayout.Width(200));

            if (entry.sceneField != null && GUILayout.Button("", GUILayout.Width(30), GUILayout.Height(18)))
            {
                Selection.activeGameObject = entry.sceneField.gameObject;
                EditorGUIUtility.PingObject(entry.sceneField.gameObject);
            }

            EditorGUILayout.EndHorizontal();

            string formatLabel = entry.usesSection ? "Section" : "Legacy";
            string sceneBadge = entry.sceneField != null ? "  •  [v] In Scene" : "";
            EditorGUILayout.LabelField(
                $"   {entry.attributeCount} attr(s)  •  {formatLabel}{sceneBadge}  •  {entry.scriptPath}",
                EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void DrawRenameActionSection()
        {
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Rename Luna Name", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (selectedEntryIndex >= 0 && selectedEntryIndex < scriptFieldEntries.Count)
            {
                ScriptFieldEntry selected = scriptFieldEntries[selectedEntryIndex];
                EditorGUILayout.LabelField($"Selected: {selected.className}");
                EditorGUILayout.LabelField($"Current: \"{selected.lunaName}\"  ({(selected.usesSection ? "Section" : "Legacy")})");
                EditorGUILayout.Space(5);
            }
            else
            {
                EditorGUILayout.HelpBox("Select a script above to rename.", MessageType.Info);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("New Name:", GUILayout.Width(80));
            renameNewName = EditorGUILayout.TextField(renameNewName);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            bool hasSelection = selectedEntryIndex >= 0 && selectedEntryIndex < scriptFieldEntries.Count;
            bool hasNewName = !string.IsNullOrWhiteSpace(renameNewName);

            GUI.enabled = hasSelection && hasNewName;
            GUI.backgroundColor = Theme.Warning;
            if (GUILayout.Button("Rename Selected", GUILayout.Height(35)))
            {
                ScriptFieldEntry selected = scriptFieldEntries[selectedEntryIndex];
                if (RenameLunaInFile(selected.scriptPath, selected.lunaName, renameNewName))
                {
                    EditorUtility.DisplayDialog("Rename Complete",
                        $"Renamed \"{selected.lunaName}\" → \"{renameNewName}\"\nin {selected.className}", "OK");
                    AssetDatabase.Refresh();
                    ScanScriptsForLunaAttributes();
                }
            }
            GUI.backgroundColor = Color.white;

            GUI.backgroundColor = Theme.Success;
            if (GUILayout.Button("Rename All Same Name", GUILayout.Height(35)))
            {
                ScriptFieldEntry selected = scriptFieldEntries[selectedEntryIndex];
                int count = RenameAllScriptsWithName(selected.lunaName, renameNewName);
                if (count > 0)
                {
                    EditorUtility.DisplayDialog("Batch Rename Complete",
                        $"Renamed \"{selected.lunaName}\" → \"{renameNewName}\"\nin {count} script(s)", "OK");
                    AssetDatabase.Refresh();
                    ScanScriptsForLunaAttributes();
                }
            }
            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "• Rename Selected: only the selected script\n" +
                "• Rename All Same Name: all scripts sharing the same Luna name",
                MessageType.None);

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Rename Logic

        private void ScanScriptsForLunaAttributes()
        {
            scriptFieldEntries.Clear();
            selectedEntryIndex = -1;
            renameScanned = true;

            if (!Directory.Exists(customFieldPath))
            {
                SDebug.LogWarning($"[LunaFieldEditor] Field path not found: {customFieldPath}");
                return;
            }

            var sceneLookup = new Dictionary<string, FieldBase>(32, StringComparer.OrdinalIgnoreCase);
            FieldBase[] sceneFields = FindObjectsOfType<FieldBase>(true);
            if (sceneFields != null)
            {
                for (int i = 0; i < sceneFields.Length; i++)
                {
                    if (sceneFields[i] == null) continue;
                    string name = sceneFields[i].GetType().Name;
                    if (!sceneLookup.ContainsKey(name))
                        sceneLookup[name] = sceneFields[i];
                }
            }

            string[] csFiles = Directory.GetFiles(customFieldPath, "*.cs", SearchOption.AllDirectories);
            for (int i = 0; i < csFiles.Length; i++)
            {
                string filePath = csFiles[i].Replace('\\', '/');
                string content = File.ReadAllText(filePath);

                Match sectionMatch = SectionAttrRegex.Match(content);
                string lunaName = null;
                bool usesSection = false;

                if (sectionMatch.Success)
                {
                    lunaName = sectionMatch.Groups[1].Value;
                    usesSection = true;
                }
                else
                {
                    Match legacyMatch = LegacyAttrRegex.Match(content);
                    if (legacyMatch.Success)
                        lunaName = legacyMatch.Groups[1].Value;
                }

                if (string.IsNullOrEmpty(lunaName)) continue;

                int attrCount = AllLunaAttrRegex.Matches(content).Count;
                string className = Path.GetFileNameWithoutExtension(filePath);
                sceneLookup.TryGetValue(className, out FieldBase sceneField);

                scriptFieldEntries.Add(new ScriptFieldEntry
                {
                    className = className,
                    lunaName = lunaName,
                    scriptPath = filePath,
                    attributeCount = attrCount,
                    sceneField = sceneField,
                    usesSection = usesSection
                });
            }

            scriptFieldEntries.Sort((a, b) =>
            {
                int cmp = string.Compare(a.lunaName, b.lunaName, StringComparison.Ordinal);
                return cmp != 0 ? cmp : string.Compare(a.className, b.className, StringComparison.Ordinal);
            });

            lunaNameCache.Clear();
            SDebug.Log($"[LunaFieldEditor] Rename scan: found {scriptFieldEntries.Count} script(s)");
        }

        private bool RenameLunaInFile(string filePath, string oldName, string newName)
        {
            if (!File.Exists(filePath)) return false;

            string content = File.ReadAllText(filePath);
            string original = content;

            content = Regex.Replace(content,
                $@"(\[LunaPlaygroundSection\s*\(\s*)""{Regex.Escape(oldName)}""",
                $@"$1""{newName}""");

            content = Regex.Replace(content,
                $@"(\[LunaPlayground(?:Asset|Field)\s*\(\s*""[^""]*""\s*,\s*\d+\s*,\s*)""{Regex.Escape(oldName)}""",
                $@"$1""{newName}""");

            if (content == original) return false;

            File.WriteAllText(filePath, content);
            SDebug.Log($"[LunaFieldEditor] Renamed \"{oldName}\" → \"{newName}\" in: {Path.GetFileName(filePath)}");
            return true;
        }

        private int RenameAllScriptsWithName(string oldName, string newName)
        {
            if (oldName == newName)
            {
                EditorUtility.DisplayDialog("No Change", "New name is the same as current name.", "OK");
                return 0;
            }

            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < scriptFieldEntries.Count; i++)
            {
                if (scriptFieldEntries[i].lunaName == oldName)
                    paths.Add(scriptFieldEntries[i].scriptPath);
            }

            if (Directory.Exists(customFieldPath))
            {
                string[] allCsFiles = Directory.GetFiles(customFieldPath, "*.cs", SearchOption.AllDirectories);
                for (int i = 0; i < allCsFiles.Length; i++)
                {
                    string content = File.ReadAllText(allCsFiles[i]);
                    if (content.Contains($"\"{oldName}\"") &&
                        (SectionAttrRegex.IsMatch(content) || LegacyAttrRegex.IsMatch(content)))
                    {
                        paths.Add(allCsFiles[i].Replace('\\', '/'));
                    }
                }
            }

            if (paths.Count == 0)
            {
                EditorUtility.DisplayDialog("Error", "No script files found for this name.", "OK");
                return 0;
            }

            int successCount = 0;
            foreach (string path in paths)
            {
                if (RenameLunaInFile(path, oldName, newName))
                    successCount++;
            }

            return successCount;
        }

        #endregion
    }
}
