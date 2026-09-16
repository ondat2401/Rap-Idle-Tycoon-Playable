using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using Amanotes.Core;

namespace Amanotes.Core.Editor
{
    [CustomEditor(typeof(GameConfigManager))]
    public class GameConfigManagerEditor : UnityEditor.Editor
    {
        private GameConfigManager manager;
        private SerializedProperty configsProp;

        // UI State
        private Vector2 scrollPos;
        private string searchFilter = "";
        private bool showStatistics = true;
        private bool showValidation = true;

        // Styling
        private GUIStyle headerStyle;
        private GUIStyle boxStyle;
        private Color validColor = new Color(0.3f, 1f, 0.3f);
        private Color errorColor = new Color(1f, 0.3f, 0.3f);
        private Color warningColor = new Color(1f, 0.8f, 0.3f);

        private void OnEnable()
        {
            manager = (GameConfigManager)target;
            configsProp = serializedObject.FindProperty("configs");
        }

        public override void OnInspectorGUI()
        {
            InitStyles();
            serializedObject.Update();

            DrawHeader();
            DrawToolbar();
            DrawStatistics();
            DrawValidation();
            DrawConfigsList();
            DrawFooter();

            serializedObject.ApplyModifiedProperties();
        }

        private void InitStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16   ,
                    alignment = TextAnchor.MiddleCenter
                };
            }

            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(10, 10, 10, 10)
                };
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            GUILayout.Label("Game Config Manager", headerStyle);
            GUILayout.Label("Centralized configuration system", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            GUILayout.Space(10);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Actions", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Scan Project", GUILayout.Height(30)))
            {
                ScanForConfigs();
            }

            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Reload All", GUILayout.Height(30)))
            {
                if (Application.isPlaying)
                {
                    manager.ReloadConfigs();
                }
                else
                {
                    EditorUtility.DisplayDialog("Warning", 
                        "Reload only works in Play Mode!", "OK");
                }
            }

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Validate All", GUILayout.Height(30)))
            {
                ValidateAllConfigs();
            }

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Clear All", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Confirm", 
                    "Remove all configs from list?", "Yes", "Cancel"))
                {
                    configsProp.ClearArray();
                    serializedObject.ApplyModifiedProperties();
                }
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private void DrawStatistics()
        {
            EditorGUILayout.BeginVertical("box");

            showStatistics = EditorGUILayout.Foldout(showStatistics, "Statistics", true);

            if (showStatistics)
            {
                int totalConfigs = configsProp.arraySize;
                int nullConfigs = 0;
                int duplicates = 0;
                Dictionary<Type, int> typeCount = new Dictionary<Type, int>();

                for (int i = 0; i < totalConfigs; i++)
                {
                    var config = configsProp.GetArrayElementAtIndex(i).objectReferenceValue as GameConfigBase;

                    if (config == null)
                    {
                        nullConfigs++;
                        continue;
                    }

                    var type = config.GetType();
                    if (typeCount.ContainsKey(type))
                    {
                        duplicates++;
                        typeCount[type]++;
                    }
                    else
                    {
                        typeCount[type] = 1;
                    }
                }

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Total Configs:", GUILayout.Width(120));
                GUILayout.Label(totalConfigs.ToString(), EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Valid Configs:", GUILayout.Width(120));
                GUI.color = validColor;
                GUILayout.Label((totalConfigs - nullConfigs - duplicates).ToString(), EditorStyles.boldLabel);
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();

                if (nullConfigs > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Null Configs:", GUILayout.Width(120));
                    GUI.color = warningColor;
                    GUILayout.Label(nullConfigs.ToString(), EditorStyles.boldLabel);
                    GUI.color = Color.white;
                    EditorGUILayout.EndHorizontal();
                }

                if (duplicates > 0)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label("Duplicates:", GUILayout.Width(120));
                    GUI.color = errorColor;
                    GUILayout.Label(duplicates.ToString(), EditorStyles.boldLabel);
                    GUI.color = Color.white;
                    EditorGUILayout.EndHorizontal();
                }

                // Show unique types
                if (typeCount.Count > 0)
                {
                    GUILayout.Space(5);
                    GUILayout.Label("Config Types:", EditorStyles.miniLabel);
                    foreach (var kvp in typeCount.OrderBy(x => x.Key.Name))
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(20);
                        GUILayout.Label($"• {kvp.Key.Name}", GUILayout.Width(200));

                        if (kvp.Value > 1)
                        {
                            GUI.color = errorColor;
                            GUILayout.Label($"({kvp.Value}x)", EditorStyles.boldLabel);
                            GUI.color = Color.white;
                        }
                        else
                        {
                            GUI.color = validColor;
                            GUILayout.Label("✓", EditorStyles.boldLabel);
                            GUI.color = Color.white;
                        }

                        EditorGUILayout.EndHorizontal();
                    }
                }
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private void DrawValidation()
        {
            EditorGUILayout.BeginVertical("box");

            showValidation = EditorGUILayout.Foldout(showValidation, "Validation", true);

            if (showValidation)
            {
                bool hasIssues = false;

                for (int i = 0; i < configsProp.arraySize; i++)
                {
                    var config = configsProp.GetArrayElementAtIndex(i).objectReferenceValue as GameConfigBase;

                    if (config == null)
                    {
                        if (!hasIssues)
                        {
                            GUILayout.Label("Issues Found:", EditorStyles.boldLabel);
                            hasIssues = true;
                        }

                        EditorGUILayout.BeginHorizontal();
                        GUI.color = warningColor;
                        GUILayout.Label($"[!] Index {i}: Null reference", EditorStyles.wordWrappedLabel);
                        GUI.color = Color.white;

                        if (GUILayout.Button("Remove", GUILayout.Width(70)))
                        {
                            configsProp.DeleteArrayElementAtIndex(i);
                            serializedObject.ApplyModifiedProperties();
                            return;
                        }
                        EditorGUILayout.EndHorizontal();
                        continue;
                    }

                    if (!config.Validate(out string error))
                    {
                        if (!hasIssues)
                        {
                            GUILayout.Label("Issues Found:", EditorStyles.boldLabel);
                            hasIssues = true;
                        }

                        EditorGUILayout.BeginHorizontal();
                        GUI.color = errorColor;
                        GUILayout.Label($"[X] {config.GetType().Name}: {error}", EditorStyles.wordWrappedLabel);
                        GUI.color = Color.white;

                        if (GUILayout.Button("Select", GUILayout.Width(70)))
                        {
                            Selection.activeObject = config;
                            EditorGUIUtility.PingObject(config);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }

                if (!hasIssues)
                {
                    GUI.color = validColor;
                    GUILayout.Label("All configs are valid!", EditorStyles.boldLabel);
                    GUI.color = Color.white;
                }
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private void DrawConfigsList()
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Configs List ({configsProp.arraySize})", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            searchFilter = EditorGUILayout.TextField("Filter:", searchFilter, GUILayout.Width(200));

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));

            for (int i = 0; i < configsProp.arraySize; i++)
            {
                var configProp = configsProp.GetArrayElementAtIndex(i);
                var config = configProp.objectReferenceValue as GameConfigBase;

                // Filter
             

                DrawConfigItem(i, configProp, config);
            }

            EditorGUILayout.EndScrollView();

            // Add button
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("+ Add Config", GUILayout.Height(30), GUILayout.Width(150)))
            {
                configsProp.InsertArrayElementAtIndex(configsProp.arraySize);
                serializedObject.ApplyModifiedProperties();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawConfigItem(int index, SerializedProperty configProp, GameConfigBase config)
        {
            EditorGUILayout.BeginHorizontal("box");

            // Index
            GUILayout.Label($"[{index}]", GUILayout.Width(40));

            // Object field
            EditorGUILayout.PropertyField(configProp, GUIContent.none, GUILayout.Width(250));

            if (config != null)
            {
                // Priority
                GUILayout.Label($"P:{config.Priority}", GUILayout.Width(40));

                // Type
                GUILayout.Label(config.GetType().Name, EditorStyles.miniLabel, GUILayout.Width(150));

                // Actions
                if (GUILayout.Button("Ping", GUILayout.Width(40), GUILayout.Height(20)))
                {
                    EditorGUIUtility.PingObject(config);
                }

                if (GUILayout.Button("Select", GUILayout.Width(50), GUILayout.Height(20)))
                {
                    Selection.activeObject = config;
                }
            }
            else
            {
                GUI.color = warningColor;
                GUILayout.Label("[!] NULL", EditorStyles.boldLabel);
                GUI.color = Color.white;
            }

            // Remove button
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("X", GUILayout.Width(30)))
            {
                configsProp.DeleteArrayElementAtIndex(index);
                if (config != null) // Need to delete twice if not null
                    configsProp.DeleteArrayElementAtIndex(index);
                serializedObject.ApplyModifiedProperties();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawFooter()
        {
            GUILayout.Space(10);

            EditorGUILayout.BeginVertical("box");

            if (Application.isPlaying)
            {
                GUI.color = Color.green;
                GUILayout.Label("Runtime Mode - Stats:", EditorStyles.boldLabel);
                GUI.color = Color.white;

                GUILayout.Label($"Loaded: {manager.TotalConfigs}");
                GUILayout.Label($"Duplicates: {manager.DuplicateCount}");
                GUILayout.Label($"Null Refs: {manager.NullConfigCount}");

                if (GUILayout.Button("Print Debug Info"))
                {
                    manager.PrintDebugInfo();
                }
            }
            else
            {
                GUI.color = warningColor;
                GUILayout.Label("Editor Mode", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }

            EditorGUILayout.EndVertical();
        }

        #region Helper Methods

        private void ScanForConfigs()
        {
            string[] guids = AssetDatabase.FindAssets("t:GameConfigBase");
            int addedCount = 0;

            List<GameConfigBase> existingConfigs = new List<GameConfigBase>();
            for (int i = 0; i < configsProp.arraySize; i++)
            {
                var config = configsProp.GetArrayElementAtIndex(i).objectReferenceValue as GameConfigBase;
                if (config != null)
                    existingConfigs.Add(config);
            }

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<GameConfigBase>(path);

                if (config != null && !existingConfigs.Contains(config))
                {
                    configsProp.InsertArrayElementAtIndex(configsProp.arraySize);
                    configsProp.GetArrayElementAtIndex(configsProp.arraySize - 1).objectReferenceValue = config;
                    addedCount++;
                }
            }

            serializedObject.ApplyModifiedProperties();

            EditorUtility.DisplayDialog("Scan Complete", 
                $"Found and added {addedCount} new configs!\nTotal: {configsProp.arraySize}", "OK");
        }

        private void ValidateAllConfigs()
        {
            int validCount = 0;
            int errorCount = 0;
            string errors = "";

            for (int i = 0; i < configsProp.arraySize; i++)
            {
                var config = configsProp.GetArrayElementAtIndex(i).objectReferenceValue as GameConfigBase;

                if (config == null)
                {
                    errorCount++;
                    continue;
                }

                if (config.Validate(out string error))
                {
                    validCount++;
                }
                else
                {
                    errorCount++;
                    errors += $"\n• {config.GetType().Name}: {error}";
                }
            }

            string message = $"Validation Complete!\n\n" +
                            $"✅ Valid: {validCount}\n" +
                            $"❌ Errors: {errorCount}";

            if (!string.IsNullOrEmpty(errors))
                message += $"\n\nErrors:{errors}";

            EditorUtility.DisplayDialog("Validation Results", message, "OK");
        }

        #endregion
    }
}
