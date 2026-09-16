using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Amanotes.Core.Editor
{
/// <summary>
/// Sticky note overlay on Scene View for quick TODO/notes.
/// Features: scroll, resize, task-style note separators.
/// Persists per-scene notes in EditorPrefs.
/// Toggle via: Tools > Playable Standard Pipeline > Overlay > Quick Note
/// </summary>
[InitializeOnLoad]
public class QuickNoteOverlay
{
    private const string VisiblePrefKey = "QuickNote_Visible";
    private const string NotesPrefPrefix = "QuickNotes_";
    private const string SizePrefPrefix = "QuickNote_Size_";
    private const string MenuPath = "Tools/Playable Standard Pipeline/Overlay/Quick Note";

    private static bool _visible;
    private static List<NoteItem> _notes = new List<NoteItem>();
    private static string _currentScenePath = "";
    private static OverlayDragHandle _dragHandle;
    private static Vector2 _scrollPos;

    // Resizable panel
    private static float _panelWidth = 260f;
    private static float _panelHeight = 200f;
    private const float MinWidth = 200f;
    private const float MinHeight = 120f;
    private const float MaxWidth = 500f;
    private const float MaxHeight = 600f;
    private const float Margin = 10f;
    private const float ResizeHandleSize = 14f;

    // Resize state
    private static bool _isResizing;
    private static Vector2 _resizeStartMouse;
    private static Vector2 _resizeStartSize;

    // New note input
    private static string _newNoteText = "";
    private static int _editingIndex = -1;
    private static string _editingText = "";

    private struct NoteItem
    {
        public string text;
        public bool done;
    }

    static QuickNoteOverlay()
    {
        _visible = EditorPrefs.GetBool(VisiblePrefKey, false);
        LoadForCurrentScene();

        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
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

    private static void OnHierarchyChanged()
    {
        var scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        if (scenePath != _currentScenePath)
        {
            LoadForCurrentScene();
            SceneView.RepaintAll();
        }
    }

    private static string GetSceneKey()
    {
        return string.IsNullOrEmpty(_currentScenePath) ? "Untitled" : _currentScenePath;
    }

    private static void LoadForCurrentScene()
    {
        _currentScenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
        string key = GetSceneKey();

        // Load size
        _panelWidth = EditorPrefs.GetFloat(SizePrefPrefix + key + "_W", 260f);
        _panelHeight = EditorPrefs.GetFloat(SizePrefPrefix + key + "_H", 200f);

        // Load notes as JSON-like string: "text|done\ntext|done\n..."
        _notes.Clear();
        string raw = EditorPrefs.GetString(NotesPrefPrefix + key, "");
        if (!string.IsNullOrEmpty(raw))
        {
            var lines = raw.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrEmpty(line)) continue;
                int sep = line.LastIndexOf('|');
                if (sep < 0)
                {
                    _notes.Add(new NoteItem { text = line, done = false });
                }
                else
                {
                    _notes.Add(new NoteItem
                    {
                        text = line.Substring(0, sep),
                        done = line.Substring(sep + 1) == "1"
                    });
                }
            }
        }
        _scrollPos = Vector2.zero;
        _editingIndex = -1;
    }

    private static void SaveNotes()
    {
        string key = GetSceneKey();
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < _notes.Count; i++)
        {
            if (i > 0) sb.Append('\n');
            // Escape newlines in text
            sb.Append(_notes[i].text.Replace("\n", "\\n"));
            sb.Append('|');
            sb.Append(_notes[i].done ? "1" : "0");
        }
        EditorPrefs.SetString(NotesPrefPrefix + key, sb.ToString());
    }

    private static void SaveSize()
    {
        string key = GetSceneKey();
        EditorPrefs.SetFloat(SizePrefPrefix + key + "_W", _panelWidth);
        EditorPrefs.SetFloat(SizePrefPrefix + key + "_H", _panelHeight);
    }

    private static Vector2 GetDefaultPosition(Rect sceneRect)
    {
        // Bottom-right corner
        return new Vector2(sceneRect.width - _panelWidth - Margin, sceneRect.height - _panelHeight - Margin - 20f);
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!_visible) return;

        Handles.BeginGUI();

        var sceneRect = sceneView.position;

        if (_dragHandle == null)
            _dragHandle = new OverlayDragHandle("QuickNote", GetDefaultPosition(sceneRect));

        var panelRect = _dragHandle.GetPanelRect(sceneRect, _panelWidth, _panelHeight);

        // Process resize before drag so resize takes priority at corner
        ProcessResize(panelRect, sceneView);

        // Background
        GUI.Box(panelRect, GUIContent.none, EditorStyles.helpBox);

        float pad = 6f;
        float y = panelRect.y + pad;
        float x = panelRect.x + pad;
        float contentW = _panelWidth - pad * 2f;

        // === Header: drag button + title ===
        var gripRect = OverlayDragHandle.GetDragButtonRect(x, y);
        _dragHandle.ProcessDrag(gripRect, sceneView, true);
        OverlayDragHandle.DrawDragButton(x, y);

        var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 10 };
        GUI.Label(new Rect(x + OverlayDragHandle.DragButtonSize + 2f, y, contentW - OverlayDragHandle.DragButtonSize - 52f - OverlayDragHandle.CloseButtonSize - 4f, 16f), "\uD83D\uDCDD Notes", titleStyle);

        // Close button
        if (OverlayDragHandle.DrawCloseButton(x + contentW - OverlayDragHandle.CloseButtonSize, y))
        {
            _visible = false;
            EditorPrefs.SetBool(VisiblePrefKey, false);
            SceneView.RepaintAll();
            Handles.EndGUI();
            return;
        }

        if (GUI.Button(new Rect(x + contentW - 44f - OverlayDragHandle.CloseButtonSize - 4f, y, 44f, 16f), "Clear", EditorStyles.miniButton))
        {
            _notes.Clear();
            SaveNotes();
        }
        y += 20f;

        // === Add new note input ===
        float inputH = 18f;
        float addBtnW = 30f;
        var inputRect = new Rect(x, y, contentW - addBtnW - 4f, inputH);

        // SetNextControlName BEFORE the TextField so focus name is registered
        GUI.SetNextControlName("QuickNoteInput");
        _newNoteText = GUI.TextField(inputRect, _newNoteText, EditorStyles.toolbarTextField);

        if (GUI.Button(new Rect(inputRect.xMax + 2f, y, addBtnW, inputH), "+", EditorStyles.miniButton))
        {
            AddNote();
        }
        // Enter key to add when input is focused
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return
            && GUI.GetNameOfFocusedControl() == "QuickNoteInput")
        {
            AddNote();
            Event.current.Use();
        }
        y += inputH + 4f;

        // === Separator ===
        DrawSeparator(x, y, contentW);
        y += 4f;

        // === Scrollable note list ===
        float scrollAreaH = panelRect.yMax - y - pad - ResizeHandleSize;
        if (scrollAreaH < 20f) scrollAreaH = 20f;

        var scrollViewRect = new Rect(x, y, contentW, scrollAreaH);
        float totalContentH = CalculateContentHeight(contentW);

        // Mouse scroll
        if (scrollViewRect.Contains(Event.current.mousePosition)
            && Event.current.type == EventType.ScrollWheel)
        {
            _scrollPos.y += Event.current.delta.y * 20f;
            _scrollPos.y = Mathf.Clamp(_scrollPos.y, 0f, Mathf.Max(0f, totalContentH - scrollAreaH));
            Event.current.Use();
            sceneView.Repaint();
        }

        _scrollPos = GUI.BeginScrollView(scrollViewRect,
            _scrollPos,
            new Rect(0, 0, contentW - 14f, totalContentH));

        DrawNoteList(contentW - 14f);

        GUI.EndScrollView();

        // === Resize grip visual ===
        DrawResizeGrip(panelRect);

        Handles.EndGUI();
    }

    private static void AddNote()
    {
        if (string.IsNullOrEmpty(_newNoteText.Trim())) return;
        _notes.Insert(0, new NoteItem { text = _newNoteText.Trim(), done = false });
        _newNoteText = "";
        SaveNotes();
        _scrollPos = Vector2.zero;
    }

    private static float CalculateContentHeight(float width)
    {
        float h = 0f;
        for (int i = 0; i < _notes.Count; i++)
        {
            h += GetNoteItemHeight(i, width);
            if (i < _notes.Count - 1) h += 5f; // separator spacing
        }
        return Mathf.Max(h, 1f);
    }

    private static float GetNoteItemHeight(int index, float width)
    {
        // Checkbox(18) + text + delete btn row
        float textW = width - 18f - 22f; // checkbox + delete btn
        var style = GetNoteTextStyle(_notes[index].done);
        float textH = style.CalcHeight(new GUIContent(_notes[index].text), textW);
        return Mathf.Max(textH, 18f) + 2f;
    }

    private static GUIStyle GetNoteTextStyle(bool done)
    {
        var style = new GUIStyle(EditorStyles.label)
        {
            fontSize = 11,
            wordWrap = true,
            richText = true,
            normal = { textColor = done ? new Color(0.5f, 0.5f, 0.5f) : EditorStyles.label.normal.textColor }
        };
        return style;
    }

    private static void DrawNoteList(float width)
    {
        float y = 0f;
        for (int i = 0; i < _notes.Count; i++)
        {
            float itemH = GetNoteItemHeight(i, width);
            DrawNoteItem(i, new Rect(0, y, width, itemH));
            y += itemH;

            // Separator between notes
            if (i < _notes.Count - 1)
            {
                y += 2f;
                var oldColor = GUI.color;
                GUI.color = new Color(0.4f, 0.4f, 0.4f, 0.5f);
                GUI.Box(new Rect(0, y, width, 1f), GUIContent.none);
                GUI.color = oldColor;
                y += 3f;
            }
        }
    }

    private static void DrawNoteItem(int index, Rect rect)
    {
        var note = _notes[index];
        float checkSize = 16f;
        float delBtnW = 18f;

        // Checkbox
        var checkRect = new Rect(rect.x, rect.y + 1f, checkSize, checkSize);
        EditorGUI.BeginChangeCheck();
        bool newDone = GUI.Toggle(checkRect, note.done, GUIContent.none);
        if (EditorGUI.EndChangeCheck())
        {
            note.done = newDone;
            _notes[index] = note;
            SaveNotes();
        }

        // Text (or edit field)
        float textX = rect.x + checkSize + 2f;
        float textW = rect.width - checkSize - delBtnW - 6f;
        var textRect = new Rect(textX, rect.y, textW, rect.height);

        if (_editingIndex == index)
        {
            EditorGUI.BeginChangeCheck();
            _editingText = EditorGUI.TextField(textRect, _editingText);
            if (EditorGUI.EndChangeCheck()) { }

            // Confirm on Enter or lost focus
            if ((Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
                || (Event.current.type == EventType.MouseDown && !textRect.Contains(Event.current.mousePosition)))
            {
                if (!string.IsNullOrEmpty(_editingText.Trim()))
                {
                    note.text = _editingText.Trim();
                    _notes[index] = note;
                    SaveNotes();
                }
                _editingIndex = -1;
            }
        }
        else
        {
            var style = GetNoteTextStyle(note.done);
            string displayText = note.done ? $"<i><color=#888>{note.text}</color></i>" : note.text;
            GUI.Label(textRect, displayText, style);

            // Double-click to edit
            if (Event.current.type == EventType.MouseDown && Event.current.clickCount == 2
                && textRect.Contains(Event.current.mousePosition))
            {
                _editingIndex = index;
                _editingText = note.text;
                Event.current.Use();
            }
        }

        // Delete button
        var delRect = new Rect(rect.xMax - delBtnW, rect.y + 1f, delBtnW, 16f);
        if (GUI.Button(delRect, "×", EditorStyles.miniButton))
        {
            _notes.RemoveAt(index);
            SaveNotes();
            if (_editingIndex == index) _editingIndex = -1;
            else if (_editingIndex > index) _editingIndex--;
        }
    }

    // === Resize logic ===
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

    private static void DrawResizeGrip(Rect panelRect)
    {
        // Draw small triangular grip indicator at bottom-right
        var gripColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
        var oldColor = Handles.color;
        Handles.color = gripColor;

        float gx = panelRect.xMax - 4f;
        float gy = panelRect.yMax - 4f;

        // Three small diagonal lines
        for (int i = 0; i < 3; i++)
        {
            float offset = i * 4f;
            Handles.DrawLine(
                new Vector3(gx - offset, gy, 0),
                new Vector3(gx, gy - offset, 0));
        }
        Handles.color = oldColor;
    }

    private static void DrawSeparator(float x, float y, float width)
    {
        var oldColor = GUI.color;
        GUI.color = new Color(0.4f, 0.4f, 0.4f, 0.4f);
        GUI.Box(new Rect(x, y, width, 1f), GUIContent.none);
        GUI.color = oldColor;
    }
}
} // namespace Amanotes.Core.Editor
