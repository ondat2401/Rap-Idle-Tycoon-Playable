using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Automatically sets up asmdef files for third-party plugins (DOTween, etc.)
/// when the PlayableCore package is imported into a new project.
/// Runs on editor load via [InitializeOnLoad].
/// </summary>
[InitializeOnLoad]
public class AutoSetupDependencies
{
    private const string SetupDoneKey = "PlayableCore_DependenciesSetup_v1";

    static AutoSetupDependencies()
    {
        // Only run once per project
        if (SessionState.GetBool(SetupDoneKey, false))
            return;

        // Delay to let Unity finish importing
        EditorApplication.delayCall += RunSetup;
    }

    [MenuItem("Tools/Playable Standard Pipeline/Setup Dependencies (ASMDEF)")]
    public static void RunSetupManual()
    {
        RunSetup();
        EditorUtility.DisplayDialog("Setup Dependencies",
            "Dependency setup complete. Check Console for details.", "OK");
    }

    private static void RunSetup()
    {
        SessionState.SetBool(SetupDoneKey, true);

        SetupDOTweenAsmdef();
        // Add more plugin setups here as needed
        // SetupOtherPluginAsmdef();

        AssetDatabase.Refresh();
    }

    private static void SetupDOTweenAsmdef()
    {
        // Common DOTween paths
        string[] possibleModulePaths = new string[]
        {
            "Assets/Plugins/Demigiant/DOTween/Modules",
            "Assets/Demigiant/DOTween/Modules",
            "Assets/DOTween/Modules",
        };

        string modulesPath = null;
        for (int i = 0; i < possibleModulePaths.Length; i++)
        {
            if (Directory.Exists(possibleModulePaths[i]))
            {
                modulesPath = possibleModulePaths[i];
                break;
            }
        }

        if (modulesPath == null)
        {
            Debug.Log("[AutoSetupDependencies] DOTween not found, skipping asmdef setup.");
            return;
        }

        string asmdefPath = modulesPath + "/DOTween.Modules.asmdef";

        if (File.Exists(asmdefPath))
        {
            // Verify content is correct
            string existingContent = File.ReadAllText(asmdefPath);
            if (existingContent.Contains("\"name\": \"DOTween.Modules\"") ||
                existingContent.Contains("\"name\":\"DOTween.Modules\""))
            {
                Debug.Log("[AutoSetupDependencies] DOTween.Modules.asmdef already exists and is valid.");
                return;
            }
        }

        // Create the asmdef
        string asmdefContent = "{\n" +
            "  \"name\": \"DOTween.Modules\",\n" +
            "  \"rootNamespace\": \"\",\n" +
            "  \"references\": [],\n" +
            "  \"includePlatforms\": [],\n" +
            "  \"excludePlatforms\": [],\n" +
            "  \"allowUnsafeCode\": false,\n" +
            "  \"overrideReferences\": true,\n" +
            "  \"precompiledReferences\": [\n" +
            "    \"DOTween.dll\"\n" +
            "  ],\n" +
            "  \"autoReferenced\": false,\n" +
            "  \"defineConstraints\": [],\n" +
            "  \"versionDefines\": [],\n" +
            "  \"noEngineReferences\": false\n" +
            "}";

        File.WriteAllText(asmdefPath, asmdefContent);
        Debug.Log("[AutoSetupDependencies] Created DOTween.Modules.asmdef at: " + asmdefPath);

        // Also create asmdef for DOTween Editor if needed
        SetupDOTweenEditorAsmdef(modulesPath);
    }

    private static void SetupDOTweenEditorAsmdef(string modulesPath)
    {
        // Editor folder is sibling to Modules
        string dotweenRoot = Path.GetDirectoryName(modulesPath);
        string editorPath = dotweenRoot + "/Editor";

        if (!Directory.Exists(editorPath))
            return;

        string editorAsmdefPath = editorPath + "/DOTween.Editor.asmdef";

        if (File.Exists(editorAsmdefPath))
        {
            Debug.Log("[AutoSetupDependencies] DOTween.Editor.asmdef already exists.");
            return;
        }

        string asmdefContent = "{\n" +
            "  \"name\": \"DOTween.Editor\",\n" +
            "  \"rootNamespace\": \"\",\n" +
            "  \"references\": [\n" +
            "    \"DOTween.Modules\"\n" +
            "  ],\n" +
            "  \"includePlatforms\": [\n" +
            "    \"Editor\"\n" +
            "  ],\n" +
            "  \"excludePlatforms\": [],\n" +
            "  \"allowUnsafeCode\": false,\n" +
            "  \"overrideReferences\": true,\n" +
            "  \"precompiledReferences\": [\n" +
            "    \"DOTweenEditor.dll\"\n" +
            "  ],\n" +
            "  \"autoReferenced\": false,\n" +
            "  \"defineConstraints\": [],\n" +
            "  \"versionDefines\": [],\n" +
            "  \"noEngineReferences\": false\n" +
            "}";

        File.WriteAllText(editorAsmdefPath, asmdefContent);
        Debug.Log("[AutoSetupDependencies] Created DOTween.Editor.asmdef at: " + editorAsmdefPath);
    }
}
