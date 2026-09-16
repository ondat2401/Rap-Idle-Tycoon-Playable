using System.Collections.Generic;
using System.Linq;
using Spine;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

namespace _Playable.Editor
{
    /// <summary>
    /// Cua so tra cuu skin cho skeleton nhan vat nam. Liet ke tat ca skin trong SkeletonDataAsset,
    /// nhom theo tien to (mic_ / outfit_ / head_ / jewel_ / emo_ / khac) de biet id nao la skin nao,
    /// cho chon mot to hop va copy chuoi de dan vao PlayableConfig, va preview truc tiep len skeleton.
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

        private SkeletonDataAsset _dataAsset;
        private SkeletonData _skeletonData;
        private Vector2 _scroll;

        // Danh sach skin theo tung nhom.
        private readonly List<string> _mics = new List<string>();
        private readonly List<string> _outfits = new List<string>();
        private readonly List<string> _heads = new List<string>();
        private readonly List<string> _jewels = new List<string>();
        private readonly List<string> _emotions = new List<string>();
        private readonly List<string> _others = new List<string>();
        private readonly List<string> _animations = new List<string>();

        // Lua chon hien tai cho tung nhom (index trong list tuong ung, -1 = khong chon).
        private int _selMic = -1;
        private int _selOutfit = -1;
        private int _selHead = -1;
        private int _selJewel = -1;
        private int _selEmotion = -1;

        [MenuItem("Tools/Playable/Skin Browser")]
        public static void Open()
        {
            var window = GetWindow<PlayableSkinBrowser>("Skin Browser");
            window.minSize = new Vector2(360f, 480f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Skin Browser - tra cuu id skin cua skeleton", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Keo SkeletonDataAsset (vd character1_SkeletonData) vao day de xem cac skin id. " +
                "Chon mot to hop roi Copy chuoi de dan vao PlayableConfig, hoac Preview len nhan vat trong scene.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            _dataAsset = (SkeletonDataAsset)EditorGUILayout.ObjectField(
                "Skeleton Data", _dataAsset, typeof(SkeletonDataAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                LoadSkins();
            }

            if (_dataAsset == null)
            {
                EditorGUILayout.HelpBox("Chua chon SkeletonDataAsset.", MessageType.Warning);
                DrawSceneShortcut();
                return;
            }

            if (_skeletonData == null)
            {
                if (GUILayout.Button("Load / Reload skins"))
                {
                    LoadSkins();
                }

                EditorGUILayout.HelpBox("Khong doc duoc SkeletonData. Bam Load lai.", MessageType.Error);
                return;
            }

            if (GUILayout.Button("Reload"))
            {
                LoadSkins();
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

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

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Animations ({_animations.Count})", EditorStyles.boldLabel);
            foreach (string a in _animations)
            {
                EditorGUILayout.LabelField("  " + a);
            }

            EditorGUILayout.EndScrollView();

            DrawSelectionBar();
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
                }
                else if (!newSel && isSel)
                {
                    selected = -1;
                }

                if (GUILayout.Button("Copy", GUILayout.Width(50f)))
                {
                    EditorGUIUtility.systemCopyBuffer = items[i];
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawSelectionBar()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("To hop dang chon", EditorStyles.boldLabel);

            string mic = Get(_mics, _selMic);
            string outfit = Get(_outfits, _selOutfit);
            string head = Get(_heads, _selHead);
            string jewel = Get(_jewels, _selJewel);
            string emo = Get(_emotions, _selEmotion);

            EditorGUILayout.LabelField($"Mic: {Show(mic)}   Outfit: {Show(outfit)}");
            EditorGUILayout.LabelField($"Head: {Show(head)}   Jewel: {Show(jewel)}   Emo: {Show(emo)}");

            // Chuoi PlayableSkinSet de dan nhanh vao code hoac ghi chu.
            string combined = $"new PlayableSkinSet(\"{mic}\", \"{outfit}\", \"{head}\", \"{jewel}\")";

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Copy PlayableSkinSet"))
            {
                EditorGUIUtility.systemCopyBuffer = combined;
                Debug.Log($"[SkinBrowser] Copied: {combined}");
            }

            if (GUILayout.Button("Preview len nhan vat (scene)"))
            {
                PreviewOnScene(mic, outfit, head, jewel, emo);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.SelectableLabel(combined, EditorStyles.textField,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
        }

        private void DrawSceneShortcut()
        {
            var view = FindManViewSkeletonData();
            if (view != null)
            {
                EditorGUILayout.HelpBox(
                    "Tim thay skeleton nhan vat nam trong scene. Bam de nap nhanh.", MessageType.None);
                if (GUILayout.Button("Nap skeleton nam tu scene"))
                {
                    _dataAsset = view;
                    LoadSkins();
                }
            }
        }

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

                if (n.StartsWith(MicPrefix))
                {
                    _mics.Add(n);
                }
                else if (n.StartsWith(OutfitPrefix))
                {
                    _outfits.Add(n);
                }
                else if (n.StartsWith(HeadPrefix))
                {
                    _heads.Add(n);
                }
                else if (n.StartsWith(JewelPrefix))
                {
                    _jewels.Add(n);
                }
                else if (n.StartsWith(EmotionPrefix))
                {
                    _emotions.Add(n);
                }
                else
                {
                    _others.Add(n);
                }
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
        }

        /// <summary>Ap to hop skin len skeleton nam dang co trong scene de xem truc tiep.</summary>
        private void PreviewOnScene(string mic, string outfit, string head, string jewel, string emo)
        {
            SkeletonRenderer renderer = FindSceneManRenderer();
            if (renderer == null)
            {
                EditorUtility.DisplayDialog("Skin Browser",
                    "Khong tim thay SkeletonRenderer nhan vat nam trong scene dang mo. " +
                    "Hay mo scene co nhan vat va thu lai.", "OK");
                return;
            }

            renderer.Initialize(false);
            Skeleton skeleton = renderer.Skeleton;
            if (skeleton == null)
            {
                return;
            }

            SkeletonData data = skeleton.Data;
            var combined = new Skin(PreviewSkinName);
            AddPart(combined, data, mic);
            AddPart(combined, data, outfit);
            AddPart(combined, data, head);
            AddPart(combined, data, jewel);
            AddPart(combined, data, emo);

            skeleton.SetSkin(combined);
            skeleton.SetSlotsToSetupPose();
            renderer.LateUpdate();
            SceneView.RepaintAll();
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
            else
            {
                Debug.LogWarning($"[SkinBrowser] Khong tim thay skin '{skinName}'.");
            }
        }

        /// <summary>Tim SkeletonRenderer cua nhan vat nam trong scene (uu tien node ten 'man').</summary>
        private static SkeletonRenderer FindSceneManRenderer()
        {
#if UNITY_2023_1_OR_NEWER
            SkeletonRenderer[] all = Object.FindObjectsByType<SkeletonRenderer>(FindObjectsSortMode.None);
#else
            SkeletonRenderer[] all = Object.FindObjectsOfType<SkeletonRenderer>();
#endif
            if (all == null || all.Length == 0)
            {
                return null;
            }

            // Uu tien node co ten cha chua "man".
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

        /// <summary>Lay SkeletonDataAsset cua nhan vat nam trong scene neu co.</summary>
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
