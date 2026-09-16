using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amanotes.Core;

namespace Amanotes.Core.Editor
{
    public class BatchAtlasPacker : EditorWindow
    {
        private List<string> folders = new List<string>();
        private Vector2 scrollPos;
        private int padding = 2;
        private int maxSize = 2048;
        private string outputFolder = "Assets/Atlases";

        [MenuItem("Tools/Playable Standard Pipeline/Atlas Packer/Batch Atlas Packer")]
        public static void ShowWindow()
        {
            GetWindow<BatchAtlasPacker>("Batch Atlas Packer");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Batch Atlas Packer", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Create separate atlases for each selected folder.", MessageType.Info);

            EditorGUILayout.Space();

            // Settings
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Output Folder:");
            if (GUILayout.Button("Choose", GUILayout.Width(80)))
            {
                string path = EditorUtility.OpenFolderPanel("Select output folder", Application.dataPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                        outputFolder = "Assets" + path.Substring(Application.dataPath.Length);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(outputFolder, EditorStyles.miniLabel);

            padding = EditorGUILayout.IntSlider("Padding", padding, 0, 10);
            maxSize = EditorGUILayout.IntPopup("Max Size", maxSize, 
                new string[] { "1024", "2048", "4096", "8192" },
                new int[] { 1024, 2048, 4096, 8192 });

            EditorGUILayout.Space();

            // Folders list
            EditorGUILayout.LabelField("Folders to Pack:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Folder", GUILayout.Height(25)))
            {
                string path = EditorUtility.OpenFolderPanel("Select folder with textures", Application.dataPath, "");
                if (!string.IsNullOrEmpty(path) && path.StartsWith(Application.dataPath))
                {
                    string relativePath = "Assets" + path.Substring(Application.dataPath.Length);
                    if (!folders.Contains(relativePath))
                    {
                        folders.Add(relativePath);
                    }
                }
            }
            if (GUILayout.Button("Clear All", GUILayout.Height(25)))
            {
                folders.Clear();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
            for (int i = 0; i < folders.Count; i++)
            {
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField(folders[i]);
                if (GUILayout.Button("×", GUILayout.Width(25)))
                {
                    folders.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(folders.Count == 0);
            if (GUILayout.Button($"Pack {folders.Count} Atlases", GUILayout.Height(35)))
            {
                PackBatch();
            }
            EditorGUI.EndDisabledGroup();
        }

        void PackBatch()
        {
            int successCount = 0;
            int failCount = 0;

            for (int i = 0; i < folders.Count; i++)
            {
                string folder = folders[i];
                string folderName = Path.GetFileName(folder);

                EditorUtility.DisplayProgressBar("Batch Packing", 
                    $"Packing {folderName}...", (float)i / folders.Count);

                try
                {
                    if (PackFolder(folder, folderName))
                        successCount++;
                    else
                        failCount++;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[BatchAtlasPacker] Failed to pack {folder}: {ex.Message}");
                    failCount++;
                }
            }

            EditorUtility.ClearProgressBar();

            EditorUtility.DisplayDialog("Batch Packing Complete", 
                $"Success: {successCount}\nFailed: {failCount}", "OK");

            AssetDatabase.Refresh();
        }

        bool PackFolder(string folder, string atlasName)
        {
            // Find all textures in folder
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });

            if (guids.Length == 0)
            {
                Debug.LogWarning($"[BatchAtlasPacker] No textures found in {folder}");
                return false;
            }

            List<Texture2D> textures = new List<Texture2D>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex != null)
                {
                    textures.Add(tex);
                }
            }

            if (textures.Count == 0)
                return false;

            // Create readable copies
            Texture2D[] copies = new Texture2D[textures.Count];
            for (int i = 0; i < textures.Count; i++)
            {
                copies[i] = MakeTextureReadableCopy(textures[i]);
            }

            // Pack
            Texture2D atlas = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Rect[] rects = atlas.PackTextures(copies, padding, maxSize);

            // Save
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            byte[] png = atlas.EncodeToPNG();
            string savePath = $"{outputFolder}/{atlasName}_atlas.png";
            File.WriteAllBytes(savePath, png);

            // Cleanup
            foreach (var copy in copies)
            {
                DestroyImmediate(copy);
            }
            DestroyImmediate(atlas);

            // Import and configure
            AssetDatabase.ImportAsset(savePath);
            ConfigureAtlas(savePath, textures, rects);

            Debug.Log($"[BatchAtlasPacker] Created atlas: {savePath} with {textures.Count} sprites");

            return true;
        }

        void ConfigureAtlas(string path, List<Texture2D> textures, Rect[] rects)
        {
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return;

            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Multiple;

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            Texture2D atlasLoaded = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            int atlasW = atlasLoaded.width;
            int atlasH = atlasLoaded.height;

            SpriteMetaData[] metas = new SpriteMetaData[rects.Length];
            for (int i = 0; i < rects.Length; i++)
            {
                Rect r = rects[i];
                int x = Mathf.RoundToInt(r.x * atlasW);
                int y = Mathf.RoundToInt(r.y * atlasH);
                int w = Mathf.RoundToInt(r.width * atlasW);
                int h = Mathf.RoundToInt(r.height * atlasH);
                y = atlasH - y - h;

                metas[i] = new SpriteMetaData
                {
                    name = textures[i].name,
                    rect = new Rect(x, y, w, h),
                    pivot = new Vector2(0.5f, 0.5f),
                    alignment = (int)SpriteAlignment.Center
                };
            }

            ti.spritesheet = metas;
            ti.SaveAndReimport();
        }

        Texture2D MakeTextureReadableCopy(Texture2D src)
        {
            RenderTexture rt = RenderTexture.GetTemporary(src.width, src.height, 0);
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;

            Texture2D readable = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            readable.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            return readable;
        }
    }
}
