using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace _Playable.Editor
{
    /// <summary>
    /// Ham tien ich dung chung cho <see cref="PlayableSceneBuilder"/>: tao node UI, nap asset,
    /// va gan gia tri vao cac field [SerializeField] private qua SerializedObject.
    /// </summary>
    internal static class PlayableBuildUtility
    {
        public const string ArtRoot = "Assets/_Playable/Art";

        private static readonly Dictionary<Object, SerializedObject> SerializedCache = new Dictionary<Object, SerializedObject>();

        public static void ClearCache()
        {
            SerializedCache.Clear();
        }

        // ------------------------------------------------------------------ asset

        public static Sprite LoadSprite(string folder, string file)
        {
            string path = $"{ArtRoot}/Sprites/{folder}/{file}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning($"[PlayableBuilder] Khong nap duoc sprite: {path}");
            }

            return sprite;
        }

        public static AudioClip LoadAudio(string folder, string file)
        {
            string path = $"{ArtRoot}/Audio/{folder}/{file}";
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogWarning($"[PlayableBuilder] Khong nap duoc audio: {path}");
            }

            return clip;
        }

        public static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                Debug.LogWarning($"[PlayableBuilder] Khong nap duoc {typeof(T).Name}: {path}");
            }

            return asset;
        }

        // ------------------------------------------------------------------ node

        public static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(100f, 100f);
            return rect;
        }

        public static Image NewImage(string name, Transform parent, Sprite sprite, bool nativeSize = true)
        {
            RectTransform rect = NewRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;

            if (sprite != null && nativeSize)
            {
                image.SetNativeSize();
            }

            return image;
        }

        /// <summary>Image phu kin khung cha.</summary>
        public static Image NewStretchImage(string name, Transform parent, Sprite sprite)
        {
            Image image = NewImage(name, parent, sprite, false);
            Stretch(image.rectTransform);
            return image;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position, Vector2? size = null)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;

            if (size.HasValue)
            {
                rect.sizeDelta = size.Value;
            }
        }

        public static Text NewText(string name, Transform parent, string content, float fontSize,
            Font font, Color color)
        {
            RectTransform rect = NewRect(name, parent);
            rect.sizeDelta = new Vector2(600f, 90f);

            var text = rect.gameObject.AddComponent<Text>();
            text.text = content;
            text.fontSize = Mathf.RoundToInt(fontSize);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            if (font != null)
            {
                text.font = font;
            }

            return text;
        }

        public static CanvasGroup AddGroup(Component target)
        {
            return target.gameObject.AddComponent<CanvasGroup>();
        }

        // ------------------------------------------------------------------ serialized field

        private static SerializedObject Serialized(Object target)
        {
            if (!SerializedCache.TryGetValue(target, out SerializedObject serialized))
            {
                serialized = new SerializedObject(target);
                SerializedCache[target] = serialized;
            }

            return serialized;
        }

        public static void Apply(Object target)
        {
            if (SerializedCache.TryGetValue(target, out SerializedObject serialized))
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        public static void ApplyAll()
        {
            foreach (SerializedObject serialized in SerializedCache.Values)
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            SerializedCache.Clear();
        }

        private static SerializedProperty Find(Object target, string field)
        {
            SerializedProperty property = Serialized(target).FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"[PlayableBuilder] {target.GetType().Name} khong co field '{field}'.");
            }

            return property;
        }

        public static void SetRef(Object target, string field, Object value)
        {
            Find(target, field).objectReferenceValue = value;
        }

        public static void SetBool(Object target, string field, bool value)
        {
            Find(target, field).boolValue = value;
        }

        public static void SetFloat(Object target, string field, float value)
        {
            Find(target, field).floatValue = value;
        }

        public static void SetString(Object target, string field, string value)
        {
            Find(target, field).stringValue = value;
        }

        public static void SetColor(Object target, string field, Color value)
        {
            Find(target, field).colorValue = value;
        }

        public static void SetVector2(Object target, string field, Vector2 value)
        {
            Find(target, field).vector2Value = value;
        }

        public static void SetRefArray(Object target, string field, IReadOnlyList<Object> values)
        {
            SerializedProperty property = Find(target, field);
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
