using UnityEditor;
using UnityEngine;
using Amanotes.LunaFieldGenerator;

namespace Amanotes.LunaFieldGenerator.Editor
{
    /// <summary>
    /// Partial: Settings tab UI.
    /// </summary>
    public partial class LunaFieldControllerEditor
    {
        #region Settings Tab

        private void DrawSettingsTab()
        {
            EditorGUILayout.BeginVertical("box");
            DrawCenteredTitle("Advanced Settings");
            EditorGUILayout.Space(10);
            DrawSettingsSection();
            EditorGUILayout.EndVertical();
        }

        private void DrawSettingsSection()
        {
            EditorGUILayout.BeginVertical("helpbox");

            EditorGUILayout.LabelField("File Paths", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            EditorGUI.BeginChangeCheck();
            customFieldPath = EditorGUILayout.TextField("Field Scripts Path", customFieldPath);
            dummyFolderPath = EditorGUILayout.TextField("Dummy Textures Path", dummyFolderPath);
            if (EditorGUI.EndChangeCheck())
            {
                SaveSettings();
                dummyManager.FolderPath = dummyFolderPath;
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Default Dummy Texture", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            EditorGUI.BeginChangeCheck();
            defaultDummyTexture = (Texture2D)EditorGUILayout.ObjectField("Texture", defaultDummyTexture, typeof(Texture2D), false);
            if (EditorGUI.EndChangeCheck())
            {
                dummyManager.DefaultTexture = defaultDummyTexture;
                SaveSettings();
            }

            if (defaultDummyTexture == null)
            {
                dummyManager.EnsureFolderExists();
                dummyManager.ReloadDefaultTexture();
                defaultDummyTexture = dummyManager.DefaultTexture;
            }

            if (defaultDummyTexture != null)
                EditorGUILayout.HelpBox($"[v] Using: {defaultDummyTexture.name}", MessageType.None);
            else
                EditorGUILayout.HelpBox("No default texture set. Will use auto-generated checkerboard.", MessageType.Info);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Generation Options", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            EditorGUI.BeginChangeCheck();
            autoRemoveDuplicates = EditorGUILayout.Toggle("Auto Remove Duplicates", autoRemoveDuplicates);
            if (EditorGUI.EndChangeCheck()) SaveSettings();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(15);

            EditorGUILayout.LabelField("Reset", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Theme.Warning;
            if (GUILayout.Button("Reset Paths to Default", GUILayout.Height(35)))
            {
                if (EditorUtility.DisplayDialog("Reset Paths?", "Reset all paths to default values?", "Yes", "Cancel"))
                {
                    settings.ResetPaths();
                    customFieldPath = settings.FieldPath;
                    dummyFolderPath = settings.DummyPath;
                    dummyManager.FolderPath = dummyFolderPath;
                }
            }
            GUI.backgroundColor = Theme.Danger;
            if (GUILayout.Button("Clear Texture", GUILayout.Height(35)))
            {
                if (EditorUtility.DisplayDialog("Clear Texture?", "Remove default dummy texture?", "Yes", "Cancel"))
                {
                    defaultDummyTexture = null;
                    dummyManager.DefaultTexture = null;
                    settings.ClearDefaultTexture();
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        #endregion
    }
}
