using UnityEditor;
using UnityEngine;

namespace Amanotes.Core.Editor
{
/// <summary>
/// Time scale controller overlay on Scene View.
/// Adjust Time.timeScale with slider and preset buttons. Includes pause/resume.
/// Drag title bar to reposition. Double-click title to reset position.
/// Toggle via: Tools > Playable Standard Pipeline > Overlay > Time Scale Controller
/// </summary>
[InitializeOnLoad]
public class TimeScaleOverlay
{
    private const string VisiblePrefKey = "TimeScale_Visible";
    private const string MenuPath = "Tools/Playable Standard Pipeline/Overlay/Time Scale Controller";

    private static bool _visible;
    private static float _timeScale = 1f;
    private static bool _paused;
    private static float _scaleBeforePause = 1f;
    private static OverlayDragHandle _dragHandle;

    private const float DefaultWidth = 280f;
    private const float DefaultHeight = 52f;
    private const float MinWidth = 220f;
    private const float MinHeight = 48f;
    private const float MaxWidth = 500f;
    private const float MaxHeight = 200f;
    private const float Margin = 10f;
    private const float ResizeHandleSize = 14f;
    private const string SizePrefKeyW = "TimeScale_SizeW";
    private const string SizePrefKeyH = "TimeScale_SizeH";

    private static float _panelWidth;
    private static float _panelHeight;
    private static bool _isResizing;
    private static Vector2 _resizeStartMouse;
    private static Vector2 _resizeStartSize;

    private static readonly float[] Presets = { 0.25f, 0.5f, 1f, 2f, 4f };
    private static readonly string[] PresetLabels = { ".25", ".5", "1x", "2x", "4x" };

    static TimeScaleOverlay()
    {
        _visible = EditorPrefs.GetBool(VisiblePrefKey, false);
        _panelWidth = EditorPrefs.GetFloat(SizePrefKeyW, DefaultWidth);
        _panelHeight = EditorPrefs.GetFloat(SizePrefKeyH, DefaultHeight);

        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
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

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            _timeScale = Time.timeScale;
            _paused = false;
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            Time.timeScale = 1f;
            _paused = false;
        }
    }

    private static Vector2 GetDefaultPosition(Rect sceneRect)
    {
        // Top-left corner
        return new Vector2(Margin, Margin);
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!_visible) return;

        Handles.BeginGUI();

        var sceneRect = sceneView.position;

        if (_dragHandle == null)
            _dragHandle = new OverlayDragHandle("TimeScale", GetDefaultPosition(sceneRect));

        var panelRect = _dragHandle.GetPanelRect(sceneRect, _panelWidth, _panelHeight);

        // Process resize
        ProcessResize(panelRect, sceneView);

        GUI.Box(panelRect, GUIContent.none, EditorStyles.helpBox);

        float padding = 6f;
        float y = panelRect.y + padding;
        float x = panelRect.x + padding;
        float contentW = _panelWidth - padding * 2f;
        float btnH = 18f;
        float lineH = 16f;

        // Row 1: Drag button + Title + Close button + Pause button + current value
        var gripRect = OverlayDragHandle.GetDragButtonRect(x, y);
        _dragHandle.ProcessDrag(gripRect, sceneView, true);
        OverlayDragHandle.DrawDragButton(x, y);

        float afterGrip = x + OverlayDragHandle.DragButtonSize + 2f;
        var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 10 };
        GUI.Label(new Rect(afterGrip, y, 80f, lineH), "\u23F1 TimeScale", titleStyle);

        // Close button
        if (OverlayDragHandle.DrawCloseButton(x + contentW - OverlayDragHandle.CloseButtonSize, y))
        {
            _visible = false;
            EditorPrefs.SetBool(VisiblePrefKey, false);
            SceneView.RepaintAll();
            Handles.EndGUI();
            return;
        }

        bool isPlaying = EditorApplication.isPlaying;
        EditorGUI.BeginDisabledGroup(!isPlaying);

        // Pause/Resume button
        float pauseBtnW = 52f;
        if (GUI.Button(new Rect(x + 84f, y - 1f, pauseBtnW, btnH), _paused ? "\u25B6 Play" : "\u23F8 Pause", EditorStyles.miniButton))
        {
            _paused = !_paused;
            if (_paused)
            {
                _scaleBeforePause = _timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                _timeScale = _scaleBeforePause;
                Time.timeScale = _timeScale;
            }
        }

        // Value label
        var valueStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
        string valueText = _paused ? "PAUSED" : string.Format("{0:F2}x", _timeScale);
        GUI.Label(new Rect(x + contentW - 50f - OverlayDragHandle.CloseButtonSize - 4f, y, 50f, lineH), valueText, valueStyle);
        y += lineH + 4f;

        // Row 2: Slider + Preset buttons
        float sliderW = contentW - (PresetLabels.Length * 30f + PresetLabels.Length * 2f) - 4f;
        float sliderH = 14f;
        float sliderCenterY = y + (btnH - sliderH) * 0.5f;

        EditorGUI.BeginDisabledGroup(_paused);

        var newScale = GUI.HorizontalSlider(new Rect(x, sliderCenterY, sliderW, sliderH), _timeScale, 0f, 5f);
        if (!Mathf.Approximately(newScale, _timeScale))
        {
            _timeScale = newScale;
            if (isPlaying) Time.timeScale = _timeScale;
        }

        // Preset buttons
        float presetX = x + sliderW + 4f;
        float presetW = 28f;
        for (int i = 0; i < Presets.Length; i++)
        {
            bool isActive = Mathf.Approximately(_timeScale, Presets[i]);
            var style = isActive ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
            if (GUI.Button(new Rect(presetX, y, presetW, btnH), PresetLabels[i], style))
            {
                _timeScale = Presets[i];
                if (isPlaying) Time.timeScale = _timeScale;
                _paused = false;
            }
            presetX += presetW + 2f;
        }

        EditorGUI.EndDisabledGroup(); // _paused
        EditorGUI.EndDisabledGroup(); // !isPlaying

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
