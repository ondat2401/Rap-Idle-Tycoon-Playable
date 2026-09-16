using UnityEngine;
using UnityEditor;
using System.Linq;
using Amanotes.Core;

namespace Amanotes.Core.Editor
{
    public class AtlasSpriteViewer : EditorWindow
    {
        private Texture2D atlasTexture;
        private Sprite[] sprites;
        private Vector2 scrollPos;
        private float previewSize = 64f;

        [MenuItem("Tools/Playable Standard Pipeline/Atlas Packer/Atlas Sprite Viewer")]
        public static void ShowWindow()
        {
            GetWindow<AtlasSpriteViewer>("Atlas Sprite Viewer");
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Atlas Sprite Viewer", EditorStyles.boldLabel);

            atlasTexture = (Texture2D)EditorGUILayout.ObjectField("Atlas Texture", atlasTexture, typeof(Texture2D), false);

            if (atlasTexture != null && GUILayout.Button("Load Sprites"))
            {
                LoadSprites();
            }

            if (sprites != null && sprites.Length > 0)
            {
                previewSize = EditorGUILayout.Slider("Preview Size", previewSize, 32f, 128f);

                EditorGUILayout.LabelField($"Sprites: {sprites.Length}");

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

                int columns = Mathf.Max(1, (int)(position.width / (previewSize + 10)));

                for (int i = 0; i < sprites.Length; i++)
                {
                    if (i % columns == 0)
                        EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.BeginVertical(GUILayout.Width(previewSize + 5));

                    Rect rect = GUILayoutUtility.GetRect(previewSize, previewSize);
                    if (sprites[i] != null)
                    {
                        DrawSprite(rect, sprites[i]);
                        EditorGUILayout.LabelField(sprites[i].name, EditorStyles.miniLabel);
                    }

                    EditorGUILayout.EndVertical();

                    if ((i + 1) % columns == 0 || i == sprites.Length - 1)
                        EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }
        }

        void LoadSprites()
        {
            string path = AssetDatabase.GetAssetPath(atlasTexture);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            sprites = assets.OfType<Sprite>().ToArray();

            Debug.Log($"[AtlasSpriteViewer] Loaded {sprites.Length} sprites from {path}");
        }

        void DrawSprite(Rect rect, Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return;

            Rect spriteRect = sprite.textureRect;
            Texture2D tex = sprite.texture;

            // Calculate normalized coordinates
            Rect coords = new Rect(
                spriteRect.x / tex.width,
                spriteRect.y / tex.height,
                spriteRect.width / tex.width,
                spriteRect.height / tex.height
            );

            GUI.DrawTextureWithTexCoords(rect, tex, coords);
        }
    }
}
