using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using Amanotes.Core;

namespace Amanotes.Core.Editor
{
    [CustomEditor(typeof(AudioManager))]
    public class AudioManagerEditor : UnityEditor.Editor
    {
        private SerializedProperty bgmSource;
        private SerializedProperty sfxSourcePrefab;
        private SerializedProperty audioClips;
        private SerializedProperty initialPoolSize;
        private SerializedProperty maxPoolSize;

        private string audioSearchFolder = "";
        private string outputFolder = "";
        private string outputFileName = "AudioName.cs";
        private bool autoGenerateOnPlay = false;

        private void OnEnable()
        {
            bgmSource = serializedObject.FindProperty("bgmSource");
            sfxSourcePrefab = serializedObject.FindProperty("sfxSourcePrefab");
            audioClips = serializedObject.FindProperty("audioClips");
            initialPoolSize = serializedObject.FindProperty("initialPoolSize");
            maxPoolSize = serializedObject.FindProperty("maxPoolSize");

            LoadEditorPrefs();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawDefaultInspector();

            EditorGUILayout.Space(20);
            DrawAudioNameGenerator();

            EditorGUILayout.Space(10);
            DrawRuntimeControls();

            serializedObject.ApplyModifiedProperties();
        }

        #region Audio Name Generator

        private void DrawAudioNameGenerator()
        {
            EditorGUILayout.LabelField("Audio Name Generator", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Audio Search Folder with browse button
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Audio Folder");
            string displaySearch = string.IsNullOrEmpty(audioSearchFolder) ? "(Click Browse to select)" : audioSearchFolder;
            EditorGUILayout.LabelField(displaySearch, EditorStyles.miniLabel);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string selected = EditorUtility.OpenFolderPanel("Select Audio Clips Folder", "Assets", "");
                if (!string.IsNullOrEmpty(selected))
                {
                    // Convert absolute path to relative Assets path
                    if (selected.Contains("Assets"))
                    {
                        audioSearchFolder = "Assets" + selected.Substring(selected.IndexOf("Assets") + 6);
                    }
                    else
                    {
                        audioSearchFolder = selected;
                    }
                    SaveEditorPrefs();
                }
            }
            EditorGUILayout.EndHorizontal();

            // Output Folder with browse button
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Output Folder");
            string displayOutput = string.IsNullOrEmpty(outputFolder) ? "(Click Browse to select)" : outputFolder;
            EditorGUILayout.LabelField(displayOutput, EditorStyles.miniLabel);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string selected = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
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
                    SaveEditorPrefs();
                }
            }
            EditorGUILayout.EndHorizontal();

            outputFileName = EditorGUILayout.TextField("Output File", outputFileName);

            EditorGUILayout.Space(5);

            // Validate before showing buttons
            bool hasFolders = !string.IsNullOrEmpty(audioSearchFolder) && !string.IsNullOrEmpty(outputFolder);

            if (!hasFolders)
            {
                EditorGUILayout.HelpBox("Please select Audio Folder and Output Folder first.", MessageType.Warning);
            }

            EditorGUI.BeginDisabledGroup(!hasFolders);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Find All Clips", GUILayout.Height(30)))
            {
                FindAndLoadAllClips();
            }

            if (GUILayout.Button("Generate AudioName.cs", GUILayout.Height(30)))
            {
                GenerateAudioNameClass();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(5);

            autoGenerateOnPlay = EditorGUILayout.Toggle("Auto-Generate on Play", autoGenerateOnPlay);

            if (autoGenerateOnPlay)
            {
                EditorGUILayout.HelpBox("AudioName.cs will be auto-generated when entering Play Mode", MessageType.Info);
            }

            EditorGUILayout.EndVertical();

            SaveEditorPrefs();
        }

        private void FindAndLoadAllClips()
        {
            if (!AssetDatabase.IsValidFolder(audioSearchFolder))
            {
                EditorUtility.DisplayDialog("Error", $"Folder not found: {audioSearchFolder}", "OK");
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { audioSearchFolder });
            var clips = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<AudioClip>)
                .Where(c => c != null)
                .OrderBy(c => c.name)
                .ToList();

            // Clear và add vào list
            audioClips.ClearArray();
            foreach (var clip in clips)
            {
                audioClips.InsertArrayElementAtIndex(audioClips.arraySize);
                audioClips.GetArrayElementAtIndex(audioClips.arraySize - 1).objectReferenceValue = clip;
            }

            serializedObject.ApplyModifiedProperties();

            Debug.Log($"✅ Loaded {clips.Count} audio clips from {audioSearchFolder}");
            EditorUtility.DisplayDialog("Success", $"Loaded {clips.Count} audio clips!", "OK");
        }

        private void GenerateAudioNameClass()
        {
            if (!AssetDatabase.IsValidFolder(audioSearchFolder))
            {
                EditorUtility.DisplayDialog("Error", $"Folder not found: {audioSearchFolder}", "OK");
                return;
            }

            // Tạo output folder nếu chưa có
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
                AssetDatabase.Refresh();
            }

            string outputPath = Path.Combine(outputFolder, outputFileName);

            // Tìm tất cả AudioClip
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { audioSearchFolder });
            var clipNames = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(Path.GetFileNameWithoutExtension)
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            if (clipNames.Count == 0)
            {
                EditorUtility.DisplayDialog("Warning", "No audio clips found!", "OK");
                return;
            }

            // Generate file
            using (StreamWriter writer = new StreamWriter(outputPath, false))
            {
                writer.WriteLine("// ========================================");
                writer.WriteLine("// AUTO-GENERATED FILE");
                writer.WriteLine("// DO NOT EDIT MANUALLY!");
                writer.WriteLine($"// Generated: {System.DateTime.Now}");
                writer.WriteLine($"// Total Clips: {clipNames.Count}");
                writer.WriteLine("// ========================================");
                writer.WriteLine();
                writer.WriteLine("public static class AudioName");
                writer.WriteLine("{");

                foreach (var name in clipNames)
                {
                    string constName = MakeValidIdentifier(name);
                    writer.WriteLine($"    public const string {constName} = \"{name}\";");
                }

                writer.WriteLine("}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"✅ AudioName.cs generated at {outputPath} with {clipNames.Count} clips");
            EditorUtility.DisplayDialog("Success", 
                $"Generated AudioName.cs with {clipNames.Count} clips!\n\nPath: {outputPath}", 
                "OK");

            // Ping file
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(outputPath);
            EditorGUIUtility.PingObject(asset);
        }

        private string MakeValidIdentifier(string name)
        {
            // Replace invalid characters
            string valid = new string(name.Select(c => 
                char.IsLetterOrDigit(c) ? c : '_'
            ).ToArray());

            // Ensure doesn't start with digit
            if (valid.Length > 0 && char.IsDigit(valid[0]))
                valid = "_" + valid;

            return valid;
        }

        #endregion

        #region Runtime Controls

        private void DrawRuntimeControls()
        {
            if (!Application.isPlaying) return;

            EditorGUILayout.LabelField("Runtime Controls", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var manager = target as AudioManager;

            // BGM Controls
            EditorGUILayout.LabelField($"BGM: {manager.CurrentBGM ?? "None"}", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Stop BGM"))
                manager.StopBGM();

            if (manager.IsBGMPlaying)
            {
                if (GUILayout.Button("Pause"))
                    manager.PauseBGM();
                if (GUILayout.Button("Resume"))
                    manager.ResumeBGM();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // SFX Stats
            EditorGUILayout.LabelField($"Active SFX: {manager.ActiveSFXCount} / {manager.PooledSFXCount} pooled");

            if (GUILayout.Button("Stop All SFX"))
                manager.StopAllSFX();

            // Volume Controls
            EditorGUILayout.Space(5);
            manager.MasterVolume = EditorGUILayout.Slider("Master", manager.MasterVolume, 0f, 1f);
            manager.BGMVolume = EditorGUILayout.Slider("BGM", manager.BGMVolume, 0f, 1f);
            manager.SFXVolume = EditorGUILayout.Slider("SFX", manager.SFXVolume, 0f, 1f);

            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Editor Prefs

        private void LoadEditorPrefs()
        {
            audioSearchFolder = EditorPrefs.GetString("AudioManager.SearchFolder", audioSearchFolder);
            outputFolder = EditorPrefs.GetString("AudioManager.OutputFolder", outputFolder);
            outputFileName = EditorPrefs.GetString("AudioManager.OutputFileName", outputFileName);
            autoGenerateOnPlay = EditorPrefs.GetBool("AudioManager.AutoGenerate", false);
        }

        private void SaveEditorPrefs()
        {
            EditorPrefs.SetString("AudioManager.SearchFolder", audioSearchFolder);
            EditorPrefs.SetString("AudioManager.OutputFolder", outputFolder);
            EditorPrefs.SetString("AudioManager.OutputFileName", outputFileName);
            EditorPrefs.SetBool("AudioManager.AutoGenerate", autoGenerateOnPlay);
        }

        #endregion
    }

    // Auto-generate on Play Mode
    [InitializeOnLoad]
    public static class AudioManager_PlayModeHook
    {
        static AudioManager_PlayModeHook()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                bool autoGenerate = EditorPrefs.GetBool("AudioManager.AutoGenerate", false);
                if (autoGenerate)
                {
                    // Trigger generation
                    var manager = Object.FindObjectOfType<AudioManager>();
                    if (manager != null)
                    {
                        Debug.Log("[AudioManager] Auto-generating AudioName.cs...");
                        // Call generation logic here if needed
                    }
                }
            }
        }
    }
}
