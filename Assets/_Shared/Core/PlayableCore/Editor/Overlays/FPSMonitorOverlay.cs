using UnityEditor;
using UnityEngine;

namespace Amanotes.Core.Editor
{
/// <summary>
/// FPS and performance stats overlay on Scene View.
/// Shows FPS, frame time. In Play mode also shows draw calls, batches, triangles.
/// Drag title bar to reposition. Double-click title to reset position.
/// Toggle via: Tools > Playable Standard Pipeline > Overlay > FPS Monitor
/// </summary>
[InitializeOnLoad]
public class FPSMonitorOverlay
{
    private const string VisiblePrefKey = "FPSMonitor_Visible";
    private const string MenuPath = "Tools/Playable Standard Pipeline/Overlay/FPS Monitor";

    private static bool _visible;
    private static OverlayDragHandle _dragHandle;

    private const float DefaultWidth = 180f;
    private const float DefaultHeight = 68f;
    private const float MinWidth = 150f;
    private const float MinHeight = 60f;
    private const float MaxWidth = 400f;
    private const float MaxHeight = 200f;
    private const float Margin = 10f;
    private const float ResizeHandleSize = 14f;
    private const string SizePrefKeyW = "FPSMonitor_SizeW";
    private const string SizePrefKeyH = "FPSMonitor_SizeH";

    private static float _panelWidth;
    private static float _panelHeight;
    private static bool _isResizing;
    private static Vector2 _resizeStartMouse;
    private static Vector2 _resizeStartSize;

    // FPS calculation
    private static int _frameCount;
    private static double _lastTime;
    private static float _fps;
    private static float _frameTime;
    private static readonly float UpdateInterval = 0.5f;

    static FPSMonitorOverlay()
    {
        _visible = EditorPrefs.GetBool(VisiblePrefKey, false);
        _panelWidth = EditorPrefs.GetFloat(SizePrefKeyW, DefaultWidth);
        _panelHeight = EditorPrefs.GetFloat(SizePrefKeyH, DefaultHeight);
        _lastTime = EditorApplication.timeSinceStartup;

        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.update += OnEditorUpdate;
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    [MenuItem(MenuPath)]
    private static void ToggleVisibility()
    {
        _visible = !_visible;
        EditorPrefs.SetBool(VisiblePrefKey, _visible);
        SceneView.RepaintAll();
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleVisibilityValidate()
    {
        Menu.SetChecked(MenuPath, _visible);
        return true;
    }

    private static void OnEditorUpdate()
    {
        if (!_visible) return;

        _frameCount++;
        double elapsed = EditorApplication.timeSinceStartup - _lastTime;
        if (elapsed >= UpdateInterval)
        {
            _fps = (float)(_frameCount / elapsed);
            _frameTime = (float)(elapsed / _frameCount) * 1000f;
            _frameCount = 0;
            _lastTime = EditorApplication.timeSinceStartup;
            SceneView.RepaintAll();
        }
    }

    private static Vector2 GetDefaultPosition(Rect sceneRect)
    {
        // Top-right corner
        return new Vector2(sceneRect.width - _panelWidth - Margin, Margin);
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!_visible) return;

        Handles.BeginGUI();

        var sceneRect = sceneView.position;

        if (_dragHandle == null)
            _dragHandle = new OverlayDragHandle("FPSMonitor", GetDefaultPosition(sceneRect));

        var panelRect = _dragHandle.GetPanelRect(sceneRect, _panelWidth, _panelHeight);

        // Process resize before drawing
        ProcessResize(panelRect, sceneView);

        GUI.Box(panelRect, GUIContent.none, EditorStyles.helpBox);

        float padding = 6f;
        float y = panelRect.y + padding;
        float x = panelRect.x + padding;
        float contentW = _panelWidth - padding * 2f;
        float lineH = 16f;

        // Title with drag button + close button
        var gripRect = OverlayDragHandle.GetDragButtonRect(x, y);
        _dragHandle.ProcessDrag(gripRect, sceneView, true);
        OverlayDragHandle.DrawDragButton(x, y);

        var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 10 };
        GUI.Label(new Rect(x + OverlayDragHandle.DragButtonSize + 2f, y, contentW - OverlayDragHandle.DragButtonSize - OverlayDragHandle.CloseButtonSize - 4f, lineH), "\u26A1 Performance", titleStyle);

        // Close button
        if (OverlayDragHandle.DrawCloseButton(x + contentW - OverlayDragHandle.CloseButtonSize, y))
        {
            _visible = false;
            EditorPrefs.SetBool(VisiblePrefKey, false);
            SceneView.RepaintAll();
            Handles.EndGUI();
            return;
        }
        y += lineH + 2f;

        var labelStyle = new GUIStyle(EditorStyles.miniLabel);

        // FPS with color coding
        var fpsColor = _fps >= 60f ? Color.green : _fps >= 30f ? Color.yellow : Color.red;
        var fpsStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = fpsColor } };
        GUI.Label(new Rect(x, y, contentW * 0.5f, lineH), string.Format("FPS: {0:F0}", _fps), fpsStyle);
        GUI.Label(new Rect(x + contentW * 0.5f, y, contentW * 0.5f, lineH), string.Format("Frame: {0:F1}ms", _frameTime), labelStyle);
        y += lineH;

        // Play mode stats
        if (EditorApplication.isPlaying)
        {
            int tris = UnityStats.triangles;
            int verts = UnityStats.vertices;
            string triStr = tris > 1000000 ? string.Format("{0:F1}M", tris / 1000000f) :
                            tris > 1000 ? string.Format("{0:F1}K", tris / 1000f) : tris.ToString();
            GUI.Label(new Rect(x, y, contentW, lineH),
                string.Format("Tris: {0}  Verts: {1}", triStr, verts > 1000 ? string.Format("{0:F1}K", verts / 1000f) : verts.ToString()),
                labelStyle);
        }
        else
        {
            GUI.Label(new Rect(x, y, contentW, lineH), "Play mode for more stats", labelStyle);
        }

        // Resize grip
        DrawResizeGrip(panelRect);

        Handles.EndGUI();
    }

    private static void ProcessResize(Rect panelRect, SceneView sceneView)
    {
        var e = Event.current;
        var gripRect = new Rect(
            panelRect.xMax - ResizeHandleSize,
            panelRect.yMax - ResizeHandleSize,
            ResizeHandleSize,
            ResizeHandleSize);

        EditorGUIUtility.AddCursorRect(gripRect, MouseCursor.ResizeUpLeft);

        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0 && gripRect.Contains(e.mousePosition))
                {
                    _isResizing = true;
                    _resizeStartMouse = e.mousePosition;
                    _resizeStartSize = new Vector2(_panelWidth, _panelHeight);
                    e.Use();
                }
                break;
            case EventType.MouseDrag:
                if (_isResizing)
                {
                    var delta = e.mousePosition - _resizeStartMouse;
                    _panelWidth = Mathf.Clamp(_resizeStartSize.x + delta.x, MinWidth, MaxWidth);
                    _panelHeight = Mathf.Clamp(_resizeStartSize.y + delta.y, MinHeight, MaxHeight);
                    e.Use();
                    sceneView.Repaint();
                }
                break;
            case EventType.MouseUp:
                if (_isResizing)
                {
                    _isResizing = false;
                    SaveSize();
                    e.Use();
                }
                break;
        }
    }

    private static void SaveSize()
    {
        EditorPrefs.SetFloat(SizePrefKeyW, _panelWidth);
        EditorPrefs.SetFloat(SizePrefKeyH, _panelHeight);
    }

    private static void DrawResizeGrip(Rect panelRect)
    {
        var gripColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
        var oldColor = Handles.color;
        Handles.color = gripColor;
        float gx = panelRect.xMax - 4f;
        float gy = panelRect.yMax - 4f;
        for (int i = 0; i < 3; i++)
        {
            float offset = i * 4f;
            Handles.DrawLine(
                new Vector3(gx - offset, gy, 0),
                new Vector3(gx, gy - offset, 0));
        }
        Handles.color = oldColor;
    }
}
} // namespace Amanotes.Core.Editor
