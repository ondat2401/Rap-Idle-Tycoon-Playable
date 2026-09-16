using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor tool to bulk-remove missing (None) script components from all prefabs in the project.
/// Access via menu: Tools > Remove Missing Scripts In Prefabs
/// </summary>
public class RemoveMissingScripts
{
    [MenuItem("Tools/Playable Standard Pipeline/Remove Missing Scripts In Prefabs")]
    public static void RemoveAll()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        int totalRemoved = 0;
        int prefabsFixed = 0;

        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
                continue;

            int removed = RemoveMissingScriptsRecursive(prefab);

            if (removed > 0)
            {
                totalRemoved += removed;
                prefabsFixed++;
                PrefabUtility.SavePrefabAsset(prefab);
                Debug.Log($"[RemoveMissingScripts] Fixed: {path} (removed {removed} missing scripts)");
            }

            if (i % 50 == 0)
            {
                EditorUtility.DisplayProgressBar(
                    "Removing Missing Scripts",
                    $"Processing {i}/{prefabGuids.Length}...",
                    (float)i / prefabGuids.Length);
            }
        }

        EditorUtility.ClearProgressBar();
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog(
            "Remove Missing Scripts",
            $"Done!\n\nPrefabs fixed: {prefabsFixed}\nTotal missing scripts removed: {totalRemoved}",
            "OK");
    }

    private static int RemoveMissingScriptsRecursive(GameObject go)
    {
        int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

        for (int i = 0; i < go.transform.childCount; i++)
        {
            removed += RemoveMissingScriptsRecursive(go.transform.GetChild(i).gameObject);
        }

        return removed;
    }
}
