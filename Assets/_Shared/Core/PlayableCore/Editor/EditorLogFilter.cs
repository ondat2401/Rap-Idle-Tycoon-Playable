using UnityEditor;
using UnityEngine;

namespace Amanotes.Core.Editor
{
    /// <summary>
    /// Fixes "Texture is not accessible" warnings from TextMeshPro and InputSystem
    /// package icons by enabling Read/Write on their texture import settings.
    /// Runs once on Editor load via [InitializeOnLoad].
    /// </summary>
    [InitializeOnLoad]
    public static class EditorLogFilter
    {
        private static readonly string[] TexturePaths = new string[]
        {
            "Packages/com.unity.textmeshpro/Editor Resources/Gizmos/TMP - Text Component Icon.psd",
            "Packages/com.unity.inputsystem/InputSystem/Editor/Icons/InputSystemUIInputModule@64.png"
        };

        static EditorLogFilter()
        {
            EditorApplication.delayCall += FixTextureImportSettings;
        }

        private static void FixTextureImportSettings()
        {
            bool anyChanged = false;

            for (int i = 0; i < TexturePaths.Length; i++)
            {
                var importer = AssetImporter.GetAtPath(TexturePaths[i]) as TextureImporter;
                if (importer == null) continue;

                if (!importer.isReadable)
                {
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                    anyChanged = true;
                    Debug.Log("[EditorLogFilter] Fixed texture: " + TexturePaths[i]);
                }
            }

            if (anyChanged)
            {
                Debug.Log("[EditorLogFilter] Texture import settings updated. Warning should no longer appear.");
            }
        }
    }
}
