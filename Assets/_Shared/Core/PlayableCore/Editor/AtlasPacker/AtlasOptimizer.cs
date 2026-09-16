using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Amanotes.Core;

namespace Amanotes.Core.Editor
{
    public class AtlasOptimizer : EditorWindow
    {
        private Texture2D atlasTexture;
        private List<string> unusedSprites = new List<string>();
        private Vector2 scrollPos;

        [MenuItem("Tools/Playable Standard Pipeline/Atlas Packer/Atlas Optimizer")]
        public static void ShowWindow()
        {
            GetWindow<AtlasOptimizer>("Atlas Optimizer");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Atlas Optimizer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Find unused sprites in atlas to optimize atlas size.", MessageType.Info);

            EditorGUILayout.Space();

            atlasTexture = (Texture2D)EditorGUILayout.ObjectField("Atlas Texture", atlasTexture, typeof(Texture2D), false);

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(atlasTexture == null);
            if (GUILayout.Button("Analyze Atlas", GUILayout.Height(30)))
            {
                AnalyzeAtlas();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            if (unusedSprites.Count > 0)
            {
                EditorGUILayout.LabelField($"Unused Sprites: {unusedSprites.Count}", EditorStyles.boldLabel);

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));

                foreach (var spriteName in unusedSprites)
                {
                    EditorGUILayout.LabelField($"• {spriteName}");
                }

                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("These sprites are not used anywhere in your project.\nConsider removing them to reduce atlas size.", MessageType.Warning);

                if (GUILayout.Button("Export List"))
                {
                    ExportUnusedList();
                }
            }
            else if (atlasTexture != null)
            {
                EditorGUILayout.HelpBox("All sprites are being used! ✓", MessageType.Info);
            }
        }

        void AnalyzeAtlas()
        {
            unusedSprites.Clear();

            if (atlasTexture == null) return;

            EditorUtility.DisplayProgressBar("Analyzing Atlas", "Loading sprites...", 0f);

            // Get all sprites from atlas
            string path = AssetDatabase.GetAssetPath(atlasTexture);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            Sprite[] atlasSprites = assets.OfType<Sprite>().ToArray();

            if (atlasSprites.Length == 0)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("No Sprites", "No sprites found in the selected atlas.", "OK");
                return;
            }

            // Create usage map
            HashSet<string> usedSprites = new HashSet<string>();

            EditorUtility.DisplayProgressBar("Analyzing Atlas", "Searching scenes...", 0.3f);

            // Search in all scenes
            string[] sceneGUIDs = AssetDatabase.FindAssets("t:Scene");
            foreach (string guid in sceneGUIDs)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);

                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (var root in rootObjects)
                {
                    FindUsedSprites(root, usedSprites);
                }

                UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
            }

            EditorUtility.DisplayProgressBar("Analyzing Atlas", "Searching prefabs...", 0.6f);

            // Search in all prefabs
            string[] prefabGUIDs = AssetDatabase.FindAssets("t:Prefab");
            for (int i = 0; i < prefabGUIDs.Length; i++)
            {
                if (i % 50 == 0)
                {
                    EditorUtility.DisplayProgressBar("Analyzing Atlas", 
                        $"Searching prefabs {i}/{prefabGUIDs.Length}...", 
                        0.6f + (0.3f * i / prefabGUIDs.Length));
                }

                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGUIDs[i]);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                if (prefab != null)
                {
                    FindUsedSprites(prefab, usedSprites);
                }
            }

            EditorUtility.DisplayProgressBar("Analyzing Atlas", "Finalizing...", 0.95f);

            // Find unused sprites
            foreach (var sprite in atlasSprites)
            {
                if (!usedSprites.Contains(sprite.name))
                {
                    unusedSprites.Add(sprite.name);
                }
            }

            EditorUtility.ClearProgressBar();

            Debug.Log($"[AtlasOptimizer] Analysis complete: {unusedSprites.Count}/{atlasSprites.Length} sprites are unused");

            if (unusedSprites.Count == 0)
            {
                EditorUtility.DisplayDialog("Analysis Complete", 
                    $"All {atlasSprites.Length} sprites in the atlas are being used!", "OK");
            }
        }

        void FindUsedSprites(GameObject go, HashSet<string> usedSprites)
        {
            // Check SpriteRenderer
            var spriteRenderer = go.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                usedSprites.Add(spriteRenderer.sprite.name);
            }

            // Check UI Image
            var image = go.GetComponent<UnityEngine.UI.Image>();
            if (image != null && image.sprite != null)
            {
                usedSprites.Add(image.sprite.name);
            }

            // Recursively check children
            foreach (Transform child in go.transform)
            {
                FindUsedSprites(child.gameObject, usedSprites);
            }
        }

        void ExportUnusedList()
        {
            string path = EditorUtility.SaveFilePanel("Export Unused Sprites", "", "unused_sprites.txt", "txt");

            if (string.IsNullOrEmpty(path)) return;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"Unused Sprites Report - {System.DateTime.Now}");
            sb.AppendLine($"Atlas: {atlasTexture.name}");
            sb.AppendLine($"Total unused: {unusedSprites.Count}");
            sb.AppendLine();
            sb.AppendLine("Unused Sprites:");

            foreach (var sprite in unusedSprites)
            {
                sb.AppendLine($"  • {sprite}");
            }

            System.IO.File.WriteAllText(path, sb.ToString());

            EditorUtility.DisplayDialog("Export Complete", $"Unused sprites list exported to:\n{path}", "OK");
        }
    }
}
