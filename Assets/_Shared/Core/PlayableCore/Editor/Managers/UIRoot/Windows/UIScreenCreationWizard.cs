using UnityEngine;
using UnityEditor;
using System.IO;
using Amanotes.Core;

namespace Amanotes.Core.Editor
{
    public class UIScreenCreationWizard : EditorWindow
    {
        private string screenName = "NewScreen";
        private bool useAnimation = true;
        private bool generatePrefab = true;
        private string savePath = "Assets/_Project/_Scripts/GUI";

        public static void ShowWindow()
        {
            var window = GetWindow<UIScreenCreationWizard>("Create UI Screen");
            window.minSize = new Vector2(400, 250);
            window.maxSize = new Vector2(400, 250);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("UI Screen Creation Wizard", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            screenName = EditorGUILayout.TextField("Screen Name:", screenName);
            useAnimation = EditorGUILayout.Toggle("Use Animation:", useAnimation);
            generatePrefab = EditorGUILayout.Toggle("Generate Prefab:", generatePrefab);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Save Location:");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField(savePath);
            if (GUILayout.Button("Browse", GUILayout.Width(80)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Save Location", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                    savePath = "Assets" + path.Substring(Application.dataPath.Length);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(20);

            if (GUILayout.Button("Create Screen", GUILayout.Height(40)))
                CreateScreen();
        }

        private void CreateScreen()
        {
            if (string.IsNullOrEmpty(screenName))
            {
                EditorUtility.DisplayDialog("Error", "Screen name cannot be empty", "OK");
                return;
            }

            string className = screenName.Replace(" ", "") + "Screen";
            string scriptPath = Path.Combine(savePath, className + ".cs");

            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);

            string baseClass = useAnimation ? "AnimatedGUIBase" : "GUIBase";
            string scriptContent = GenerateScriptContent(className, baseClass);

            File.WriteAllText(scriptPath, scriptContent);
            AssetDatabase.Refresh();

            Debug.Log($"[UIScreenCreationWizard] Created UI Screen script: {scriptPath}");

            if (generatePrefab)
                EditorApplication.delayCall += () => GeneratePrefab(className);

            Close();
        }

        private string GenerateScriptContent(string className, string baseClass)
        {
            return
    $@"using UnityEngine;
    using UnityEngine.UI;

    public class {className} : {baseClass}
    {{
        //[Header(""UI References"")]
        // Add your UI component references here

        public override void OnScreenInitialize()
        {{
            base.OnScreenInitialize();
            // Initialize UI components
            GUIManager.Instance.RegisterScreen(this);
        }}

        public override void OnScreenShow()
        {{
            base.OnScreenShow();
            // Called when screen is shown
        }}

        public override void OnScreenHide()
        {{
            base.OnScreenHide();
            // Called when screen is hidden
        }}
    }}";
        }

        private void GeneratePrefab(string className)
        {
            if (EditorApplication.isCompiling)
            {
                EditorApplication.delayCall += () => GeneratePrefab(className);
                return;
            }

            GameObject screenGO = new GameObject(className);
            screenGO.AddComponent<Canvas>();
            screenGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            screenGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            screenGO.AddComponent<CanvasGroup>();

            System.Type screenType = System.Type.GetType($"{className}, Assembly-CSharp");
            if (screenType != null)
                screenGO.AddComponent(screenType);

            string prefabPath = Path.Combine("Assets/Prefabs/UI", className + ".prefab");
            string prefabDir = Path.GetDirectoryName(prefabPath);

            if (!Directory.Exists(prefabDir))
                Directory.CreateDirectory(prefabDir);

            PrefabUtility.SaveAsPrefabAsset(screenGO, prefabPath);
            DestroyImmediate(screenGO);

            Debug.Log($"[UIScreenCreationWizard] Created prefab: {prefabPath}");
            EditorUtility.DisplayDialog("Success",
                $"Screen created successfully!\n\nScript: {savePath}\nPrefab: {prefabPath}", "OK");
        }
    }
}
