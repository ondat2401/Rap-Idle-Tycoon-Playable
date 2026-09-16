using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Amanotes.Core.Editor
{
/// <summary>
/// Scene View overlay with two independent volume channels: Music and SFX.
/// Applies directly to the game's AudioSources while in Play Mode, so it edits
/// the actual song/BGM and sound-effect volumes of the project (not the global
/// AudioListener). Sources are classified by clip length: long clips are treated
/// as Music, short clips as SFX.
/// Values persist in EditorPrefs across domain reloads.
/// Drag title bar to reposition. Toggle via Tools > Playable Standard Pipeline > Editor Volume Control.
/// </summary>
[InitializeOnLoad]
public class VolumeControlOverlay
{
    private const string MusicVolKey = "EditorVolumeControl_MusicVolume";
    private const string MusicMuteKey = "EditorVolumeControl_MusicMuted";
    private const string SfxVolKey = "EditorVolumeControl_SfxVolume";
    private const string SfxMuteKey = "EditorVolumeControl_SfxMuted";
    private const string VisiblePrefKey = "EditorVolumeControl_Visible";
    private const string SizePrefKeyW = "VolumeControl_SizeW";
    private const string SizePrefKeyH = "VolumeControl_SizeH";
    private const string MenuPath = "Tools/Playable Standard Pipeline/Editor Volume Control";

    private const float MusicClipLengthSeconds = 10f;
    private const double SourceRescanInterval = 0.5;

    private static float _musicVolume = 1f;
    private static bool _musicMuted;
    private static float _musicVolumeBeforeMute = 1f;
    private static float _sfxVolume = 1f;
    private static bool _sfxMuted;
    private static float _sfxVolumeBeforeMute = 1f;
    private static bool _visible;
    private static OverlayDragHandle _dragHandle;

    private static AudioSource[] _sources = System.Array.Empty<AudioSource>();
    private static double _lastRescanTime;

    private const float DefaultWidth = 260f;
    private const float DefaultHeight = 72f;
    private const float MinWidth = 220f;
    private const float MinHeight = 66f;
    private const float MaxWidth = 440f;
    private const float MaxHeight = 160f;
    private const float Margin = 10f;
    private const float ResizeHandleSize = 14f;

    private static float _panelWidth;
    private static float _panelHeight;
    private static bool _isResizing;
    private static Vector2 _resizeStartMouse;
    private static Vector2 _resizeStartSize;

    static VolumeControlOverlay()
    {
        _musicVolume = EditorPrefs.GetFloat(MusicVolKey, 1f);
        _musicMuted = EditorPrefs.GetBool(MusicMuteKey, false);
        _sfxVolume = EditorPrefs.GetFloat(SfxVolKey, 1f);
        _sfxMuted = EditorPrefs.GetBool(SfxMuteKey, false);
        _visible = EditorPrefs.GetBool(VisiblePrefKey, true);
        _panelWidth = EditorPrefs.GetFloat(SizePrefKeyW, DefaultWidth);
        _panelHeight = EditorPrefs.GetFloat(SizePrefKeyH, DefaultHeight);
        _musicVolumeBeforeMute = _musicVolume;
        _sfxVolumeBeforeMute = _sfxVolume;

        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.update += OnEditorUpdate;
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
        if (!EditorApplication.isPlaying) return;

        var now = EditorApplication.timeSinceStartup;
        if (now - _lastRescanTime >= SourceRescanInterval)
        {
            _lastRescanTime = now;
            _sources = UnityObject.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        ApplyToSources();
    }

    private static void ApplyToSources()
    {
        float music = _musicMuted ? 0f : _musicVolume;
        float sfx = _sfxMuted ? 0f : _sfxVolume;

        for (int i = 0; i < _sources.Length; i++)
        {
            var s = _sources[i];
            if (s == null) continue;
            bool isMusic = s.clip != null && s.clip.length >= MusicClipLengthSeconds;
            s.volume = isMusic ? music : sfx;
        }
    }

    private static Vector2 GetDefaultPosition(Rect sceneRect)
    {
        return new Vector2(Margin, sceneRect.height - _panelHeight - Margin - 20f);
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!_visible) return;

        Handles.BeginGUI();

        var sceneRect = sceneView.position;

        if (_dragHandle == null)
            _dragHandle = new OverlayDragHandle("VolumeControl", GetDefaultPosition(sceneRect));

        var panelRect = _dragHandle.GetPanelRect(sceneRect, _panelWidth, _panelHeight);

        ProcessResize(panelRect, sceneView);

        GUI.Box(panelRect, GUIContent.none, EditorStyles.helpBox);

        float padding = 6f;
        float topY = panelRect.y + 5f;

        // Drag grip (top-left)
        float gripX = panelRect.x + padding;
        var gripRect = OverlayDragHandle.GetDragButtonRect(gripX, topY);
        _dragHandle.ProcessDrag(gripRect, sceneView, true);
        OverlayDragHandle.DrawDragButton(gripX, topY);

        // Title
        var titleRect = new Rect(gripX + OverlayDragHandle.DragButtonSize + 4f, topY, 90f, OverlayDragHandle.DragButtonSize);
        GUI.Label(titleRect, EditorApplication.isPlaying ? "Volume" : "Volume (Play)");

        // Close button (top-right)
        if (OverlayDragHandle.DrawCloseButton(panelRect.xMax - padding - OverlayDragHandle.CloseButtonSize, topY))
        {
            _visible = false;
            EditorPrefs.SetBool(VisiblePrefKey, false);
            SceneView.RepaintAll();
            Handles.EndGUI();
            return;
        }

        float rowsTop = topY + OverlayDragHandle.DragButtonSize + 4f;
        float rowH = 18f;
        float gap = 3f;

        DrawChannelRow(panelRect, rowsTop, "Music", padding,
            ref _musicVolume, ref _musicMuted, ref _musicVolumeBeforeMute, MusicVolKey, MusicMuteKey);
        DrawChannelRow(panelRect, rowsTop + rowH + gap, "SFX", padding,
            ref _sfxVolume, ref _sfxMuted, ref _sfxVolumeBeforeMute, SfxVolKey, SfxMuteKey);

        DrawResizeGrip(panelRect);

        Handles.EndGUI();
    }

    private static void DrawChannelRow(Rect panelRect, float rowY, string label, float padding,
        ref float volume, ref bool muted, ref float volumeBeforeMute, string volKey, string muteKey)
    {
        float rowH = 18f;
        float labelW = 42f;
        float muteW = 40f;
        float pctW = 34f;
        float sliderH = 14f;

        float x = panelRect.x + padding;

        var labelRect = new Rect(x, rowY, labelW, rowH);
        GUI.Label(labelRect, label);
        x += labelW;

        var muteRect = new Rect(x, rowY, muteW, rowH);
        if (GUI.Button(muteRect, muted ? "Mute" : "\u266B"))
        {
            muted = !muted;
            if (muted)
                volumeBeforeMute = volume;
            else
                volume = volumeBeforeMute;
            EditorPrefs.SetBool(muteKey, muted);
        }
        x += muteW + 4f;

        float pctX = panelRect.xMax - padding - pctW;
        float sliderW = pctX - 4f - x;
        if (sliderW < 20f) sliderW = 20f;

        var sliderRect = new Rect(x, rowY + (rowH - sliderH) * 0.5f, sliderW, sliderH);
        EditorGUI.BeginDisabledGroup(muted);
        var newVolume = GUI.HorizontalSlider(sliderRect, volume, 0f, 1f);
        if (!Mathf.Approximately(newVolume, volume))
        {
            volume = newVolume;
            volumeBeforeMute = volume;
            EditorPrefs.SetFloat(volKey, volume);
        }
        EditorGUI.EndDisabledGroup();

        var percent = muted ? 0 : Mathf.RoundToInt(volume * 100);
        var pctRect = new Rect(pctX, rowY, pctW, rowH);
        GUI.Label(pctRect, percent + "%");
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
