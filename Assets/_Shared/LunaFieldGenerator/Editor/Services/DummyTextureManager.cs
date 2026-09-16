using System.IO;
using UnityEditor;
using UnityEngine;
using Amanotes.Core;

namespace Amanotes.LunaFieldGenerator.Editor
{
    /// <summary>
    /// Manages dummy placeholder textures for Luna Fields.
    /// Handles creation, lookup, and validation of per-class dummy textures.
    /// </summary>
    public class DummyTextureManager
    {
        #region Constants

        /// <summary>Fallback path to package-bundled dummy textures.</summary>
        private const string PackageDummyFolderPath = "Assets/Plugins/com.amanotes.playable-luna-field-generator/Dummys";

        #endregion

        #region State

        private string dummyFolderPath;
        private Texture2D defaultTexture;

        #endregion

        #region Constructor

        public DummyTextureManager(string folderPath, Texture2D defaultTex = null)
        {
            dummyFolderPath = folderPath;
            defaultTexture = defaultTex;
        }

        #endregion

        #region Public API

        public string FolderPath
        {
            get => dummyFolderPath;
            set => dummyFolderPath = value;
        }

        public Texture2D DefaultTexture
        {
            get => defaultTexture;
            set => defaultTexture = value;
        }

        /// <summary>Ensure the dummy folder exists on disk.</summary>
        public void EnsureFolderExists()
        {
            if (!Directory.Exists(dummyFolderPath))
            {
                Directory.CreateDirectory(dummyFolderPath);
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// Get or create a dummy texture named after the class.
        /// Reuses existing file if found; otherwise clones defaultTexture or generates checkerboard.
        /// </summary>
        public Texture2D GetOrCreateForClass(string className)
        {
            EnsureFolderExists();

            // Try to find existing file named after the class
            string[] extensions = { ".png", ".jpg", ".jpeg" };
            foreach (string ext in extensions)
            {
                string candidate = Path.Combine(dummyFolderPath, className + ext);
                if (File.Exists(candidate))
                {
                    Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(candidate);
                    if (existing != null) return existing;
                }
            }

            // Create new file named after the class
            string targetPath = Path.Combine(dummyFolderPath, className + ".png");

            if (defaultTexture != null)
            {
                string sourcePath = AssetDatabase.GetAssetPath(defaultTexture);
                if (!string.IsNullOrEmpty(sourcePath))
                {
                    AssetDatabase.CopyAsset(sourcePath, targetPath);
                    AssetDatabase.Refresh();
                    return AssetDatabase.LoadAssetAtPath<Texture2D>(targetPath);
                }
            }

            // Fallback: generate checkerboard
            return CreateCheckerboardTexture(targetPath);
        }

        /// <summary>Load batch of dummy textures for generation.</summary>
        public Texture2D[] LoadOrCreateBatch(int requiredCount)
        {
            EnsureFolderExists();

            var textures = new System.Collections.Generic.List<Texture2D>(requiredCount);

            if (Directory.Exists(dummyFolderPath))
            {
                string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { dummyFolderPath });
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (tex != null) textures.Add(tex);
                }
            }

            if (textures.Count == 0)
            {
                string defaultPath = Path.Combine(dummyFolderPath, "dummy_default.png");
                Texture2D defaultTex = defaultTexture != null
                    ? CopyTexture(AssetDatabase.GetAssetPath(defaultTexture), defaultPath)
                    : CreateCheckerboardTexture(defaultPath);
                textures.Add(defaultTex);
            }

            while (textures.Count < requiredCount)
            {
                Texture2D source = textures[textures.Count - 1];
                string newPath = Path.Combine(dummyFolderPath, $"dummy_{textures.Count + 1}.png");
                Texture2D copy = CopyTexture(AssetDatabase.GetAssetPath(source), newPath);
                textures.Add(copy ?? source);
            }

            return textures.ToArray();
        }

        /// <summary>
        /// Reload default texture from EditorPrefs or dummy folder.
        /// Call in OnEnable to ensure texture survives domain reload.
        /// Falls back to package Dummys folder if main folder is empty.
        /// </summary>
        public void ReloadDefaultTexture()
        {
            if (defaultTexture != null) return;

            string savedPath = EditorPrefs.GetString("LunaFieldEditor_DefaultTexture", "");
            if (!string.IsNullOrEmpty(savedPath))
            {
                defaultTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(savedPath);
                if (defaultTexture != null) return;
            }

            // Try main dummy folder first
            defaultTexture = FindFirstTextureInFolder(dummyFolderPath);
            if (defaultTexture != null) return;

            // Fallback: scan package Dummys folder
            defaultTexture = FindFirstTextureInFolder(PackageDummyFolderPath);
        }

        /// <summary>Search a folder for any Texture2D, preferring "dummy_default" named files.</summary>
        private Texture2D FindFirstTextureInFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath)) return null;

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
            if (guids.Length == 0) return null;

            // Prefer dummy_default named file
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string fileName = Path.GetFileNameWithoutExtension(path);
                if (fileName.StartsWith("dummy_default"))
                {
                    Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (tex != null) return tex;
                }
            }

            // Fallback: first texture found
            return AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        /// <summary>Create a Sprite from a Texture2D (for SpriteRenderer fields).</summary>
        public Sprite CreateSpriteFromTexture(Texture2D texture)
        {
            if (texture == null) return null;

            string path = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null)
            {
                bool needsReimport = false;
                if (!importer.isReadable) { importer.isReadable = true; needsReimport = true; }
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    needsReimport = true;
                }
                if (needsReimport) AssetDatabase.ImportAsset(path);
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                sprite = Sprite.Create(texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), 100f);
            }
            return sprite;
        }

        #endregion

        #region Private Helpers

        private Texture2D CreateCheckerboardTexture(string targetPath)
        {
            Texture2D tex = new Texture2D(128, 128);
            Color[] colors = new Color[128 * 128];
            for (int i = 0; i < colors.Length; i++)
            {
                int x = i % 128;
                int y = i / 128;
                bool checker = ((x / 16) + (y / 16)) % 2 == 0;
                colors[i] = checker ? new Color(0.8f, 0.8f, 0.8f) : new Color(0.6f, 0.6f, 0.6f);
            }
            tex.SetPixels(colors);
            tex.Apply();
            File.WriteAllBytes(targetPath, tex.EncodeToPNG());
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(targetPath);
        }

        private Texture2D CopyTexture(string sourcePath, string destPath)
        {
            if (string.IsNullOrEmpty(sourcePath)) return null;
            AssetDatabase.CopyAsset(sourcePath, destPath);
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(destPath);
        }

        #endregion
    }
}
