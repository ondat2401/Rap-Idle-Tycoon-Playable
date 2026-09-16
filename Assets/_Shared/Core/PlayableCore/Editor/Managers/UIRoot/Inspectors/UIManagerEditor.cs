using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Linq;
using Amanotes.Core;

namespace Amanotes.Core.Editor
{
    [CustomEditor(typeof(GUIManager))]
    public class UIManagerEditor : UnityEditor.Editor
    {
        private const string PREF_OUTPUT_FOLDER = "UIManager.OutputFolder";
        private string outputFolder = "";
        private string outputFileName = "GUIName.cs";

        private void OnEnable()
        {
            outputFolder = EditorPrefs.GetString(PREF_OUTPUT_FOLDER, "");
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Code Generation", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Output Folder with browse button
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Output Folder");
            string displayFolder = string.IsNullOrEmpty(outputFolder) ? "(Click Browse to select)" : outputFolder;
            EditorGUILayout.LabelField(displayFolder, EditorStyles.miniLabel);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string selected = EditorUtility.OpenFolderPanel("Select Output Folder for GUIName.cs", "Assets", "");
                if (!string.IsNullOrEmpty(selected))
                {
                    if (selected.Contains("Assets"))
                    {
                        outputFolder = "Assets" + selected.Substring(selected.IndexOf("Assets") + 6);
                    }
                    else
                    {
                        outputFolder = selected;
                    }
                    EditorPrefs.SetString(PREF_OUTPUT_FOLDER, outputFolder);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            bool hasFolder = !string.IsNullOrEmpty(outputFolder);

            if (!hasFolder)
            {
                EditorGUILayout.HelpBox("Please select an Output Folder first.", MessageType.Warning);
            }

            EditorGUI.BeginDisabledGroup(!hasFolder);
            if (GUILayout.Button("Generate GUIName Class", GUILayout.Height(30)))
            {
                GenerateGUINameClass();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "Generates a static class with constants for all Screen IDs found in Canvas Root children.\n" +
                "Usage: GUIManager.Instance.ShowGUI(GUIName.GUI_Gameplay);",
                MessageType.Info
            );
        }

        private void GenerateGUINameClass()
        {
            var manager = (GUIManager)target;
            if (manager == null)
            {
                Debug.LogError("[UIManagerEditor] GUIManager not found.");
                return;
            }

            // Get rootCanvas field using reflection
            var rootCanvasField = typeof(GUIManager)
                .GetField("rootCanvas", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var rootCanvas = rootCanvasField?.GetValue(manager) as Canvas;
            if (rootCanvas == null)
            {
                Debug.LogError("[UIManagerEditor] Root Canvas not found. Please assign Root Canvas in GUIManager.");
                return;
            }

            // Collect all GUIBase components from children
            var screens = rootCanvas.GetComponentsInChildren<GUIBase>(true)
                .Where(s => !string.IsNullOrEmpty(s.ScreenId))
                .OrderBy(s => s.ScreenId)
                .ToList();

            if (screens.Count == 0)
            {
                Debug.LogWarning("[UIManagerEditor] No GUIBase components with valid Screen IDs found.");
                return;
            }

            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            var sb = new StringBuilder();
            sb.AppendLine("// AUTO-GENERATED FILE. DO NOT EDIT MANUALLY!");
            sb.AppendLine("public static class GUIName");
            sb.AppendLine("{");

            foreach (var screen in screens)
            {
                string constName = MakeValidIdentifier(screen.ScreenId);
                sb.AppendLine($"    public const string {constName} = \"{screen.ScreenId}\";");
            }

            sb.AppendLine("}");

            string fullPath = Path.Combine(outputFolder, outputFileName);
            File.WriteAllText(fullPath, sb.ToString());
            AssetDatabase.Refresh();

            Debug.Log($"[UIManagerEditor] GUIName class generated at {fullPath} with {screens.Count} screens.");
        }

        private string MakeValidIdentifier(string input)
        {
            if (string.IsNullOrEmpty(input)) return "Unknown";

            var sb = new StringBuilder();
            foreach (char c in input)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    sb.Append(c);
                else if (c == ' ' || c == '-')
                    sb.Append('_');
            }

            string result = sb.ToString();
            if (result.Length > 0 && char.IsDigit(result[0]))
                result = "_" + result;

            return string.IsNullOrEmpty(result) ? "Unknown" : result;
        }
    }
}
