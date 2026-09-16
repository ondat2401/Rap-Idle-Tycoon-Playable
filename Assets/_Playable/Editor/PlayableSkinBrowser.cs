using System;
using System.Collections.Generic;
using System.Text;
using _Playable.Runtime.Config;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

namespace _Playable.Editor
{
    /// <summary>
    /// Cua so tra cuu + preview skin cho skeleton nhan vat nam.
    ///
    /// - Liet ke skin theo nhom (mic_/outfit_/head_/jewel_/emo_) va animations.
    /// - Preview to hop skin NGAY TRONG WINDOW (khong can scene) bang PreviewRenderUtility.
    /// - Xuat/nhap JSON cac bo skin; nap thang vao PlayableConfig SO (StartSkin + ClothesSkins)
    ///   de khoi phai go tung id.
    ///
    /// Mo qua menu: Tools > Playable > Skin Browser.
    /// </summary>
    public sealed class PlayableSkinBrowser : EditorWindow
    {
        private const string MicPrefix = "mic_";
        private const string OutfitPrefix = "outfit_";
        private const string HeadPrefix = "head_";
        private const string JewelPrefix = "jewel_";
        private const string EmotionPrefix = "emo_";
        private const string PreviewSkinName = "playable_skin_browser_preview";

        [Serializable]
        private struct SkinSetDto
        {
            public string Mic;
            public string Outfit;
            public string Head;
            public string Jewel;
        }

        [Serializable]
        private class SkinConfigDto
        {
            public SkinSetDto start;
            public SkinSetDto[] clothes;
        }

        private SkeletonDataAsset _dataAsset;
        private SkeletonData _skeletonData;
        private PlayableConfig _config;
        private Vector2 _scroll;

        private readonly List<string> _mics = new List<string>();
        private readonly List<string> _outfits = new List<string>();
        private readonly List<string> _heads = new List<string>();
        private readonly List<string> _jewels = new List<string>();
        private readonly List<string> _emotions = new List<string>();
        private readonly List<string> _others = new List<string>();
        private readonly List<string> _animations = new List<string>();

        private int _selMic = -1;
        private int _selOutfit = -1;
        private int _selHead = -1;
        private int _selJewel = -1;
        private int _selEmotion = -1;

        private string _jsonBuffer = string.Empty;

        // Preview
        private PreviewRenderUtility _preview;
        private GameObject _previewGo;
        private SkeletonAnimation _previewAnim;
        private float _previewZoom = 1f;
        private Vector2 _previewPan = Vector2.zero;
        private string _previewAnimName = string.Empty;
        private double _lastTime;

        [MenuItem("Tools/Playable/Skin Browser")]
        public static void Open()
        {
            var window = GetWindow<PlayableSkinBrowser>("Skin Browser");
            window.minSize = new Vector2(760f, 560f);
            window.Show();
        }

        private void OnDisable()
        {
            DestroyPreview();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            // Cot trai: danh sach skin + config.
            EditorGUILayout.BeginVertical(GUILayout.Width(380f));
            DrawLeftColumn();
            EditorGUILayout.EndVertical();

            // Cot phai: preview.
            EditorGUILayout.BeginVertical();
            DrawPreviewColumn();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        // ------------------------------------------------------------------ cot trai

        private void DrawLeftColumn()
        {
            EditorGUILayout.LabelField("Skin Browser", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _dataAsset = (SkeletonDataAsset)EditorGUILayout.ObjectField(
                "Skeleton Data", _dataAsset, typeof(SkeletonDataAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                LoadSkins();
                RebuildPreview();
            }

            _config = (PlayableConfig)EditorGUILayout.ObjectField(
                "Playable Config", _config, typeof(PlayableConfig), false);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Nap skeleton nam tu scene"))
            {
                SkeletonDataAsset fromScene = FindManViewSkeletonData();
                if (fromScene != null)
                {
                    _dataAsset = fromScene;
                    LoadSkins();
                    RebuildPreview();
                }
            }

            if (_dataAsset != null && GUILayout.Button("Reload"))
            {
                LoadSkins();
                RebuildPreview();
            }

            EditorGUILayout.EndHorizontal();

            if (_dataAsset == null || _skeletonData == null)
            {
                EditorGUILayout.HelpBox("Keo SkeletonDataAsset vao de bat dau.", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));

            DrawGroup("Mic", _mics, ref _selMic);
            DrawGroup("Outfit", _outfits, ref _selOutfit);
            DrawGroup("Head", _heads, ref _selHead);
            DrawGroup("Jewel", _jewels, ref _selJewel);
            DrawGroup("Emotion", _emotions, ref _selEmotion);

            if (_others.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"Skin khac ({_others.Count})", EditorStyles.boldLabel);
                foreach (string s in _others)
                {
                    EditorGUILayout.LabelField("  " + s);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawGroup(string title, List<string> items, ref int selected)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"{title} ({items.Count})", EditorStyles.boldLabel);

            if (items.Count == 0)
            {
                EditorGUILayout.LabelField("  (khong co)", EditorStyles.miniLabel);
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                bool isSel = selected == i;
                bool newSel = GUILayout.Toggle(isSel, items[i], EditorStyles.radioButton);
                if (newSel && !isSel)
                {
                    selected = i;
                    ApplyPreviewSkin();
                }
                else if (!newSel && isSel)
                {
                    selected = -1;
                    ApplyPreviewSkin();
                }

                if (GUILayout.Button("Copy", GUILayout.Width(48f)))
                {
                    EditorGUIUtility.systemCopyBuffer = items[i];
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        // ------------------------------------------------------------------ cot phai (preview + json)

        private void DrawPreviewColumn()
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            string mic = Get(_mics, _selMic);
            string outfit = Get(_outfits, _selOutfit);
            string head = Get(_heads, _selHead);
            string jewel = Get(_jewels, _selJewel);
            string emo = Get(_emotions, _selEmotion);

            // Vung ve preview.
            Rect previewRect = GUILayoutUtility.GetRect(300f, 360f, GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(false));
            DrawPreview(previewRect);

            // Chon animation de xem.
            if (_animations.Count > 0 && _previewAnim != null)
            {
                int current = Mathf.Max(0, _animations.IndexOf(_previewAnimName));
                int next = EditorGUILayout.Popup("Animation", current, _animations.ToArray());
                if (next != current || string.IsNullOrEmpty(_previewAnimName))
                {
                    _previewAnimName = _animations[next];
                    PlayPreviewAnimation(_previewAnimName);
                }
            }

            _previewZoom = EditorGUILayout.Slider("Zoom", _previewZoom, 0.2f, 3f);

            EditorGUILayout.LabelField("To hop dang chon", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Mic: {Show(mic)}   Outfit: {Show(outfit)}");
            EditorGUILayout.LabelField($"Head: {Show(head)}   Jewel: {Show(jewel)}   Emo: {Show(emo)}");

            string skinSet = $"new PlayableSkinSet(\"{mic}\", \"{outfit}\", \"{head}\", \"{jewel}\")";
            if (GUILayout.Button("Copy PlayableSkinSet code"))
            {
                EditorGUIUtility.systemCopyBuffer = skinSet;
            }

            EditorGUILayout.Space();
            DrawJsonSection(mic, outfit, head, jewel);
        }

        private void DrawJsonSection(string mic, string outfit, string head, string jewel)
        {
            EditorGUILayout.LabelField("JSON config (start + clothes)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Export: xuat StartSkin + ClothesSkins hien tai cua Playable Config ra JSON. " +
                "Import: dan JSON roi bam Import de ghi vao Playable Config (khong can go tung id).",
                MessageType.None);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export tu Config"))
            {
                ExportConfigToJson();
            }

            using (new EditorGUI.DisabledScope(_config == null || string.IsNullOrWhiteSpace(_jsonBuffer)))
            {
                if (GUILayout.Button("Import vao Config"))
                {
                    ImportJsonToConfig();
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Them to hop dang chon vao JSON (clothes)"))
            {
                AppendSelectedToJson(mic, outfit, head, jewel);
            }

            if (GUILayout.Button("Set lam Start trong JSON"))
            {
                SetSelectedAsStartJson(mic, outfit, head, jewel);
            }

            EditorGUILayout.EndHorizontal();

            _jsonBuffer = EditorGUILayout.TextArea(_jsonBuffer, GUILayout.MinHeight(120f));
        }

        // ------------------------------------------------------------------ preview render

        private void DrawPreview(Rect rect)
        {
            if (_dataAsset == null)
            {
                EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
                return;
            }

            if (_preview == null)
            {
                RebuildPreview();
            }

            if (_preview == null || _previewGo == null)
            {
                EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
                EditorGUI.LabelField(rect, "Khong tao duoc preview.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            HandlePreviewInput(rect);

            // Cap nhat animation theo thoi gian editor.
            double now = EditorApplication.timeSinceStartup;
            float delta = (float)(now - _lastTime);
            _lastTime = now;
            if (_previewAnim != null && delta > 0f && delta < 0.5f)
            {
                _previewAnim.Update(delta);
                _previewAnim.LateUpdate();
            }

            _preview.BeginPreview(rect, GUIStyle.none);

            Camera cam = _preview.camera;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.2f, 0.2f, 0.22f);
            cam.orthographic = true;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 100f;

            // Khung nhin: skeleton cao ~ vai don vi; dat camera nhin vao giua.
            float orthoSize = 6f / Mathf.Max(0.01f, _previewZoom);
            cam.orthographicSize = orthoSize;
            cam.transform.position = new Vector3(_previewPan.x, 3f + _previewPan.y, -10f);
            cam.transform.rotation = Quaternion.identity;

            _preview.Render(true);
            Texture tex = _preview.EndPreview();
            GUI.DrawTexture(rect, tex, ScaleMode.StretchToFill, false);

            Repaint();
        }

        private void HandlePreviewInput(Rect rect)
        {
            UnityEngine.Event e = UnityEngine.Event.current;
            if (!rect.Contains(e.mousePosition))
            {
                return;
            }

            if (e.type == EventType.ScrollWheel)
            {
                _previewZoom = Mathf.Clamp(_previewZoom - e.delta.y * 0.03f, 0.2f, 3f);
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && e.button == 0)
            {
                _previewPan += new Vector2(-e.delta.x, e.delta.y) * 0.02f / Mathf.Max(0.01f, _previewZoom);
                e.Use();
            }
        }

        private void RebuildPreview()
        {
            DestroyPreview();

            if (_dataAsset == null || _skeletonData == null)
            {
                return;
            }

            _preview = new PreviewRenderUtility();

            _previewAnim = SkeletonAnimation.NewSkeletonAnimationGameObject(_dataAsset, true);
            if (_previewAnim == null)
            {
                return;
            }

            _previewGo = _previewAnim.gameObject;
            _previewGo.hideFlags = HideFlags.HideAndDontSave;
            _previewGo.transform.position = Vector3.zero;

            _preview.AddSingleGO(_previewGo);

            // Animation mac dinh.
            if (_animations.Count > 0)
            {
                _previewAnimName = _animations.Contains("no_rap_idle") ? "no_rap_idle"
                    : (_animations.Contains("idle") ? "idle" : _animations[0]);
                PlayPreviewAnimation(_previewAnimName);
            }

            ApplyPreviewSkin();
            _lastTime = EditorApplication.timeSinceStartup;
        }

        private void PlayPreviewAnimation(string animName)
        {
            if (_previewAnim == null || string.IsNullOrEmpty(animName))
            {
                return;
            }

            _previewAnim.Initialize(false);
            if (_previewAnim.AnimationState != null && _previewAnim.Skeleton.Data.FindAnimation(animName) != null)
            {
                _previewAnim.AnimationState.SetAnimation(0, animName, true);
            }
        }

        private void ApplyPreviewSkin()
        {
            if (_previewAnim == null)
            {
                return;
            }

            _previewAnim.Initialize(false);
            Skeleton skeleton = _previewAnim.Skeleton;
            if (skeleton == null)
            {
                return;
            }

            SkeletonData data = skeleton.Data;
            var combined = new Skin(PreviewSkinName);
            AddPart(combined, data, Get(_mics, _selMic));
            AddPart(combined, data, Get(_outfits, _selOutfit));
            AddPart(combined, data, Get(_heads, _selHead));
            AddPart(combined, data, Get(_jewels, _selJewel));
            AddPart(combined, data, Get(_emotions, _selEmotion));

            skeleton.SetSkin(combined);
            skeleton.SetSlotsToSetupPose();
            Repaint();
        }

        private void DestroyPreview()
        {
            if (_previewGo != null)
            {
                DestroyImmediate(_previewGo);
                _previewGo = null;
                _previewAnim = null;
            }

            if (_preview != null)
            {
                _preview.Cleanup();
                _preview = null;
            }
        }

        // ------------------------------------------------------------------ json <-> config

        private void ExportConfigToJson()
        {
            if (_config == null)
            {
                EditorUtility.DisplayDialog("Skin Browser", "Chua gan Playable Config.", "OK");
                return;
            }

            var so = new SerializedObject(_config);
            var dto = new SkinConfigDto
            {
                start = ReadSkinSet(so.FindProperty("_startSkin")),
                clothes = ReadSkinSetArray(so.FindProperty("_clothesSkins"))
            };

            _jsonBuffer = JsonUtility.ToJson(dto, true);
        }

        private void ImportJsonToConfig()
        {
            if (_config == null)
            {
                return;
            }

            SkinConfigDto dto;
            try
            {
                dto = JsonUtility.FromJson<SkinConfigDto>(_jsonBuffer);
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Skin Browser", "JSON khong hop le:\n" + ex.Message, "OK");
                return;
            }

            if (dto == null)
            {
                EditorUtility.DisplayDialog("Skin Browser", "JSON rong hoac sai dinh dang.", "OK");
                return;
            }

            var so = new SerializedObject(_config);
            WriteSkinSet(so.FindProperty("_startSkin"), dto.start);

            SerializedProperty clothes = so.FindProperty("_clothesSkins");
            int count = dto.clothes != null ? dto.clothes.Length : 0;
            clothes.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                WriteSkinSet(clothes.GetArrayElementAtIndex(i), dto.clothes[i]);
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SkinBrowser] Da ghi {count} clothes skin + start vao {_config.name}.");
        }

        private void AppendSelectedToJson(string mic, string outfit, string head, string jewel)
        {
            SkinConfigDto dto = ParseBufferOrConfig();
            var list = new List<SkinSetDto>(dto.clothes ?? Array.Empty<SkinSetDto>());
            list.Add(new SkinSetDto { Mic = mic, Outfit = outfit, Head = head, Jewel = jewel });
            dto.clothes = list.ToArray();
            _jsonBuffer = JsonUtility.ToJson(dto, true);
        }

        private void SetSelectedAsStartJson(string mic, string outfit, string head, string jewel)
        {
            SkinConfigDto dto = ParseBufferOrConfig();
            dto.start = new SkinSetDto { Mic = mic, Outfit = outfit, Head = head, Jewel = jewel };
            _jsonBuffer = JsonUtility.ToJson(dto, true);
        }

        private SkinConfigDto ParseBufferOrConfig()
        {
            if (!string.IsNullOrWhiteSpace(_jsonBuffer))
            {
                try
                {
                    SkinConfigDto parsed = JsonUtility.FromJson<SkinConfigDto>(_jsonBuffer);
                    if (parsed != null)
                    {
                        return parsed;
                    }
                }
                catch
                {
                    // bo qua, tao moi
                }
            }

            return new SkinConfigDto { clothes = Array.Empty<SkinSetDto>() };
        }

        private static SkinSetDto ReadSkinSet(SerializedProperty prop)
        {
            if (prop == null)
            {
                return default;
            }

            return new SkinSetDto
            {
                Mic = prop.FindPropertyRelative("Mic").stringValue,
                Outfit = prop.FindPropertyRelative("Outfit").stringValue,
                Head = prop.FindPropertyRelative("Head").stringValue,
                Jewel = prop.FindPropertyRelative("Jewel").stringValue
            };
        }

        private static SkinSetDto[] ReadSkinSetArray(SerializedProperty arrayProp)
        {
            if (arrayProp == null || !arrayProp.isArray)
            {
                return Array.Empty<SkinSetDto>();
            }

            var result = new SkinSetDto[arrayProp.arraySize];
            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                result[i] = ReadSkinSet(arrayProp.GetArrayElementAtIndex(i));
            }

            return result;
        }

        private static void WriteSkinSet(SerializedProperty prop, SkinSetDto dto)
        {
            if (prop == null)
            {
                return;
            }

            prop.FindPropertyRelative("Mic").stringValue = dto.Mic ?? string.Empty;
            prop.FindPropertyRelative("Outfit").stringValue = dto.Outfit ?? string.Empty;
            prop.FindPropertyRelative("Head").stringValue = dto.Head ?? string.Empty;
            prop.FindPropertyRelative("Jewel").stringValue = dto.Jewel ?? string.Empty;
        }

        // ------------------------------------------------------------------ load skins

        private void LoadSkins()
        {
            _mics.Clear();
            _outfits.Clear();
            _heads.Clear();
            _jewels.Clear();
            _emotions.Clear();
            _others.Clear();
            _animations.Clear();
            _selMic = _selOutfit = _selHead = _selJewel = _selEmotion = -1;
            _skeletonData = null;

            if (_dataAsset == null)
            {
                return;
            }

            _skeletonData = _dataAsset.GetSkeletonData(true);
            if (_skeletonData == null)
            {
                return;
            }

            foreach (Skin skin in _skeletonData.Skins)
            {
                string n = skin.Name;
                if (n == "default")
                {
                    continue;
                }

                if (n.StartsWith(MicPrefix)) _mics.Add(n);
                else if (n.StartsWith(OutfitPrefix)) _outfits.Add(n);
                else if (n.StartsWith(HeadPrefix)) _heads.Add(n);
                else if (n.StartsWith(JewelPrefix)) _jewels.Add(n);
                else if (n.StartsWith(EmotionPrefix)) _emotions.Add(n);
                else _others.Add(n);
            }

            foreach (Spine.Animation anim in _skeletonData.Animations)
            {
                _animations.Add(anim.Name);
            }

            _mics.Sort();
            _outfits.Sort();
            _heads.Sort();
            _jewels.Sort();
            _emotions.Sort();
            _others.Sort();
            _animations.Sort();
        }

        private static void AddPart(Skin target, SkeletonData data, string skinName)
        {
            if (string.IsNullOrEmpty(skinName))
            {
                return;
            }

            Skin skin = data.FindSkin(skinName);
            if (skin != null)
            {
                target.AddSkin(skin);
            }
        }

        private static SkeletonRenderer FindSceneManRenderer()
        {
#if UNITY_2023_1_OR_NEWER
            SkeletonRenderer[] all = UnityengineFindAll();
#else
            SkeletonRenderer[] all = UnityEngine.Object.FindObjectsOfType<SkeletonRenderer>();
#endif
            if (all == null || all.Length == 0)
            {
                return null;
            }

            foreach (SkeletonRenderer r in all)
            {
                Transform parent = r.transform.parent;
                if (parent != null && parent.name.ToLowerInvariant().Contains("man"))
                {
                    return r;
                }
            }

            return all[0];
        }

#if UNITY_2023_1_OR_NEWER
        private static SkeletonRenderer[] UnityengineFindAll()
        {
            return UnityEngine.Object.FindObjectsByType<SkeletonRenderer>(FindObjectsSortMode.None);
        }
#endif

        private static SkeletonDataAsset FindManViewSkeletonData()
        {
            SkeletonRenderer r = FindSceneManRenderer();
            return r != null ? r.skeletonDataAsset : null;
        }

        private static string Get(List<string> list, int index)
        {
            return index >= 0 && index < list.Count ? list[index] : string.Empty;
        }

        private static string Show(string value)
        {
            return string.IsNullOrEmpty(value) ? "(chua chon)" : value;
        }
    }
}
