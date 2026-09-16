using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Amanotes.Core.Editor
{
/// <summary>
/// Live monitor for EventBus activity on Scene View.
/// Shows registered event types, subscriber counts, and recent publishes.
/// Drag title bar to reposition. Double-click title to reset position.
/// Toggle via: Tools > Playable Standard Pipeline > Overlay > EventBus Monitor
/// </summary>
[InitializeOnLoad]
public class EventBusMonitorOverlay
{
    private const string VisiblePrefKey = "EventBusMonitor_Visible";
    private const string MenuPath = "Tools/Playable Standard Pipeline/Overlay/EventBus Monitor";

    private static bool _visible;
    private static OverlayDragHandle _dragHandle;

    private const float DefaultWidth = 260f;
    private const float MinPanelHeight = 50f;
    private const float MinWidth = 200f;
    private const float MaxWidth = 500f;
    private const float Margin = 10f;
    private const float LineHeight = 15f;
    private const int MaxVisibleEvents = 8;
    private const float ResizeHandleSize = 14f;
    private const string SizePrefKeyW = "EventBusMonitor_SizeW";

    private static float _panelWidth;
    private static bool _isResizing;
    private static Vector2 _resizeStartMouse;
    private static float _resizeStartWidth;

    private static readonly Dictionary<string, int> _fireCounts = new Dictionary<string, int>();
    private static readonly List<string> _recentEvents = new List<string>(16);
    private static double _lastRefreshTime;
    private const float RefreshInterval = 0.3f;

    private static FieldInfo _subscribersField;

    static EventBusMonitorOverlay()
    {
        _visible = EditorPrefs.GetBool(VisiblePrefKey, false);
        _panelWidth = EditorPrefs.GetFloat(SizePrefKeyW, DefaultWidth);
        CacheReflection();

        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.update += OnEditorUpdate;
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void CacheReflection()
    {
        _subscribersField = typeof(EventBus).GetField("_subscribers", BindingFlags.NonPublic | BindingFlags.Static);
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
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            _fireCounts.Clear();
            _recentEvents.Clear();
        }
    }

    private static void OnEditorUpdate()
    {
        if (!_visible || !EditorApplication.isPlaying) return;

        double now = EditorApplication.timeSinceStartup;
        if (now - _lastRefreshTime < RefreshInterval) return;
        _lastRefreshTime = now;

        SceneView.RepaintAll();
    }

    private static Dictionary<Type, Delegate> GetSubscribers()
    {
        if (_subscribersField == null) CacheReflection();
        if (_subscribersField == null) return null;
        return _subscribersField.GetValue(null) as Dictionary<Type, Delegate>;
    }

    private static Vector2 GetDefaultPosition(Rect sceneRect)
    {
        // Top-right, below FPS Monitor (FPS height ~68 + margin gap)
        return new Vector2(sceneRect.width - _panelWidth - Margin, Margin + 78f);
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!_visible) return;

        Handles.BeginGUI();

        var subscribers = GetSubscribers();
        int eventCount = subscribers != null ? subscribers.Count : 0;
        int lines = Mathf.Min(eventCount, MaxVisibleEvents);
        float panelHeight = MinPanelHeight + lines * LineHeight;

        var sceneRect = sceneView.position;

        if (_dragHandle == null)
            _dragHandle = new OverlayDragHandle("EventBusMonitor", GetDefaultPosition(sceneRect));

        var panelRect = _dragHandle.GetPanelRect(sceneRect, _panelWidth, panelHeight);

        // Process resize (width only, height is dynamic)
        ProcessResize(panelRect, sceneView);

        GUI.Box(panelRect, GUIContent.none, EditorStyles.helpBox);

        float padding = 6f;
        float y = panelRect.y + padding;
        float x = panelRect.x + padding;
        float contentW = _panelWidth - padding * 2f;

        // Title with drag button + close button
        var gripRect = OverlayDragHandle.GetDragButtonRect(x, y);
        _dragHandle.ProcessDrag(gripRect, sceneView, true);
        OverlayDragHandle.DrawDragButton(x, y);

        float afterGrip = x + OverlayDragHandle.DragButtonSize + 2f;
        var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 10 };
        string status = EditorApplication.isPlaying ? string.Format(" ({0} types)", eventCount) : " (not playing)";
        GUI.Label(new Rect(afterGrip, y, contentW - OverlayDragHandle.DragButtonSize - OverlayDragHandle.CloseButtonSize - 6f, 16f), "\u2709 EventBus" + status, titleStyle);

        // Close button
        if (OverlayDragHandle.DrawCloseButton(x + contentW - OverlayDragHandle.CloseButtonSize, y))
        {
            _visible = false;
            EditorPrefs.SetBool(VisiblePrefKey, false);
            SceneView.RepaintAll();
            Handles.EndGUI();
            return;
        }
        y += 18f;

        if (!EditorApplication.isPlaying || subscribers == null || subscribers.Count == 0)
        {
            var infoStyle = new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Italic };
            GUI.Label(new Rect(x, y, contentW, 14f), EditorApplication.isPlaying ? "No subscribers" : "Enter Play mode to monitor", infoStyle);
            Handles.EndGUI();
            return;
        }

        // Event list
        var labelStyle = new GUIStyle(EditorStyles.miniLabel);
        var countStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
        int shown = 0;
        foreach (var kvp in subscribers)
        {
            if (shown >= MaxVisibleEvents) break;

            string typeName = kvp.Key.Name;
            int subCount = 0;
            if (kvp.Value != null)
            {
                var invList = kvp.Value.GetInvocationList();
                subCount = invList != null ? invList.Length : 0;
            }

            GUI.Label(new Rect(x, y, contentW * 0.6f, LineHeight), typeName, labelStyle);
            GUI.Label(new Rect(x + contentW * 0.6f, y, contentW * 0.4f, LineHeight),
                string.Format("sub:{0}", subCount), countStyle);
            y += LineHeight;
            shown++;
        }

        if (eventCount > MaxVisibleEvents)
        {
            var moreStyle = new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Italic };
            GUI.Label(new Rect(x, y, contentW, 14f), string.Format("... +{0} more", eventCount - MaxVisibleEvents), moreStyle);
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
                    _resizeStartWidth = _panelWidth;
                    e.Use();
                }
                break;
            case EventType.MouseDrag:
                if (_isResizing)
                {
                    var delta = e.mousePosition - _resizeStartMouse;
                    _panelWidth = Mathf.Clamp(_resizeStartWidth + delta.x, MinWidth, MaxWidth);
                    e.Use();
                    sceneView.Repaint();
                }
                break;
            case EventType.MouseUp:
                if (_isResizing)
                {
                    _isResizing = false;
                    EditorPrefs.SetFloat(SizePrefKeyW, _panelWidth);
                    e.Use();
                }
                break;
        }
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
