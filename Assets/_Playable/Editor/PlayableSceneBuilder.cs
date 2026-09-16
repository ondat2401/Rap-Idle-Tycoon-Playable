using System.Collections.Generic;
using System.IO;
using _Playable.Runtime.Config;
using _Playable.Runtime.Core;
using _Playable.Runtime.Flow;
using _Playable.Runtime.View;
using Spine.Unity;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static _Playable.Runtime.Core.PlayableSfx;
using Object = UnityEngine.Object;
using Util = _Playable.Editor.PlayableBuildUtility;

namespace _Playable.Editor
{
    /// <summary>
    /// Dung toan bo prefab va scene demo cua playable bang code.
    ///
    /// Vi sao dung code chu khong dung tay: sinh lai duoc, review duoc bang diff, va khong phai
    /// go 16 prefab bang chuot. Chay lai bat ky luc nao se ghi de toan bo - moi tinh chinh tay
    /// tren prefab se mat, nen sau khi ban giao thi chinh truc tiep tren prefab va dung chay lai.
    /// </summary>
    public static class PlayableSceneBuilder
    {
        private const string Root = "Assets/_Playable";
        private const string PrefabDir = Root + "/Prefabs";
        private const string ConfigPath = Root + "/Config/PlayableConfig.asset";
        private const string ScenePath = Root + "/Scenes/PlayableDemo.unity";
        private const string FontPath = Root + "/Art/Font/Lalezar-Regular SDF.asset";

        private const string BubblePrefabPath = PrefabDir + "/PB_Bubble.prefab";
        private const string ChoiceButtonPrefabPath = PrefabDir + "/PB_ChoiceButton.prefab";
        private const string RootPrefabPath = PrefabDir + "/PB_PlayableRoot.prefab";
        private const string MainMapPrefabPath = PrefabDir + "/PlayableMainMap.prefab";
        private const string EmojiPrefabDir = PrefabDir + "/Emoji";

        private static readonly Vector2 ReferenceResolution = new Vector2(1080f, 1920f);

        // Sprite theo level cua tung slot map. Index 0 la trang thai khoi dau, 1..3 la ba lua chon
        // mua duoc. Nut mua doc cung mang nay nen icon tren nut khong bao gio lech voi vat the tren map.
        private static readonly string[] BackgroundLevels =
            { "m1_background", "m2_background", "m3_background", "m4_background" };

        private static readonly string[] HouseLevels = { "house_1", "house_2", "house_3", "house_4" };
        private static readonly string[] CarLevels = { "car_1", "car_2", "car_3", "car_4" };

        private static readonly Color TextDark = new Color(0.13f, 0.11f, 0.09f);
        private static readonly Color TextLight = Color.white;

        private static TMP_FontAsset s_font;

        [MenuItem("Tools/Playable/Rebuild Scene And Prefabs")]
        public static void BuildAll()
        {
            Util.ClearCache();
            s_font = Util.Load<TMP_FontAsset>(FontPath);

            EnsureFolders();
            PlayableConfig config = EnsureConfig();

            GameObject mapPrefab = BuildMainMapPrefab();
            GameObject bubblePrefab = BuildBubblePrefab();
            GameObject buttonPrefab = BuildChoiceButtonPrefab();
            List<GameObject> emojiPrefabs = BuildEmojiPrefabs();

            BuildSceneAndRoot(config, mapPrefab, bubblePrefab, buttonPrefab, emojiPrefabs);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ValidateWiring();
            Debug.Log("[PlayableBuilder] Xong: prefab + scene da duoc sinh lai.");
        }

        /// <summary>
        /// Quet prefab goc, canh bao moi field tham chieu cua component _Playable dang de trong.
        /// Bat loi go thieu ngay o buoc sinh thay vi doi den luc chay.
        /// </summary>
        [MenuItem("Tools/Playable/Validate Wiring")]
        public static void ValidateWiring()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RootPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[PlayableBuilder] Khong tim thay {RootPrefabPath}");
                return;
            }

            // Nhung field co y de trong: PlayableAudio tu tao AudioSource luc chay.
            var optional = new HashSet<string>
            {
                "PlayableAudio._sfxSource",
                "PlayableAudio._beatSource",
                "PlayableAudio._vocalSource",
                "PlayableChoiceButton._icon"
            };

            int missing = 0;
            foreach (MonoBehaviour behaviour in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null)
                {
                    continue;
                }

                System.Type type = behaviour.GetType();
                if (type.Namespace == null || !type.Namespace.StartsWith("_Playable.Runtime"))
                {
                    continue;
                }

                var serialized = new SerializedObject(behaviour);
                SerializedProperty property = serialized.GetIterator();
                bool enterChildren = true;

                while (property.NextVisible(enterChildren))
                {
                    enterChildren = false;

                    if (property.propertyType != SerializedPropertyType.ObjectReference ||
                        property.objectReferenceValue != null ||
                        property.name == "m_Script")
                    {
                        continue;
                    }

                    string key = $"{type.Name}.{property.name}";
                    if (optional.Contains(key))
                    {
                        continue;
                    }

                    missing++;
                    Debug.LogWarning($"[PlayableBuilder] Chua gan: {key} tren '{behaviour.name}'");
                }
            }

            if (missing == 0)
            {
                Debug.Log("[PlayableBuilder] Wiring OK - khong co tham chieu nao bi bo trong.");
            }
        }

        // ------------------------------------------------------------------ chuan bi

        private static void EnsureFolders()
        {
            foreach (string folder in new[]
                     {
                         Root + "/Prefabs", Root + "/Prefabs/Emoji", Root + "/Config", Root + "/Scenes"
                     })
            {
                if (AssetDatabase.IsValidFolder(folder))
                {
                    continue;
                }

                string parent = Path.GetDirectoryName(folder)!.Replace('\\', '/');
                AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
            }
        }

        private static PlayableConfig EnsureConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<PlayableConfig>(ConfigPath);
            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<PlayableConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
            return config;
        }

        // ------------------------------------------------------------------ prefab con

        /// <summary>
        /// Dung prefab map rieng, theo dung bo cuc cua
        /// Assets/_Project/Addressables/Prefabs/StoryScene/MainMap.prefab: mot root gom day du vat the
        /// cua map, moi vat the mot node con dat theo ten slot.
        /// Thu tu con quyet dinh thu tu ve - node dau danh sach nam duoi cung.
        /// </summary>
        private static GameObject BuildMainMapPrefab()
        {
            // World space nhu MainMap goc: Transform thuong + SpriteRenderer, Main Camera nhin.
            var go = new GameObject("PlayableMainMap");
            Transform root = go.transform;

            var map = go.AddComponent<PlayableMainMap>();

            // Vi tri va sorting order chep dung tu MainMap.prefab (don vi world, PPU 100).
            // Thu tu con giu nguyen nhu ban goc; thu tu ve do sorting order quyet dinh.
            SpriteRenderer media = MapSlotRenderer(root, "media", "media_1", new Vector3(2.82f, -6.35f), 0);
            SpriteRenderer floor = MapSlotRenderer(root, "floor", "floor_1", new Vector3(0f, -10.8f), -2);
            SpriteRenderer speaker =
                MapSlotRenderer(root, "speaker", "speaker_1", new Vector3(2.71f, -8.61f), 0);
            SpriteRenderer background =
                MapSlotRenderer(root, "background", BackgroundLevels[0], new Vector3(0f, 4.96f), -1);
            SpriteRenderer backyard =
                MapSlotRenderer(root, "backyard", "backyard_1", new Vector3(-3.98f, 3.18f), 0);
            SpriteRenderer statue = MapSlotRenderer(root, "statue", "statue_1", new Vector3(2.54f, -0.91f), 0);
            SpriteRenderer car = MapSlotRenderer(root, "car", CarLevels[0], new Vector3(1.3f, 1.14f), 0);
            SpriteRenderer house = MapSlotRenderer(root, "house", HouseLevels[0], new Vector3(0.337f, 4.427f), 0);

            var bindings = new List<(PlayableMapSlot Slot, SpriteRenderer Renderer, string[] Levels)>
            {
                (PlayableMapSlot.Background, background, BackgroundLevels),
                (PlayableMapSlot.Backyard, backyard, new[] { "backyard_1" }),
                (PlayableMapSlot.House, house, HouseLevels),
                (PlayableMapSlot.Floor, floor, new[] { "floor_1" }),
                (PlayableMapSlot.Media, media, new[] { "media_1" }),
                (PlayableMapSlot.Speaker, speaker, new[] { "speaker_1" }),
                (PlayableMapSlot.Statue, statue, new[] { "statue_1" }),
                (PlayableMapSlot.Car, car, CarLevels)
            };

            var serialized = new SerializedObject(map);
            SerializedProperty slots = serialized.FindProperty("_slots");
            slots.arraySize = bindings.Count;

            for (int i = 0; i < bindings.Count; i++)
            {
                (PlayableMapSlot slot, SpriteRenderer renderer, string[] levels) = bindings[i];

                SerializedProperty element = slots.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Slot").enumValueIndex = (int)slot;
                element.FindPropertyRelative("Renderer").objectReferenceValue = renderer;

                SerializedProperty levelArray = element.FindPropertyRelative("Levels");
                levelArray.arraySize = levels.Length;
                for (int level = 0; level < levels.Length; level++)
                {
                    levelArray.GetArrayElementAtIndex(level).objectReferenceValue =
                        Util.LoadSprite("Stage", levels[level]);
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Nhan vat dung tren map trong world space, giong c_drake / lover_prefab cua game.
            PlayableManView man = BuildMan(root);
            var women = new[]
            {
                BuildWoman(root, "woman_1", "lover_1"),
                BuildWoman(root, "woman_2", "lover_2")
            };

            Util.SetRef(map, "_man", man);
            Util.SetRefArray(map, "_womanVariants", women);
            Util.ApplyAll();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, MainMapPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static SpriteRenderer MapSlotRenderer(Transform parent, string name, string spriteName,
            Vector3 position, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Util.LoadSprite("Stage", spriteName);
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static GameObject BuildBubblePrefab()
        {
            var go = new GameObject("PB_Bubble", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(460f, 200f);

            CanvasGroup group = go.AddComponent<CanvasGroup>();

            Image frame = Util.NewStretchImage("Frame", rect, Util.LoadSprite("Ui", "box_chat"));
            frame.type = Image.Type.Sliced;

            TextMeshProUGUI label = Util.NewText("Label", rect, "...", 44f, s_font, TextDark);
            Util.Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(40f, 40f);
            label.rectTransform.offsetMax = new Vector2(-40f, -40f);
            label.enableWordWrapping = true;

            var view = go.AddComponent<PlayableBubbleView>();
            Util.SetRef(view, "_group", group);
            Util.SetRef(view, "_content", rect);
            Util.SetRef(view, "_label", label);
            Util.ApplyAll();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, BubblePrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject BuildChoiceButtonPrefab()
        {
            var go = new GameObject("PB_ChoiceButton", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(300f, 340f);

            Image background = go.AddComponent<Image>();
            background.sprite = Util.LoadSprite("Ui", "button_green");
            background.type = Image.Type.Sliced;
            background.raycastTarget = true;

            Button button = go.AddComponent<Button>();
            button.targetGraphic = background;

            Image icon = Util.NewImage("Icon", rect, null, false);
            Util.Anchor(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f),
                new Vector2(220f, 180f));
            icon.preserveAspect = true;

            TextMeshProUGUI title = Util.NewText("Title", rect, string.Empty, 40f, s_font, TextLight);
            Util.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -46f),
                new Vector2(280f, 60f));

            RectTransform priceRoot = Util.NewRect("Price", rect);
            Util.Anchor(priceRoot, new Vector2(0.5f, 0f), new Vector2(0f, 54f), new Vector2(260f, 70f));

            Image coin = Util.NewImage("Coin", priceRoot, Util.LoadSprite("Ui", "0"), false);
            Util.Anchor(coin.rectTransform, new Vector2(0f, 0.5f), new Vector2(40f, 0f),
                new Vector2(56f, 56f));
            coin.preserveAspect = true;

            TextMeshProUGUI price = Util.NewText("Amount", priceRoot, "0", 40f, s_font, TextLight);
            Util.Anchor(price.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(24f, 0f),
                new Vector2(200f, 60f));

            var view = go.AddComponent<PlayableChoiceButton>();
            Util.SetRef(view, "_button", button);
            Util.SetRef(view, "_icon", icon);
            Util.SetRef(view, "_priceRoot", priceRoot.gameObject);
            Util.SetRef(view, "_priceLabel", price);
            Util.SetRef(view, "_titleLabel", title);
            Util.ApplyAll();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, ChoiceButtonPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static List<GameObject> BuildEmojiPrefabs()
        {
            var sprites = new List<(string Name, Sprite Sprite)>
            {
                ("disc_101", Util.LoadSprite("Icons", "disc_101")),
                ("disc_102", Util.LoadSprite("Icons", "disc_102")),
                ("disc_103", Util.LoadSprite("Icons", "disc_103")),
                ("disc_104", Util.LoadSprite("Icons", "disc_104")),
                ("disc_105", Util.LoadSprite("Icons", "disc_105")),
                ("heart", Util.LoadSprite("Ui", "HEART")),
                ("money", Util.LoadSprite("Fx", "fx_money"))
            };

            var prefabs = new List<GameObject>();
            foreach ((string name, Sprite sprite) in sprites)
            {
                var go = new GameObject($"PB_Emoji_{name}", typeof(RectTransform));
                var rect = (RectTransform)go.transform;
                rect.sizeDelta = new Vector2(110f, 110f);

                go.AddComponent<CanvasGroup>();

                var image = go.AddComponent<Image>();
                image.sprite = sprite;
                image.raycastTarget = false;
                image.preserveAspect = true;

                prefabs.Add(PrefabUtility.SaveAsPrefabAsset(go, $"{EmojiPrefabDir}/PB_Emoji_{name}.prefab"));
                Object.DestroyImmediate(go);
            }

            return prefabs;
        }

        // ------------------------------------------------------------------ scene

        private static void BuildSceneAndRoot(PlayableConfig config, GameObject mapPrefab,
            GameObject bubblePrefab, GameObject buttonPrefab, List<GameObject> emojiPrefabs)
        {
            // Mo additive chu khong Single: Single se dong scene dang mo va co the bat hop thoai
            // "luu thay doi?" - khong chay duoc trong quy trinh tu dong.
            Scene previousActive = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);

            CreateCamera();
            CreateEventSystem();

            var rootGo = new GameObject("PB_PlayableRoot");
            var audio = rootGo.AddComponent<PlayableAudio>();
            var exit = rootGo.AddComponent<PlayableExit>();
            var flow = rootGo.AddComponent<PlayableFlow>();

            ConfigureAudio(audio);

            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(rootGo.transform, false);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            canvasGo.AddComponent<GraphicRaycaster>();

            var canvasRect = (RectTransform)canvasGo.transform;

            // Map nam ngoai Canvas, la con truc tiep cua root o goc toa do world - giong GameManager
            // spawn MainMap. Canvas overlay ve UI va nhan vat de len tren.
            var mapInstance = (GameObject)PrefabUtility.InstantiatePrefab(mapPrefab, rootGo.transform);
            mapInstance.name = "MainMap";
            mapInstance.transform.SetSiblingIndex(0);
            mapInstance.transform.localPosition = Vector3.zero;
            var map = mapInstance.GetComponent<PlayableMainMap>();

            PlayableStageView stage = BuildStage(canvasRect, map);
            PlayableHudView hud = BuildHud(canvasRect);
            PlayableFloatingText[] prompts = BuildPrompts(canvasRect);
            PlayableBubbleView[] bubbles = BuildBubbles(canvasRect, bubblePrefab);
            Dictionary<string, PlayableChoiceGroup> groups = BuildChoiceGroups(canvasRect, buttonPrefab);
            (PlayableEmojiSpawner Emoji, PlayablePraiseView Praise) fx = BuildFx(canvasRect, emojiPrefabs);
            PlayableGuideHand guideHand = BuildGuideHand(canvasRect);
            (GameObject Card, Button[] Buttons) endCard = BuildEndCard(canvasRect);
            PlayableFadeOverlay fade = BuildFade(canvasRect);

            Util.SetRef(flow, "_config", config);
            Util.SetRef(flow, "_audio", audio);
            Util.SetRef(flow, "_exit", exit);
            Util.SetRef(flow, "_stage", stage);
            Util.SetRef(flow, "_hud", hud);
            Util.SetRef(flow, "_guideHand", guideHand);
            Util.SetRef(flow, "_praise", fx.Praise);
            Util.SetRef(flow, "_emoji", fx.Emoji);
            Util.SetRef(flow, "_fade", fade);
            Util.SetRefArray(flow, "_bubbles", bubbles);
            Util.SetRefArray(flow, "_prompts", prompts);
            Util.SetRef(flow, "_girlGroup", groups["girl"]);
            Util.SetRef(flow, "_dialogGroup", groups["dialog"]);
            Util.SetRef(flow, "_carGroup", groups["car"]);
            Util.SetRef(flow, "_houseGroup", groups["house"]);
            Util.SetRef(flow, "_clothesGroup", groups["clothes"]);
            Util.SetRef(flow, "_pardonGroup", groups["pardon"]);
            Util.SetRef(flow, "_endCard", endCard.Card);
            Util.SetRefArray(flow, "_exitButtons", endCard.Buttons);

            Util.ApplyAll();

            PrefabUtility.SaveAsPrefabAssetAndConnect(rootGo, RootPrefabPath, InteractionMode.AutomatedAction);
            EditorSceneManager.MarkSceneDirty(scene);

            // Neu scene demo dang mo san trong Editor thi khong ghi de len duong dan do duoc.
            // Dong ban dang mo (khong luu) roi moi ghi - noi dung cua no dang bi sinh lai toan bo.
            Scene alreadyOpen = SceneManager.GetSceneByPath(ScenePath);
            if (alreadyOpen.IsValid() && alreadyOpen.isLoaded && alreadyOpen != scene)
            {
                Debug.LogWarning(
                    $"[PlayableBuilder] {ScenePath} dang mo - dong lai (bo thay doi chua luu) de ghi de.");
                EditorSceneManager.CloseScene(alreadyOpen, true);
            }

            EditorSceneManager.SaveScene(scene, ScenePath);

            if (previousActive.IsValid())
            {
                SceneManager.SetActiveScene(previousActive);
            }

            EditorSceneManager.CloseScene(scene, true);
        }

        private static void CreateCamera()
        {
            // Thong so lay dung tu camera map cua game (LoadingScene): orthographic size 10.8, dat o z = -10.
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0f, 0f, -10f);

            Camera camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.orthographicSize = 10.8f;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 1000f;
        }

        /// <summary>
        /// EventSystem chi nam trong SCENE demo, khong nam trong prefab: du an dang bat Input System moi
        /// nen input module khac nhau tuy project. Ben nhan package tu them EventSystem cua ho.
        /// </summary>
        private static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();

            System.Type moduleType =
                System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

            if (moduleType != null)
            {
                go.AddComponent(moduleType);
                return;
            }

            Debug.LogWarning(
                "[PlayableBuilder] Khong tim thay InputSystemUIInputModule - hay tu them input module vao EventSystem.");
        }

        // ------------------------------------------------------------------ stage

        private static PlayableStageView BuildStage(RectTransform parent, PlayableMainMap map)
        {
            RectTransform stageRect = Util.NewRect("Stage", parent);
            Util.Stretch(stageRect);
            var stage = stageRect.gameObject.AddComponent<PlayableStageView>();

            Image musicFx = Util.NewImage("MusicFx", stageRect, Util.LoadSprite("Icons", "disc_101"), false);
            Util.Anchor(musicFx.rectTransform, new Vector2(0.5f, 0f), new Vector2(-330f, 900f),
                new Vector2(130f, 130f));
            musicFx.preserveAspect = true;
            musicFx.gameObject.SetActive(false);

            PlayableTapTargetView tapTarget = BuildTapTarget(stageRect);

            Util.SetRef(stage, "_map", map);
            Util.SetRef(stage, "_tapTarget", tapTarget);
            Util.SetRef(stage, "_musicFx", musicFx.rectTransform);

            return stage;
        }

        // Vi tri nhan vat nam lay theo game: StorySceneConstant.MainCharacterPosition = (-2.13, -7.85).
        private static readonly Vector3 ManPosition = new Vector3(-2.13f, -7.85f, 0f);

        // Game khong co vi tri "ban gai dung canh nhan vat chinh"; dat ben phai nhu ban playable goc.
        private static readonly Vector3 WomanPosition = new Vector3(1.4f, -7.85f, 0f);

        // Cao hon moi slot cua map (order 0). Khong dung sorting layer "Character" cua game vi sorting layer
        // la ProjectSettings - khong di theo .unitypackage, sang project khac se roi ve Default.
        private const int CharacterSortingOrder = 10;

        private static PlayableManView BuildMan(Transform parent)
        {
            var holder = new GameObject("man");
            holder.transform.SetParent(parent, false);
            holder.transform.localPosition = ManPosition;
            var view = holder.AddComponent<PlayableManView>();

            var dataAsset = Util.Load<SkeletonDataAsset>(
                Util.ArtRoot + "/Spine/Man/character1_SkeletonData.asset");
            AttachSpine(holder.transform, view, dataAsset);
            return view;
        }

        private static PlayableWomanView BuildWoman(Transform parent, string name, string npc)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            holder.transform.localPosition = WomanPosition;
            var view = holder.AddComponent<PlayableWomanView>();

            var dataAsset = Util.Load<SkeletonDataAsset>(
                $"{Util.ArtRoot}/Spine/Woman_{npc.Replace("npc_", string.Empty)}/{npc}_SkeletonData.asset");
            AttachSpine(holder.transform, view, dataAsset);

            holder.SetActive(false);
            return view;
        }

        /// <summary>
        /// Gan SkeletonRenderer + SkeletonAnimation (world space, MeshRenderer) vao node con "skeleton",
        /// dung bo component nhu prefab nhan vat cua game.
        /// </summary>
        private static void AttachSpine(Transform parent, PlayableSpineView view, SkeletonDataAsset dataAsset)
        {
            var go = new GameObject("skeleton");
            go.transform.SetParent(parent, false);

            if (dataAsset == null)
            {
                Debug.LogWarning($"[PlayableBuilder] {parent.name}: thieu SkeletonDataAsset.");
                return;
            }

            // BAY cua spine-unity 4.3: SkeletonAnimation.Awake chay ca o edit-mode va goi UpgradeTo43().
            // Khi co wasDeprecatedTransferred con false, no chep truong cu skeletonDataAssetDeprecated (null
            // tren component moi) DE LEN SkeletonRenderer.skeletonDataAsset. Vi vay:
            //  1. Them SkeletonAnimation TRUOC (de Awake + chuyen du lieu cu chay xong),
            //  2. roi moi gan skeletonDataAsset cho renderer,
            //  3. va danh dau wasDeprecatedTransferred = true de lan import prefab / load scene / Play
            //     sau khong chep de lan nua.
            // Khong goi Initialize o day: hook OnPostprocessPrefab cua Spine tu Initialize khi import,
            // luc chay thi Awake + PlayableSpineView.EnsureSpineReady lo.
            var skeletonRenderer = go.AddComponent<SkeletonRenderer>();
            var skeletonAnimation = go.AddComponent<SkeletonAnimation>();

            skeletonRenderer.skeletonDataAsset = dataAsset;
            Util.SetBool(skeletonAnimation, "wasDeprecatedTransferred", true);

            MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = go.AddComponent<MeshRenderer>();
            }

            meshRenderer.sortingOrder = CharacterSortingOrder;

            if (go.GetComponent<MeshFilter>() == null)
            {
                go.AddComponent<MeshFilter>();
            }

            Util.SetRef(view, "SpineRenderer", skeletonRenderer);
            Util.SetRef(view, "SpineAnimation", skeletonAnimation);
        }

        private static PlayableTapTargetView BuildTapTarget(RectTransform parent)
        {
            RectTransform rect = Util.NewRect("TapTarget", parent);
            Util.Anchor(rect, new Vector2(0.5f, 0f), new Vector2(0f, 700f), new Vector2(240f, 240f));

            var hit = rect.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            hit.raycastTarget = true;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;

            Image glow = Util.NewImage("Glow", rect, Util.LoadSprite("Fx", "unit"), false);
            Util.Anchor(glow.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260f, 260f));
            glow.color = new Color(1f, 0.86f, 0.29f, 0.55f);

            Image ring = Util.NewImage("Ring", rect, Util.LoadSprite("Fx", "unit"), false);
            Util.Anchor(ring.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 240f));
            ring.color = new Color(1f, 1f, 1f, 0.6f);
            CanvasGroup ringGroup = Util.AddGroup(ring);
            ringGroup.alpha = 0f;

            Image icon = Util.NewImage("Icon", rect, Util.LoadSprite("Icons", "disc_101"), false);
            Util.Anchor(icon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(190f, 190f));
            icon.preserveAspect = true;

            var view = rect.gameObject.AddComponent<PlayableTapTargetView>();
            Util.SetRef(view, "_button", button);
            Util.SetRef(view, "_icon", icon.rectTransform);
            Util.SetRef(view, "_ring", ring.rectTransform);
            Util.SetRef(view, "_ringGroup", ringGroup);

            rect.gameObject.SetActive(false);
            return view;
        }

        // ------------------------------------------------------------------ hud + prompt

        private static PlayableHudView BuildHud(RectTransform parent)
        {
            RectTransform rect = Util.NewRect("Hud", parent);
            Util.Anchor(rect, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(660f, 130f));

            CanvasGroup group = rect.gameObject.AddComponent<CanvasGroup>();

            Image background = Util.NewStretchImage("Bg", rect, Util.LoadSprite("Ui", "bg_energy_top"));
            background.type = Image.Type.Sliced;

            Image icon = Util.NewImage("Icon", rect, Util.LoadSprite("Ui", "0"), false);
            Util.Anchor(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(70f, 12f),
                new Vector2(80f, 80f));
            icon.preserveAspect = true;

            TextMeshProUGUI value = Util.NewText("Value", rect, "0", 56f, s_font, TextDark);
            Util.Anchor(value.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(40f, 12f),
                new Vector2(460f, 70f));

            RectTransform progress = Util.NewRect("Progress", rect);
            Util.Anchor(progress, new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(560f, 26f));

            Image track = Util.NewStretchImage("Track", progress, Util.LoadSprite("Ui", "button_yellow"));
            track.type = Image.Type.Sliced;
            track.color = new Color(0f, 0f, 0f, 0.35f);

            Image fill = Util.NewStretchImage("Fill", progress, Util.LoadSprite("Ui", "button_green"));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 0f;

            var view = rect.gameObject.AddComponent<PlayableHudView>();
            Util.SetRef(view, "_group", group);
            Util.SetRef(view, "_value", value);
            Util.SetRef(view, "_fill", fill);
            Util.SetColor(view, "_normalColor", TextDark);
            Util.SetColor(view, "_errorColor", new Color(0.85f, 0.15f, 0.15f));

            return view;
        }

        private static PlayableFloatingText[] BuildPrompts(RectTransform parent)
        {
            RectTransform holder = Util.NewRect("Prompts", parent);
            Util.Stretch(holder);

            return new[]
            {
                BuildPrompt(holder, "Prompt_TapToRap", "TAP TO MAKE MONEY!", new Vector2(0f, 980f)),
                BuildPrompt(holder, "Prompt_ChooseGirl", "CHOOSE YOUR GIRL", new Vector2(0f, 760f))
            };
        }

        private static PlayableFloatingText BuildPrompt(RectTransform parent, string name, string content,
            Vector2 position)
        {
            RectTransform rect = Util.NewRect(name, parent);
            Util.Anchor(rect, new Vector2(0.5f, 0f), position, new Vector2(900f, 90f));

            TextMeshProUGUI label = Util.NewText("Label", rect, content, 56f, s_font, TextLight);
            Util.Stretch(label.rectTransform);
            label.fontStyle = FontStyles.Bold;

            var view = rect.gameObject.AddComponent<PlayableFloatingText>();
            Util.SetRef(view, "_content", rect);
            Util.SetRef(view, "_label", label);

            rect.gameObject.SetActive(false);
            return view;
        }

        // ------------------------------------------------------------------ bubble

        private static PlayableBubbleView[] BuildBubbles(RectTransform parent, GameObject prefab)
        {
            RectTransform holder = Util.NewRect("Bubbles", parent);
            Util.Stretch(holder);

            (string Name, string Text, Vector2 Position, bool LoopScale, bool LoopMove)[] specs =
            {
                ("Bubble_GirlTalk", "Buy me something nice!", new Vector2(230f, 1150f), true, false),
                ("Bubble_Anger", "You are broke! Bye!", new Vector2(230f, 1180f), false, false),
                ("Bubble_RapHint", "Tap to drop a beat!", new Vector2(-60f, 980f), false, true),
                ("Bubble_PraiseA", "He is on fire!", new Vector2(-330f, 1320f), false, true),
                ("Bubble_PraiseB", "That flow is insane!", new Vector2(330f, 1400f), false, true)
            };

            var views = new PlayableBubbleView[specs.Length];
            for (int i = 0; i < specs.Length; i++)
            {
                (string name, string text, Vector2 position, bool loopScale, bool loopMove) = specs[i];

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder);
                instance.name = name;

                var rect = (RectTransform)instance.transform;
                Util.Anchor(rect, new Vector2(0.5f, 0f), position, new Vector2(460f, 200f));

                var view = instance.GetComponent<PlayableBubbleView>();
                view.SetText(text);
                Util.SetBool(view, "_loopScale", loopScale);
                Util.SetBool(view, "_loopMove", loopMove);

                instance.SetActive(false);
                views[i] = view;
            }

            return views;
        }

        // ------------------------------------------------------------------ choice group

        private static Dictionary<string, PlayableChoiceGroup> BuildChoiceGroups(RectTransform parent,
            GameObject buttonPrefab)
        {
            RectTransform holder = Util.NewRect("Choices", parent);
            Util.Stretch(holder);

            var groups = new Dictionary<string, PlayableChoiceGroup>
            {
                ["girl"] = BuildGroup(holder, buttonPrefab, "Choices_Girl",
                    new[] { "icon_lover_1", "icon_lover_2" }, "Icons",
                    new[] { string.Empty, string.Empty }, 560f, new Vector2(300f, 340f)),

                ["dialog"] = BuildGroup(holder, buttonPrefab, "Choices_Dialog",
                    new string[] { null, null }, "Icons",
                    new[] { "I will make it big", "Give me some time" }, 560f, new Vector2(440f, 150f)),

                // Icon nut lay tu chinh mang level cua map -> khong the lech voi vat the se hien ra.
                ["car"] = BuildGroup(holder, buttonPrefab, "Choices_Car",
                    CarLevels[1..], "Stage",
                    new[] { string.Empty, string.Empty, string.Empty }, 400f, new Vector2(320f, 340f)),

                ["house"] = BuildGroup(holder, buttonPrefab, "Choices_House",
                    HouseLevels[1..], "Stage",
                    new[] { string.Empty, string.Empty, string.Empty }, 400f, new Vector2(320f, 340f)),

                ["clothes"] = BuildGroup(holder, buttonPrefab, "Choices_Clothes",
                    new[] { "icon_property_401", "icon_property_402", "icon_property_403" }, "Icons",
                    new[] { string.Empty, string.Empty, string.Empty }, 400f, new Vector2(320f, 340f)),

                ["pardon"] = BuildGroup(holder, buttonPrefab, "Choices_Pardon",
                    new[] { "HEART", "button_check_off" }, "Ui",
                    new[] { "Forgive her", "Walk away" }, 560f, new Vector2(440f, 260f))
            };

            return groups;
        }

        private static PlayableChoiceGroup BuildGroup(RectTransform parent, GameObject buttonPrefab, string name,
            IReadOnlyList<string> icons, string iconFolder, IReadOnlyList<string> titles, float spacing,
            Vector2 buttonSize)
        {
            RectTransform rect = Util.NewRect(name, parent);
            Util.Anchor(rect, new Vector2(0.5f, 0f), new Vector2(0f, 420f), new Vector2(1000f, 400f));

            var group = rect.gameObject.AddComponent<PlayableChoiceGroup>();

            int count = icons.Count;
            float start = -(count - 1) * 0.5f * spacing;

            var buttons = new PlayableChoiceButton[count];
            for (int i = 0; i < count; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, rect);
                instance.name = $"Button_{i}";

                var buttonRect = (RectTransform)instance.transform;
                Util.Anchor(buttonRect, new Vector2(0.5f, 0.5f), new Vector2(start + (i * spacing), 0f),
                    buttonSize);

                var view = instance.GetComponent<PlayableChoiceButton>();
                view.SetIcon(icons[i] == null ? null : Util.LoadSprite(iconFolder, icons[i]));
                view.SetTitle(titles[i]);
                buttons[i] = view;
            }

            Util.SetRefArray(group, "_buttons", buttons);
            rect.gameObject.SetActive(false);
            return group;
        }

        // ------------------------------------------------------------------ fx

        private static (PlayableEmojiSpawner, PlayablePraiseView) BuildFx(RectTransform parent,
            List<GameObject> emojiPrefabs)
        {
            RectTransform holder = Util.NewRect("Fx", parent);
            Util.Stretch(holder);

            RectTransform spawnPoint = Util.NewRect("EmojiSpawnPoint", holder);
            Util.Anchor(spawnPoint, new Vector2(0.5f, 0f), new Vector2(-200f, 1000f), new Vector2(10f, 10f));

            var spawner = holder.gameObject.AddComponent<PlayableEmojiSpawner>();
            Util.SetRef(spawner, "_spawnPoint", spawnPoint);
            Util.SetRef(spawner, "_container", holder);
            Util.SetRefArray(spawner, "_prefabs", emojiPrefabs);

            RectTransform praiseRect = Util.NewRect("Praise", holder);
            Util.Anchor(praiseRect, new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(800f, 200f));
            var praise = praiseRect.gameObject.AddComponent<PlayablePraiseView>();

            CanvasGroup nice = BuildPraiseLabel(praiseRect, "Nice", "NICE!", new Color(1f, 0.85f, 0.2f));
            CanvasGroup awesome = BuildPraiseLabel(praiseRect, "Awesome", "AWESOME!",
                new Color(1f, 0.45f, 0.75f));

            Util.SetRef(praise, "_niceGroup", nice);
            Util.SetRef(praise, "_awesomeGroup", awesome);

            return (spawner, praise);
        }

        private static CanvasGroup BuildPraiseLabel(RectTransform parent, string name, string content, Color color)
        {
            RectTransform rect = Util.NewRect(name, parent);
            Util.Anchor(rect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(800f, 160f));

            CanvasGroup group = rect.gameObject.AddComponent<CanvasGroup>();

            TextMeshProUGUI label = Util.NewText("Label", rect, content, 110f, s_font, color);
            Util.Stretch(label.rectTransform);
            label.fontStyle = FontStyles.Bold;

            rect.gameObject.SetActive(false);
            return group;
        }

        private static PlayableGuideHand BuildGuideHand(RectTransform parent)
        {
            RectTransform rect = Util.NewRect("GuideHand", parent);
            Util.Stretch(rect);

            var view = rect.gameObject.AddComponent<PlayableGuideHand>();

            Image hand = Util.NewImage("Hand", rect, Util.LoadSprite("Fx", "guidehand"), false);
            Util.Anchor(hand.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f));
            hand.preserveAspect = true;
            CanvasGroup handGroup = Util.AddGroup(hand);
            handGroup.alpha = 0f;

            Util.SetRef(view, "_hand", hand.rectTransform);
            Util.SetRef(view, "_handGroup", handGroup);
            return view;
        }

        // ------------------------------------------------------------------ end card + fade

        private static (GameObject, Button[]) BuildEndCard(RectTransform parent)
        {
            RectTransform rect = Util.NewRect("EndCard", parent);
            Util.Stretch(rect);

            Image dim = Util.NewStretchImage("Dim", rect, null);
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            dim.raycastTarget = true;

            RectTransform logoRect = Util.NewRect("Logo", rect);
            Util.Anchor(logoRect, new Vector2(0.5f, 0.5f), new Vector2(0f, 220f), new Vector2(360f, 360f));
            var logoImage = logoRect.gameObject.AddComponent<Image>();
            logoImage.sprite = Util.LoadSprite("Ui", "rap_icon_2");
            logoImage.preserveAspect = true;
            Button logoButton = logoRect.gameObject.AddComponent<Button>();
            logoButton.targetGraphic = logoImage;

            TextMeshProUGUI title = Util.NewText("Title", rect, "Live the rags-to-riches dream", 52f, s_font,
                TextLight);
            Util.Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -60f),
                new Vector2(900f, 120f));
            title.enableWordWrapping = true;

            RectTransform downloadRect = Util.NewRect("BtnDownload", rect);
            Util.Anchor(downloadRect, new Vector2(0.5f, 0.5f), new Vector2(0f, -280f),
                new Vector2(560f, 150f));
            var downloadImage = downloadRect.gameObject.AddComponent<Image>();
            downloadImage.sprite = Util.LoadSprite("Ui", "button_green");
            downloadImage.type = Image.Type.Sliced;
            Button downloadButton = downloadRect.gameObject.AddComponent<Button>();
            downloadButton.targetGraphic = downloadImage;

            TextMeshProUGUI downloadLabel = Util.NewText("Label", downloadRect, "PLAY NOW", 56f, s_font,
                TextLight);
            Util.Stretch(downloadLabel.rectTransform);
            downloadLabel.fontStyle = FontStyles.Bold;

            rect.gameObject.SetActive(false);
            return (rect.gameObject, new[] { logoButton, downloadButton });
        }

        private static PlayableFadeOverlay BuildFade(RectTransform parent)
        {
            Image image = Util.NewStretchImage("Fade", parent, null);
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = false;

            var view = image.gameObject.AddComponent<PlayableFadeOverlay>();
            Util.SetRef(view, "_image", image);
            return view;
        }

        // ------------------------------------------------------------------ audio

        private static void ConfigureAudio(PlayableAudio audio)
        {
            (PlayableSfx Sfx, AudioClip Clip)[] entries =
            {
                (Click, Util.LoadAudio("Sfx", "sfx_button_2.ogg")),
                (Money, Util.LoadAudio("Sfx", "sfx_cash_2.ogg")),
                (Shiny, Util.LoadAudio("Sfx", "sfx_ting.mp3")),
                (Whoosh, Util.LoadAudio("Sfx", "sfx_beat_switch_2.mp3")),
                (PutDown, Util.LoadAudio("Sfx", "sfx_close_modal_2.ogg")),

                // TODO(asset): bon clip thoai nhan vat chua co. Tam dung sfx_talk cho co tieng;
                // khi co file that thi doi Clip o day (hoac keo thang vao Inspector).
                (ManCrying, Util.LoadAudio("Sfx", "sfx_talk.mp3")),
                (ManHappy, Util.LoadAudio("Sfx", "sfx_talk.mp3")),
                (WomanAngry, Util.LoadAudio("Sfx", "sfx_talk.mp3")),
                (WomanSatisfied, Util.LoadAudio("Sfx", "sfx_talk.mp3")),

                (BeatLoop, Util.LoadAudio("Music", "beat_101.ogg")),
                (Vocal, Util.LoadAudio("Music", "vocal_101.ogg"))
            };

            var serialized = new SerializedObject(audio);
            SerializedProperty array = serialized.FindProperty("_entries");
            array.arraySize = entries.Length;

            for (int i = 0; i < entries.Length; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Sfx").enumValueIndex = (int)entries[i].Sfx;
                element.FindPropertyRelative("Clip").objectReferenceValue = entries[i].Clip;
                element.FindPropertyRelative("Volume").floatValue = 1f;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
