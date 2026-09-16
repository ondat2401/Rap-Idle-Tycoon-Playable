using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// One-time utility to merge Assets/__Project into Assets/___TH3.
/// Run via menu: Tools > Merge __Project into ___TH3
/// Safe to delete this script after running.
/// </summary>
public class MergeProjectFolder
{
    private const string Source = "Assets/__Project";
    private const string Destination = "Assets/___TH3";

    private static int movedCount = 0;
    private static int errorCount = 0;
    private static List<string> errors = new List<string>();

    [MenuItem("Tools/Playable Standard Pipeline/Merge __Project into ___TH3")]
    public static void Execute()
    {
        if (!AssetDatabase.IsValidFolder(Source))
        {
            EditorUtility.DisplayDialog("Error", "Source folder not found: " + Source, "OK");
            return;
        }

        bool confirm = EditorUtility.DisplayDialog(
            "Merge Folders",
            "This will move all contents from:\n" + Source + "\n\nInto:\n" + Destination +
            "\n\nMapping:\n" +
            "• Art → Art\n" +
            "• Audios → Audios\n" +
            "• Element → Element\n" +
            "• Materials → Materials\n" +
            "• Prefabs → Prefabs\n" +
            "• Prefabs 1 → Prefabs\n" +
            "• Textures → Textures\n" +
            "• Textures 1 → Textures\n" +
            "• NameEditor → NameEditor\n\n" +
            "Continue?",
            "Yes, Merge",
            "Cancel");

        if (!confirm)
            return;

        movedCount = 0;
        errorCount = 0;
        errors.Clear();

        // Define merge mapping: source subfolder -> destination subfolder
        MergeSubfolder("Art", "Art");
        MergeSubfolder("Audios", "Audios");
        MergeSubfolder("Element", "Element");
        MergeSubfolder("Materials", "Materials");
        MergeSubfolder("Prefabs", "Prefabs");
        MergeSubfolder("Prefabs 1", "Prefabs");
        MergeSubfolder("Textures", "Textures");
        MergeSubfolder("Textures 1", "Textures");
        MergeSubfolder("NameEditor", "NameEditor");

        AssetDatabase.Refresh();

        // Try to delete the now-empty source folder
        TryDeleteEmptyFolders(Source);
        AssetDatabase.Refresh();

        string message = "Merge complete!\n\nMoved: " + movedCount + " assets\nErrors: " + errorCount;
        if (errors.Count > 0)
        {
            message += "\n\nErrors:";
            for (int i = 0; i < Mathf.Min(errors.Count, 10); i++)
            {
                message += "\n• " + errors[i];
            }
        }

        EditorUtility.DisplayDialog("Merge Result", message, "OK");
        Debug.Log("[MergeProjectFolder] " + message);
    }

    private static void MergeSubfolder(string srcSubfolder, string dstSubfolder)
    {
        string srcPath = Source + "/" + srcSubfolder;
        string dstPath = Destination + "/" + dstSubfolder;

        if (!AssetDatabase.IsValidFolder(srcPath))
        {
            Debug.Log("[MergeProjectFolder] Skipping (not found): " + srcPath);
            return;
        }

        // Ensure destination folder exists
        EnsureFolder(dstPath);

        // Move all contents recursively
        MoveContents(srcPath, dstPath);
    }

    private static void MoveContents(string srcFolder, string dstFolder)
    {
        // Get all assets in source folder (non-recursive first level)
        string[] guids = AssetDatabase.FindAssets("", new[] { srcFolder });

        // Collect direct children only
        HashSet<string> processed = new HashSet<string>();

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);

            // Only process direct children of srcFolder
            string relativePath = assetPath.Substring(srcFolder.Length + 1);
            if (relativePath.Contains("/"))
            {
                // This is a nested item, get the top-level subfolder
                string topLevel = relativePath.Split('/')[0];
                string subSrcPath = srcFolder + "/" + topLevel;

                if (processed.Contains(subSrcPath))
                    continue;
                processed.Add(subSrcPath);

                if (AssetDatabase.IsValidFolder(subSrcPath))
                {
                    string subDstPath = dstFolder + "/" + topLevel;
                    if (AssetDatabase.IsValidFolder(subDstPath))
                    {
                        // Destination subfolder exists, merge recursively
                        MoveContents(subSrcPath, subDstPath);
                    }
                    else
                    {
                        // Move entire subfolder
                        MoveAsset(subSrcPath, subDstPath);
                    }
                }
            }
            else
            {
                if (processed.Contains(assetPath))
                    continue;
                processed.Add(assetPath);

                // Direct file, move it
                string fileName = Path.GetFileName(assetPath);
                string dstAssetPath = dstFolder + "/" + fileName;

                if (AssetDatabase.IsValidFolder(assetPath))
                {
                    if (AssetDatabase.IsValidFolder(dstAssetPath))
                    {
                        MoveContents(assetPath, dstAssetPath);
                    }
                    else
                    {
                        MoveAsset(assetPath, dstAssetPath);
                    }
                }
                else
                {
                    MoveAsset(assetPath, dstAssetPath);
                }
            }
        }
    }

    private static void MoveAsset(string src, string dst)
    {
        // Check if destination already exists
        if (!AssetDatabase.IsValidFolder(src) && File.Exists(dst))
        {
            // Rename with suffix to avoid conflict
            string dir = Path.GetDirectoryName(dst);
            string nameNoExt = Path.GetFileNameWithoutExtension(dst);
            string ext = Path.GetExtension(dst);
            dst = dir + "/" + nameNoExt + "_merged" + ext;
        }

        string result = AssetDatabase.MoveAsset(src, dst);
        if (string.IsNullOrEmpty(result))
        {
            movedCount++;
        }
        else
        {
            errorCount++;
            errors.Add(src + " → " + result);
            Debug.LogWarning("[MergeProjectFolder] Failed: " + src + " → " + dst + " | " + result);
        }
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        string parent = Path.GetDirectoryName(folderPath).Replace("\\", "/");
        string folderName = Path.GetFileName(folderPath);

        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static void TryDeleteEmptyFolders(string folderPath)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
            return;

        string[] subFolders = AssetDatabase.GetSubFolders(folderPath);
        foreach (string sub in subFolders)
        {
            TryDeleteEmptyFolders(sub);
        }

        // Check if folder is now empty
        string[] remaining = AssetDatabase.FindAssets("", new[] { folderPath });
        if (remaining.Length == 0)
        {
            AssetDatabase.DeleteAsset(folderPath);
            Debug.Log("[MergeProjectFolder] Deleted empty folder: " + folderPath);
        }
    }
}
