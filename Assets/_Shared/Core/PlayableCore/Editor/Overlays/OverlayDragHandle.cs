using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Amanotes.Core.Editor
{
/// <summary>
/// Shared drag-to-reposition helper for Scene View overlays.
/// Each overlay stores its position offset in EditorPrefs.
/// Drag the grip button (≡) to move. Double-click grip to reset position.
/// Automatically resolves overlaps with other registered overlays on drop.
/// </summary>
public class OverlayDragHandle
{
    private readonly string _prefKeyX;
    private readonly string _prefKeyY;
    private readonly Vector2 _defaultPosition;

    private Vector2 _offset;
    private bool _isDragging;
    private Vector2 _dragStartMouse;
    private Vector2 _dragStartOffset;

    public const float DragButtonSize = 18f;
    public const float CloseButtonSize = 16f;
    private const float OverlapPadding = 4f;
    private const int MaxResolveIterations = 10;

    private static GUIStyle _dragButtonStyle;
    private static GUIStyle _closeButtonStyle;

    // Registry: all active handles register their current rect each frame
    private static readonly Dictionary<OverlayDragHandle, Rect> _registry = new Dictionary<OverlayDragHandle, Rect>();

    public OverlayDragHandle(string prefKey, Vector2 defaultPosition)
    {
        _prefKeyX = prefKey + "_PosX";
        _prefKeyY = prefKey + "_PosY";
        _defaultPosition = defaultPosition;

        _offset = new Vector2(
            EditorPrefs.GetFloat(_prefKeyX, 0f),
            EditorPrefs.GetFloat(_prefKeyY, 0f)
        );
    }

    /// <summary>
    /// Returns the final panel Rect after applying drag offset.
    /// Also registers this rect in the global registry for overlap detection.
    /// </summary>
    public Rect GetPanelRect(Rect sceneRect, float panelWidth, float panelHeight)
    {
        float x = _defaultPosition.x + _offset.x;
        float y = _defaultPosition.y + _offset.y;

        x = Mathf.Clamp(x, 0f, sceneRect.width - panelWidth);
        y = Mathf.Clamp(y, 0f, sceneRect.height - panelHeight - 20f);

        var rect = new Rect(x, y, panelWidth, panelHeight);
        _registry[this] = rect;
        return rect;
    }

    public static Rect GetDragButtonRect(float x, float y)
    {
        return new Rect(x, y, DragButtonSize, DragButtonSize);
    }

    public static Rect DrawDragButton(float x, float y)
    {
        var rect = new Rect(x, y, DragButtonSize, DragButtonSize);

        if (_dragButtonStyle == null)
        {
            _dragButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 2),
                fixedHeight = DragButtonSize,
                fixedWidth = DragButtonSize
            };
        }

        GUI.Label(rect, "\u2261", _dragButtonStyle);
        return rect;
    }

    /// <summary>
    /// Draw a close (×) button. Returns true if clicked.
    /// </summary>
    public static bool DrawCloseButton(float x, float y)
    {
        var rect = new Rect(x, y, CloseButtonSize, CloseButtonSize);

        if (_closeButtonStyle == null)
        {
            _closeButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 1),
                fixedHeight = CloseButtonSize,
                fixedWidth = CloseButtonSize
            };
        }

        return GUI.Button(rect, "\u00D7", _closeButtonStyle);
    }

    /// <summary>
    /// Process drag events. On MouseUp resolves overlaps with other registered overlays.
    /// </summary>
    public bool ProcessDrag(Rect dragRect, SceneView sceneView, bool useButtonRect)
    {
        var e = Event.current;
        int controlId = GUIUtility.GetControlID(FocusType.Passive);

        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button == 0 && dragRect.Contains(e.mousePosition))
                {
                    if (e.clickCount == 2)
                    {
                        _offset = Vector2.zero;
                        SavePosition();
                        e.Use();
                        return true;
                    }

                    _isDragging = true;
                    _dragStartMouse = e.mousePosition;
                    _dragStartOffset = _offset;
                    GUIUtility.hotControl = controlId;
                    e.Use();
                    return true;
                }
                break;

            case EventType.MouseDrag:
                if (_isDragging)
                {
                    _offset = _dragStartOffset + (e.mousePosition - _dragStartMouse);
                    e.Use();
                    sceneView.Repaint();
                    return true;
                }
                break;

            case EventType.MouseUp:
                if (_isDragging)
                {
                    _isDragging = false;
                    GUIUtility.hotControl = 0;
                    ResolveOverlaps(sceneView);
                    SavePosition();
                    e.Use();
                    return true;
                }
                break;
        }

        EditorGUIUtility.AddCursorRect(dragRect, MouseCursor.MoveArrow);
        return false;
    }

    /// <summary>
    /// Legacy: process drag using full title bar area.
    /// </summary>
    public bool ProcessDrag(Rect panelRect, SceneView sceneView)
    {
        var dragRect = new Rect(panelRect.x, panelRect.y, panelRect.width, DragButtonSize);
        return ProcessDrag(dragRect, sceneView, true);
    }

    /// <summary>
    /// Push this overlay away from all others until no overlap remains.
    /// Uses iterative separation: finds the shallowest penetration axis and nudges along it.
    /// </summary>
    private void ResolveOverlaps(SceneView sceneView)
    {
        if (!_registry.TryGetValue(this, out var myRect)) return;

        for (int iter = 0; iter < MaxResolveIterations; iter++)
        {
            bool anyOverlap = false;

            foreach (var kvp in _registry)
            {
                if (kvp.Key == this) continue;

                var other = kvp.Value;
                var expanded = new Rect(
                    other.x - OverlapPadding,
                    other.y - OverlapPadding,
                    other.width + OverlapPadding * 2f,
                    other.height + OverlapPadding * 2f);

                if (!myRect.Overlaps(expanded)) continue;

                anyOverlap = true;

                // Compute overlap depths on each axis
                float overlapLeft  = myRect.xMax - expanded.xMin;
                float overlapRight = expanded.xMax - myRect.xMin;
                float overlapUp    = myRect.yMax - expanded.yMin;
                float overlapDown  = expanded.yMax - myRect.yMin;

                // Pick the axis with minimum penetration
                float minH = Mathf.Min(overlapLeft, overlapRight);
                float minV = Mathf.Min(overlapUp, overlapDown);

                Vector2 push;
                if (minH < minV)
                    push = overlapLeft < overlapRight ? new Vector2(-overlapLeft, 0f) : new Vector2(overlapRight, 0f);
                else
                    push = overlapUp < overlapDown ? new Vector2(0f, -overlapUp) : new Vector2(0f, overlapDown);

                _offset += push;

                // Clamp to scene bounds
                float x = Mathf.Clamp(_defaultPosition.x + _offset.x, 0f, sceneView.position.width - myRect.width);
                float y = Mathf.Clamp(_defaultPosition.y + _offset.y, 0f, sceneView.position.height - myRect.height - 20f);
                _offset = new Vector2(x - _defaultPosition.x, y - _defaultPosition.y);

                myRect = new Rect(x, y, myRect.width, myRect.height);
                _registry[this] = myRect;
                break; // re-check all others after each push
            }

            if (!anyOverlap) break;
        }

        sceneView.Repaint();
    }

    private void SavePosition()
    {
        EditorPrefs.SetFloat(_prefKeyX, _offset.x);
        EditorPrefs.SetFloat(_prefKeyY, _offset.y);
    }

    public void ResetPosition()
    {
        _offset = Vector2.zero;
        SavePosition();
    }
}
} // namespace Amanotes.Core.Editor
