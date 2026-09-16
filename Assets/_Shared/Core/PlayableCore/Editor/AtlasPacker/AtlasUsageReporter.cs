using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using Amanotes.Core;

namespace Amanotes.Core.Editor
{
    public class AtlasUsageReporter : EditorWindow
    {
        private Texture2D atlasTexture;
        private List<UsageInfo> usages = new List<UsageInfo>();
        private Vector2 scrollPos;
        private bool searchInScenes = true;
        private bool searchInPrefabs = true;

        private class UsageInfo
        {
            public GameObject gameObject;
            public Component component;
            public Sprite sprite;
            public string scenePath;
            public bool isInScene;
        }

        [MenuItem("Tools/Playable Standard Pipeline/Atlas Packer/Atlas Usage Reporter")]
        public static void ShowWindow()
        {
            GetWindow<AtlasUsageReporter>("Atlas Usage Reporter");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Atlas Usage Reporter", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Find all GameObjects that use sprites from a specific atlas.", MessageType.Info);

            EditorGUILayout.Space();

            atlasTexture = (Texture2D)EditorGUILayout.ObjectField("Atlas Texture", atlasTexture, typeof(Texture2D), false);

            searchInScenes = EditorGUILayout.Toggle("Search in Open Scenes", searchInScenes);
            searchInPrefabs = EditorGUILayout.Toggle("Search in Prefabs", searchInPrefabs);

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(atlasTexture == null);
            if (GUILayout.Button("Find Usages", GUILayout.Height(30)))
            {
                FindUsages();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            if (usages.Count > 0)
            {
                EditorGUILayout.LabelField($"Found {usages.Count} usages:", EditorStyles.boldLabel);

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                foreach (var usage in usages)
                {
                    EditorGUILayout.BeginVertical("box");

                    EditorGUILayout.BeginHorizontal();

                    // Icon
                    Texture2D icon = usage.isInScene ? 
                        EditorGUIUtility.IconContent("SceneAsset Icon").image as Texture2D : 
                        EditorGUIUtility.IconContent("Prefab Icon").image as Texture2D;
                    GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));

                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField($"{usage.gameObject.name} ({usage.component.GetType().Name})", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Sprite: {usage.sprite.name}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"Location: {usage.scenePath}", EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();

                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        Selection.activeObject = usage.gameObject;
                        EditorGUIUtility.PingObject(usage.gameObject);
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space();

                if (GUILayout.Button("Export to CSV"))
                {
                    ExportToCSV();
                }
            }
        }

        void FindUsages()
        {
            usages.Clear();

            if (atlasTexture == null) return;

            // Get all sprites from atlas
            string path = AssetDatabase.GetAssetPath(atlasTexture);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            Sprite[] atlasSprites = assets.OfType<Sprite>().ToArray();

            if (atlasSprites.Length == 0)
            {
                EditorUtility.DisplayDialog("No Sprites", "No sprites found in the selected atlas.", "OK");
                return;
            }

            HashSet<string> spriteNames = new HashSet<string>(atlasSprites.Select(s => s.name));

            Debug.Log($"[AtlasUsageReporter] Searching for usages of {atlasSprites.Length} sprites...");

            // Search in scenes
            if (searchInScenes)
            {
                SearchInScenes(spriteNames);
            }

            // Search in prefabs
            if (searchInPrefabs)
            {
                SearchInPrefabs(spriteNames);
            }

            Debug.Log($"[AtlasUsageReporter] Found {usages.Count} usages");

            if (usages.Count == 0)
            {
                EditorUtility.DisplayDialog("No Usages", "No usages found for this atlas.", "OK");
            }
        }

        void SearchInScenes(HashSet<string> spriteNames)
        {
            for (int i = 0; i < UnityEditor.SceneManagement.EditorSceneManager.sceneCount; i++)
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                GameObject[] rootObjects = scene.GetRootGameObjects();

                foreach (var root in rootObjects)
                {
                    SearchInGameObject(root, spriteNames, true, scene.path);
                }
            }
        }

        void SearchInPrefabs(HashSet<string> spriteNames)
        {
            string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab");

            for (int i = 0; i < prefabGUIDs.Length; i++)
            {
                if (i % 10 == 0)
                {
                    EditorUtility.DisplayProgressBar("Searching Prefabs", 
                        $"Checking prefab {i}/{prefabGUIDs.Length}...", (float)i / prefabGUIDs.Length);
                }

                string guid = prefabGUIDs[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab != null)
                {
                    SearchInGameObject(prefab, spriteNames, false, path);
                }
            }

            EditorUtility.ClearProgressBar();
        }

        void SearchInGameObject(GameObject go, HashSet<string> spriteNames, bool isInScene, string scenePath)
        {
            // Check SpriteRenderer
            var spriteRenderer = go.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null && spriteNames.Contains(spriteRenderer.sprite.name))
            {
                usages.Add(new UsageInfo
                {
                    gameObject = go,
                    component = spriteRenderer,
                    sprite = spriteRenderer.sprite,
                    scenePath = scenePath,
                    isInScene = isInScene
                });
            }

            // Check UI Image
            var image = go.GetComponent<Image>();
            if (image != null && image.sprite != null && spriteNames.Contains(image.sprite.name))
            {
                usages.Add(new UsageInfo
                {
                    gameObject = go,
                    component = image,
                    sprite = image.sprite,
                    scenePath = scenePath,
                    isInScene = isInScene
                });
            }

            // Recursively check children
            foreach (Transform child in go.transform)
            {
                SearchInGameObject(child.gameObject, spriteNames, isInScene, scenePath);
            }
        }

        void ExportToCSV()
        {
            string path = EditorUtility.SaveFilePanel("Export Usage Report", "", "atlas_usage_report.csv", "csv");

            if (string.IsNullOrEmpty(path)) return;

            System.Text.StringBuilder csv = new System.Text.StringBuilder();
            csv.AppendLine("GameObject,Component,Sprite,Location,Type");

            foreach (var usage in usages)
            {
                csv.AppendLine($"{usage.gameObject.name},{usage.component.GetType().Name},{usage.sprite.name},{usage.scenePath},{(usage.isInScene ? "Scene" : "Prefab")}");
            }

            System.IO.File.WriteAllText(path, csv.ToString());

            EditorUtility.DisplayDialog("Export Complete", $"Usage report exported to:\n{path}", "OK");
            Debug.Log($"[AtlasUsageReporter] Exported to: {path}");
        }
    }
}
