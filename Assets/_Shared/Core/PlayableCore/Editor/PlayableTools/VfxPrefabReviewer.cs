using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

/// <summary>
/// Editor window for quickly reviewing VFX particle prefabs.
/// Shows preview thumbnails in a grid + live preview.
/// Access via menu: Tools > Playable Standard Pipeline > VFX Prefab Reviewer
/// </summary>
public class VfxPrefabReviewer : EditorWindow
{
    private List<GameObject> prefabs = new List<GameObject>();
    private Vector2 scrollPos;
    private Vector2 rightPanelScroll;
    private int selectedIndex = -1;
    private string searchFolder = "Assets/ExportedAsset/Prefabs/Vfxs";
    private Object searchFolderAsset;
    private string searchFilter = "";
    private int thumbnailSize = 80;
    private Editor gameObjectEditor;
    private bool showLivePreview = true;
    private bool needsRepaint;

    // Preview rendering
    private PreviewRenderUtility previewRender;
    private GameObject previewInstance;
    private ParticleSystem[] previewParticles;
    private float previewTime;
    private int previewPrefabIndex = -1;
    private float previewSpeed = 1f;
    private float previewCamDist = 5f;
    private float previewCamAngle = 20f;
    private double lastEditorTime;

    // Thumbnail cache
    private Dictionary<int, Texture2D> thumbnailCache = new Dictionary<int, Texture2D>();
    private int thumbnailGenIndex = -1;
    private float thumbnailSimTime = 0.5f; // simulate to this time for screenshot

    [MenuItem("Tools/Playable Standard Pipeline/VFX Prefab Reviewer")]
    public static void ShowWindow()
    {
        var window = GetWindow<VfxPrefabReviewer>("VFX Reviewer");
        window.minSize = new Vector2(500, 400);
        window.LoadPrefabs();
    }

    private void OnEnable()
    {
        searchFolderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(searchFolder);
        LoadPrefabs();
    }

    private void OnDisable()
    {
        CleanupPreview();
        if (gameObjectEditor != null)
        {
            DestroyImmediate(gameObjectEditor);
        }
    }

    private void CleanupPreview()
    {
        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
            previewInstance = null;
        }
        if (previewRender != null)
        {
            previewRender.Cleanup();
            previewRender = null;
        }
        previewParticles = null;
        previewPrefabIndex = -1;
    }

    private void Update()
    {
        // Keep repainting while previews are loading
        if (needsRepaint)
        {
            Repaint();
        }
    }

    private void LoadPrefabs()
    {
        prefabs.Clear();
        selectedIndex = -1;
        thumbnailCache.Clear();
        thumbnailGenIndex = -1;

        if (!AssetDatabase.IsValidFolder(searchFolder))
        {
            Debug.LogWarning("[VFX Reviewer] Folder not found: " + searchFolder);
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { searchFolder });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);

            if (!string.IsNullOrEmpty(searchFilter) && !fileName.ToLower().Contains(searchFilter.ToLower()))
                continue;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;

            // Only include prefabs that have ParticleSystem
            if (prefab.GetComponentInChildren<ParticleSystem>(true) != null)
            {
                prefabs.Add(prefab);
            }
        }

        Debug.Log("[VFX Reviewer] Found " + prefabs.Count + " VFX prefabs in: " + searchFolder);
        needsRepaint = true;
    }

    private void OnGUI()
    {
        // Handle keyboard navigation
        HandleKeyboardInput();

        DrawToolbar();
        EditorGUILayout.Space(4);

        if (prefabs.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "No VFX prefabs found.\nFolder: " + searchFolder + "\nFilter: " + searchFilter +
                "\n\nMake sure the folder path is correct and click Refresh.",
                MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginHorizontal();

        // Left panel: grid
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.55f));
        DrawGrid();
        EditorGUILayout.EndVertical();

        // Right panel: preview & info
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
        DrawPreviewPanel();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        EditorGUILayout.LabelField("Folder:", GUILayout.Width(45));

        // Drag-drop folder field
        Object newFolderAsset = EditorGUILayout.ObjectField(
            searchFolderAsset, typeof(DefaultAsset), false, GUILayout.Width(200));

        if (newFolderAsset != searchFolderAsset)
        {
            string path = AssetDatabase.GetAssetPath(newFolderAsset);
            if (AssetDatabase.IsValidFolder(path))
            {
                searchFolderAsset = newFolderAsset;
                searchFolder = path;
                LoadPrefabs();
            }
        }

        // Browse button
        if (GUILayout.Button("…", EditorStyles.toolbarButton, GUILayout.Width(22)))
        {
            string selected = EditorUtility.OpenFolderPanel("Select VFX Folder", "Assets", "");
            if (!string.IsNullOrEmpty(selected))
            {
                // Convert absolute path to relative Assets path
                if (selected.Contains("Assets"))
                {
                    int assetsIndex = selected.IndexOf("Assets");
                    searchFolder = selected.Substring(assetsIndex);
                    searchFolderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(searchFolder);
                    LoadPrefabs();
                }
            }
        }

        EditorGUILayout.LabelField("Filter:", GUILayout.Width(40));
        searchFilter = EditorGUILayout.TextField(searchFilter, GUILayout.Width(80));

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            LoadPrefabs();
        }

        EditorGUILayout.LabelField("(" + prefabs.Count + ")", GUILayout.Width(40));

        thumbnailSize = EditorGUILayout.IntSlider(thumbnailSize, 48, 160, GUILayout.Width(140));

        EditorGUILayout.EndHorizontal();
    }

    private void DrawGrid()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        float availableWidth = position.width * 0.55f - 20f;
        int columns = Mathf.Max(1, Mathf.FloorToInt(availableWidth / (thumbnailSize + 10)));

        needsRepaint = false;

        int col = 0;
        EditorGUILayout.BeginHorizontal();

        for (int i = 0; i < prefabs.Count; i++)
        {
            if (col >= columns)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                col = 0;
            }

            DrawPrefabTile(i);
            col++;
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
    }

    private void DrawPrefabTile(int index)
    {
        bool isSelected = (index == selectedIndex);

        EditorGUILayout.BeginVertical(GUILayout.Width(thumbnailSize + 6));

        // Use colored background for selected
        Color prevBg = GUI.backgroundColor;
        if (isSelected)
        {
            GUI.backgroundColor = new Color(0.3f, 0.8f, 1f, 1f);
        }

        // Get or generate thumbnail
        Texture2D thumb = GetThumbnail(index);

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
        btnStyle.imagePosition = ImagePosition.ImageAbove;
        btnStyle.padding = new RectOffset(2, 2, 2, 2);

        GUIContent content = thumb != null
            ? new GUIContent(thumb)
            : EditorGUIUtility.IconContent("ParticleSystem Icon");

        if (GUILayout.Button(content, btnStyle, GUILayout.Width(thumbnailSize), GUILayout.Height(thumbnailSize - 16)))
        {
            selectedIndex = index;
            Selection.activeObject = prefabs[index];
            EditorGUIUtility.PingObject(prefabs[index]);

            if (gameObjectEditor != null)
            {
                DestroyImmediate(gameObjectEditor);
                gameObjectEditor = null;
            }
        }

        GUI.backgroundColor = prevBg;

        // Particle count badge
        GUIStyle countStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
        if (isSelected)
        {
            countStyle.normal.textColor = new Color(0.3f, 0.9f, 1f);
            countStyle.fontStyle = FontStyle.Bold;
        }

        // Label
        string label = prefabs[index].name;
        if (label.Length > 14)
            label = label.Substring(0, 13) + "…";
        EditorGUILayout.LabelField(label, countStyle, GUILayout.Width(thumbnailSize));

        EditorGUILayout.EndVertical();
    }

    private Texture2D GetThumbnail(int index)
    {
        if (thumbnailCache.ContainsKey(index))
            return thumbnailCache[index];

        // Queue generation - generate one per frame to avoid lag
        if (thumbnailGenIndex == -1)
        {
            thumbnailGenIndex = index;
            EditorApplication.delayCall += GenerateNextThumbnail;
        }

        return null;
    }

    private void GenerateNextThumbnail()
    {
        if (thumbnailGenIndex < 0 || thumbnailGenIndex >= prefabs.Count)
        {
            thumbnailGenIndex = -1;
            return;
        }

        int index = thumbnailGenIndex;
        thumbnailGenIndex = -1;

        Texture2D thumb = RenderThumbnail(prefabs[index]);
        if (thumb != null)
        {
            thumbnailCache[index] = thumb;
        }

        // Find next un-generated thumbnail
        for (int i = 0; i < prefabs.Count; i++)
        {
            if (!thumbnailCache.ContainsKey(i))
            {
                thumbnailGenIndex = i;
                EditorApplication.delayCall += GenerateNextThumbnail;
                break;
            }
        }

        Repaint();
    }

    private Texture2D RenderThumbnail(GameObject prefab)
    {
        PreviewRenderUtility thumbRender = null;
        GameObject thumbInstance = null;

        try
        {
            thumbRender = new PreviewRenderUtility();

            thumbInstance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            thumbInstance.hideFlags = HideFlags.HideAndDontSave;
            thumbRender.AddSingleGO(thumbInstance);

            // Simulate particles to get some emission
            var particles = thumbInstance.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var p in particles)
            {
                if (p != null)
                {
                    p.Simulate(thumbnailSimTime, true, true);
                }
            }

            // Calculate bounds
            var renderers = thumbInstance.GetComponentsInChildren<Renderer>(true);
            Bounds bounds;
            if (renderers.Length > 0)
            {
                bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
                if (bounds.size.magnitude < 0.1f)
                {
                    bounds = new Bounds(Vector3.zero, Vector3.one * 3f);
                }
            }
            else
            {
                bounds = new Bounds(Vector3.zero, Vector3.one * 3f);
            }

            // Setup camera
            float camDist = Mathf.Max(bounds.size.magnitude, 1f) * 2f;
            thumbRender.camera.transform.position = bounds.center + new Vector3(0f, camDist * 0.3f, -camDist);
            thumbRender.camera.transform.LookAt(bounds.center);
            thumbRender.camera.nearClipPlane = 0.01f;
            thumbRender.camera.farClipPlane = camDist * 10f;
            thumbRender.camera.clearFlags = CameraClearFlags.SolidColor;
            thumbRender.camera.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);

            // Render using BeginPreview (not BeginStaticPreview) to avoid matrix stack issues
            int size = Mathf.Max(thumbnailSize * 2, 64);
            Rect rect = new Rect(0, 0, size, size);
            thumbRender.BeginPreview(rect, GUIStyle.none);
            thumbRender.camera.Render();
            var previewTex = thumbRender.EndPreview();

            // Copy to persistent Texture2D
            if (previewTex != null)
            {
                RenderTexture prevRT = RenderTexture.active;
                RenderTexture rt = previewTex as RenderTexture;
                if (rt == null)
                {
                    // previewTex is already Texture2D in some Unity versions
                    var t2d = previewTex as Texture2D;
                    if (t2d != null) return t2d;
                    return null;
                }

                Texture2D thumb = new Texture2D(size, size, TextureFormat.RGBA32, false);
                RenderTexture.active = rt;
                thumb.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                thumb.Apply();
                RenderTexture.active = prevRT;
                return thumb;
            }

            return null;
        }
        catch (System.Exception)
        {
            // Silently skip failed thumbnails
            return null;
        }
        finally
        {
            if (thumbInstance != null)
                DestroyImmediate(thumbInstance);
            if (thumbRender != null)
                thumbRender.Cleanup();
        }
    }

    private void DrawPreviewPanel()
    {
        if (selectedIndex < 0 || selectedIndex >= prefabs.Count)
        {
            EditorGUILayout.HelpBox("Select a prefab from the grid to preview.", MessageType.Info);
            return;
        }

        rightPanelScroll = EditorGUILayout.BeginScrollView(rightPanelScroll);

        GameObject selected = prefabs[selectedIndex];

        // Navigation
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = selectedIndex > 0;
        if (GUILayout.Button("◀ Prev", GUILayout.Width(60)))
        {
            SelectIndex(selectedIndex - 1);
        }
        GUI.enabled = true;

        GUIStyle centerStyle = new GUIStyle(EditorStyles.label);
        centerStyle.alignment = TextAnchor.MiddleCenter;
        EditorGUILayout.LabelField((selectedIndex + 1) + " / " + prefabs.Count, centerStyle);

        GUI.enabled = selectedIndex < prefabs.Count - 1;
        if (GUILayout.Button("Next ▶", GUILayout.Width(60)))
        {
            SelectIndex(selectedIndex + 1);
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(selected.name, EditorStyles.boldLabel);
        EditorGUILayout.LabelField(AssetDatabase.GetAssetPath(selected), EditorStyles.miniLabel);

        EditorGUILayout.Space(4);

        // Particle system info
        var particles = selected.GetComponentsInChildren<ParticleSystem>(true);
        EditorGUILayout.LabelField("Particle Systems: " + particles.Length, EditorStyles.boldLabel);

        for (int i = 0; i < particles.Length; i++)
        {
            var ps = particles[i];
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField(ps.gameObject.name, EditorStyles.miniLabel, GUILayout.Width(130));
            var main = ps.main;
            EditorGUILayout.LabelField("Max:" + main.maxParticles, EditorStyles.miniLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("Dur:" + main.duration.ToString("F1") + "s", EditorStyles.miniLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField(main.loop ? "Loop" : "Once", EditorStyles.miniLabel, GUILayout.Width(35));
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(8);

        // Interactive particle preview
        showLivePreview = EditorGUILayout.Toggle("Live Preview", showLivePreview);

        if (showLivePreview)
        {
            SetupPreviewForPrefab(selected, selectedIndex);

            Rect previewRect = GUILayoutUtility.GetRect(200, 200, GUILayout.ExpandWidth(true));

            if (previewRender != null && previewInstance != null && Event.current.type == EventType.Repaint)
            {
                // Calculate real delta time
                double currentTime = EditorApplication.timeSinceStartup;
                float deltaTime = (float)(currentTime - lastEditorTime);
                lastEditorTime = currentTime;

                // Clamp delta to avoid jumps when window was inactive
                deltaTime = Mathf.Clamp(deltaTime, 0f, 0.1f);

                // Apply speed multiplier to delta
                float scaledDelta = deltaTime * previewSpeed;

                // Advance preview time
                previewTime += scaledDelta;

                // Auto-loop: find max duration and reset when exceeded
                float maxDuration = GetMaxDuration();
                if (maxDuration > 0f && previewTime > maxDuration)
                {
                    previewTime = 0f;
                    // Restart particles for clean loop
                    if (previewParticles != null)
                    {
                        foreach (var p in previewParticles)
                        {
                            if (p != null)
                            {
                                p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                                p.Play(true);
                            }
                        }
                    }
                }

                // Incremental simulation: advance by scaledDelta, no restart
                if (previewParticles != null)
                {
                    foreach (var p in previewParticles)
                    {
                        if (p != null)
                        {
                            p.Simulate(scaledDelta, true, false);
                        }
                    }
                }

                // Auto-fit camera based on particle bounds
                Bounds bounds = CalculatePreviewBounds();
                float boundsSize = Mathf.Max(bounds.size.magnitude, 1f);
                float camDist = boundsSize * previewCamDist * 0.5f;

                float angleRad = previewCamAngle * Mathf.Deg2Rad;
                Vector3 camPos = bounds.center + new Vector3(0f, Mathf.Sin(angleRad) * camDist, -Mathf.Cos(angleRad) * camDist);

                previewRender.camera.transform.position = camPos;
                previewRender.camera.transform.LookAt(bounds.center);
                previewRender.camera.nearClipPlane = 0.01f;
                previewRender.camera.farClipPlane = camDist * 10f;
                previewRender.camera.clearFlags = CameraClearFlags.SolidColor;
                previewRender.camera.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);

                // Render preview
                previewRender.BeginPreview(previewRect, GUIStyle.none);
                previewRender.camera.Render();
                Texture resultRender = previewRender.EndPreview();
                GUI.DrawTexture(previewRect, resultRender, ScaleMode.StretchToFill, false);
            }

            // Controls
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset", GUILayout.Width(50)))
            {
                previewTime = 0f;
                if (previewParticles != null)
                {
                    foreach (var p in previewParticles)
                    {
                        if (p != null)
                        {
                            p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                            p.Play(true);
                            p.Pause(true);
                        }
                    }
                }
            }
            EditorGUILayout.LabelField("T:" + previewTime.ToString("F2") + "s", EditorStyles.miniLabel, GUILayout.Width(60));
            EditorGUILayout.LabelField("Speed:", EditorStyles.miniLabel, GUILayout.Width(42));
            previewSpeed = EditorGUILayout.Slider(previewSpeed, 0.1f, 5f);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Zoom:", EditorStyles.miniLabel, GUILayout.Width(42));
            previewCamDist = EditorGUILayout.Slider(previewCamDist, 0.5f, 20f);
            EditorGUILayout.LabelField("Angle:", EditorStyles.miniLabel, GUILayout.Width(42));
            previewCamAngle = EditorGUILayout.Slider(previewCamAngle, -89f, 89f);
            EditorGUILayout.EndHorizontal();

            Repaint();
        }

        EditorGUILayout.Space(4);
        if (GUILayout.Button("Open Prefab", GUILayout.Height(24)))
        {
            AssetDatabase.OpenAsset(selected);
        }

        EditorGUILayout.EndScrollView();
    }

    private void HandleKeyboardInput()
    {
        Event e = Event.current;
        if (e.type != EventType.KeyDown || prefabs.Count == 0 || selectedIndex < 0)
            return;

        if (e.keyCode == KeyCode.LeftArrow)
        {
            SelectIndex(selectedIndex - 1);
            e.Use();
        }
        else if (e.keyCode == KeyCode.RightArrow)
        {
            SelectIndex(selectedIndex + 1);
            e.Use();
        }
    }

    private void SelectIndex(int newIndex)
    {
        selectedIndex = Mathf.Clamp(newIndex, 0, prefabs.Count - 1);
        Selection.activeObject = prefabs[selectedIndex];

        if (gameObjectEditor != null)
        {
            DestroyImmediate(gameObjectEditor);
            gameObjectEditor = null;
        }
    }

    private void SetupPreviewForPrefab(GameObject prefab, int index)
    {
        if (previewPrefabIndex == index && previewRender != null && previewInstance != null)
            return;

        // Cleanup old
        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
            previewInstance = null;
        }
        if (previewRender != null)
        {
            previewRender.Cleanup();
            previewRender = null;
        }

        // Create new preview
        previewRender = new PreviewRenderUtility();

        previewInstance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        previewInstance.hideFlags = HideFlags.HideAndDontSave;

        // Add instance to preview scene
        previewRender.AddSingleGO(previewInstance);

        previewParticles = previewInstance.GetComponentsInChildren<ParticleSystem>(true);

        // Stop all particles initially then play so we simulate incrementally
        foreach (var p in previewParticles)
        {
            if (p != null)
            {
                p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                p.Play(true);
                p.Pause(true);
            }
        }

        previewTime = 0f;
        previewPrefabIndex = index;
        lastEditorTime = EditorApplication.timeSinceStartup;
    }

    private Bounds CalculatePreviewBounds()
    {
        if (previewInstance == null)
            return new Bounds(Vector3.zero, Vector3.one);

        var renderers = previewInstance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(previewInstance.transform.position, Vector3.one * 2f);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i].bounds.size.sqrMagnitude > 0.001f)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        // If bounds are too small (particles haven't emitted yet), use a default
        if (bounds.size.magnitude < 0.1f)
        {
            bounds = new Bounds(previewInstance.transform.position, Vector3.one * 3f);
        }

        return bounds;
    }

    private float GetMaxDuration()
    {
        if (previewParticles == null || previewParticles.Length == 0)
            return 2f;

        float maxDur = 0f;
        foreach (var p in previewParticles)
        {
            if (p != null)
            {
                float dur = p.main.duration + p.main.startLifetime.constantMax;
                if (p.main.loop)
                {
                    dur = p.main.duration;
                }
                if (dur > maxDur)
                    maxDur = dur;
            }
        }

        return maxDur > 0f ? maxDur : 2f;
    }
}
