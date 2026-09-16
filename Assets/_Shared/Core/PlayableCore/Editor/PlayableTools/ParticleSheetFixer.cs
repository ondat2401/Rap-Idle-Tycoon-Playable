using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

namespace Amanotes.Core.Editor
{
    public class ParticleSheetFixer : EditorWindow
    {
        private GameObject targetGO;
        private Vector2 scrollPos;
        private List<ParticleSheetEntry> entries = new List<ParticleSheetEntry>();
        private FixMode fixMode = FixMode.ShaderFlipbook;

        private enum FixMode
        {
            ShaderFlipbook,
            BakeToSpriteAnimation
        }

        private class ParticleSheetEntry
        {
            public ParticleSystem ps;
            public ParticleSystemRenderer psr;
            public Material originalMaterial;
            public Texture2D sheetTexture;
            public int tilesX;
            public int tilesY;
            public int totalFrames;
            public float fps;
            public bool selected;
            public bool processed;
            public string status;
        }

        [MenuItem("Tools/Playable Standard Pipeline/Particle Sheet Fixer (Luna)")]
        public static void ShowWindow()
        {
            var window = GetWindow<ParticleSheetFixer>("Particle Sheet Fixer");
            window.minSize = new Vector2(520, 420);
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawTargetField();

            if (entries.Count > 0)
            {
                DrawEntries();
                DrawActions();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Drag a Particle GameObject above.\n\n" +
                    "This tool fixes Luna's Texture Sheet Animation bug\n" +
                    "(only shows last frame) by replacing it with a\n" +
                    "shader-based flipbook that animates UV in the fragment shader.\n\n" +
                    "Luna renders shaders correctly — the animation logic\n" +
                    "moves from C# (broken on Luna) to GPU (works everywhere).",
                    MessageType.Info);
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Particle Sheet Fixer (Luna)", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);
        }

        private void DrawTargetField()
        {
            EditorGUI.BeginChangeCheck();
            targetGO = (GameObject)EditorGUILayout.ObjectField("Particle GameObject", targetGO, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck() && targetGO != null)
                ScanParticleSystems();

            EditorGUILayout.Space(4);
        }

        private void DrawEntries()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                e.selected = EditorGUILayout.ToggleLeft(e.ps.name, e.selected, EditorStyles.boldLabel);
                if (e.processed)
                {
                    var prev = GUI.color;
                    GUI.color = Color.green;
                    EditorGUILayout.LabelField("✓", GUILayout.Width(20));
                    GUI.color = prev;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("Texture", e.sheetTexture != null ? e.sheetTexture.name : "(none)");
                EditorGUILayout.LabelField("Grid", $"{e.tilesX} x {e.tilesY} = {e.totalFrames} frames");
                e.fps = EditorGUILayout.FloatField("FPS", e.fps);
                if (!string.IsNullOrEmpty(e.status))
                    EditorGUILayout.HelpBox(e.status, MessageType.Info);
                EditorGUI.indentLevel--;

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawActions()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Fix Mode", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Shader Flipbook: Disables Texture Sheet Animation module,\n" +
                "creates a shader that animates UV per-particle using Custom Vertex Streams.\n" +
                "Works on Luna because animation logic lives in the shader.",
                MessageType.None);

            EditorGUILayout.Space(4);

            var anySelected = entries.Exists(e => e.selected && !e.processed);
            EditorGUI.BeginDisabledGroup(!anySelected);
            if (GUILayout.Button("Fix Selected (Shader Flipbook)", GUILayout.Height(30)))
                FixSelected();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndVertical();
        }

        private void ScanParticleSystems()
        {
            entries.Clear();
            if (targetGO == null) return;

            var systems = targetGO.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in systems)
            {
                var tsa = ps.textureSheetAnimation;
                if (!tsa.enabled) continue;
                if (tsa.mode != ParticleSystemAnimationMode.Grid) continue;

                var psr = ps.GetComponent<ParticleSystemRenderer>();
                if (psr == null) continue;

                var mat = psr.sharedMaterial;
                if (mat == null || mat.mainTexture == null) continue;

                var tex = mat.mainTexture as Texture2D;
                if (tex == null) continue;

                float estimatedFps = EstimateFps(ps, tsa);

                entries.Add(new ParticleSheetEntry
                {
                    ps = ps,
                    psr = psr,
                    originalMaterial = mat,
                    sheetTexture = tex,
                    tilesX = tsa.numTilesX,
                    tilesY = tsa.numTilesY,
                    totalFrames = tsa.numTilesX * tsa.numTilesY,
                    fps = estimatedFps,
                    selected = true,
                    processed = false,
                    status = ""
                });
            }

            if (entries.Count == 0)
                EditorUtility.DisplayDialog("No Sheet Animation Found",
                    "No ParticleSystem with Texture Sheet Animation (Grid mode) found.",
                    "OK");
        }

        private float EstimateFps(ParticleSystem ps, ParticleSystem.TextureSheetAnimationModule tsa)
        {
            int frames = tsa.numTilesX * tsa.numTilesY;
            float lifetime = ps.main.startLifetime.constantMax;
            if (lifetime <= 0f) lifetime = 1f;
            int cycles = tsa.cycleCount;
            if (cycles <= 0) cycles = 1;
            return (frames * cycles) / lifetime;
        }

        private void FixSelected()
        {
            Undo.RegisterFullObjectHierarchyUndo(targetGO, "Particle Sheet Fixer - Shader Flipbook");

            var shader = GetOrCreateFlipbookShader();
            if (shader == null)
            {
                EditorUtility.DisplayDialog("Error", "Failed to create flipbook shader.", "OK");
                return;
            }

            int converted = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (!e.selected || e.processed) continue;

                ApplyShaderFlipbook(e, shader);
                converted++;
                e.processed = true;
            }

            if (converted > 0)
            {
                ApplyPrefabChanges();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            EditorUtility.DisplayDialog("Done", $"Fixed {converted} particle system(s) with shader flipbook.", "OK");
        }

        private void ApplyShaderFlipbook(ParticleSheetEntry entry, Shader shader)
        {
            var ps = entry.ps;
            var psr = entry.psr;

            var tsa = ps.textureSheetAnimation;
            tsa.enabled = false;

            var matFolder = GetMaterialFolder(entry);
            var matName = $"{entry.sheetTexture.name}_flipbook_{entry.tilesX}x{entry.tilesY}";
            var matPath = $"{matFolder}/{matName}.mat";

            Material mat;
            var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existing != null)
            {
                mat = existing;
                entry.status = $"Reused existing material: {matName}";
            }
            else
            {
                mat = new Material(shader);
                mat.name = matName;
                mat.SetTexture("_MainTex", entry.sheetTexture);
                mat.SetFloat("_TilesX", entry.tilesX);
                mat.SetFloat("_TilesY", entry.tilesY);
                mat.SetFloat("_TotalFrames", entry.totalFrames);
                mat.SetFloat("_FPS", entry.fps);

                CopyBlendMode(entry.originalMaterial, mat);

                if (!Directory.Exists(matFolder))
                    Directory.CreateDirectory(matFolder);
                AssetDatabase.Refresh();
                AssetDatabase.CreateAsset(mat, matPath);
                entry.status = $"Created material: {matName}";
            }

            mat.SetFloat("_FPS", entry.fps);
            psr.sharedMaterial = mat;

            SetupCustomVertexStreams(psr);
        }

        private void SetupCustomVertexStreams(ParticleSystemRenderer psr)
        {
            var streams = new List<ParticleSystemVertexStream>
            {
                ParticleSystemVertexStream.Position,
                ParticleSystemVertexStream.Color,
                ParticleSystemVertexStream.UV,
                ParticleSystemVertexStream.AgePercent
            };
            psr.SetActiveVertexStreams(streams);
        }

        private void CopyBlendMode(Material src, Material dst)
        {
            if (src.HasProperty("_SrcBlend"))
                dst.SetFloat("_SrcBlend", src.GetFloat("_SrcBlend"));
            if (src.HasProperty("_DstBlend"))
                dst.SetFloat("_DstBlend", src.GetFloat("_DstBlend"));

            dst.renderQueue = src.renderQueue;

            var srcKeywords = src.shaderKeywords;
            foreach (var kw in srcKeywords)
            {
                if (kw.Contains("BLEND") || kw.Contains("ALPHA"))
                    dst.EnableKeyword(kw);
            }
        }

        private string GetMaterialFolder(ParticleSheetEntry entry)
        {
            var texPath = AssetDatabase.GetAssetPath(entry.sheetTexture);
            if (!string.IsNullOrEmpty(texPath))
                return Path.GetDirectoryName(texPath);

            var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(targetGO);
            if (!string.IsNullOrEmpty(prefabPath))
                return Path.GetDirectoryName(prefabPath);

            return "Assets";
        }

        private Shader GetOrCreateFlipbookShader()
        {
            var shaderPath = "Assets/_Shared/Shaders/Particles-Flipbook-Luna.shader";
            var existing = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            if (existing != null) return existing;

            var dir = Path.GetDirectoryName(shaderPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(shaderPath, GetFlipbookShaderSource());
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
        }

        private string GetFlipbookShaderSource()
        {
            return @"Shader ""Particles/Flipbook-Luna""
{
    Properties
    {
        _MainTex (""Sprite Sheet"", 2D) = ""white"" {}
        _TilesX (""Tiles X"", Float) = 4
        _TilesY (""Tiles Y"", Float) = 4
        _TotalFrames (""Total Frames"", Float) = 16
        _FPS (""FPS"", Float) = 24
        _TintColor (""Tint Color"", Color) = (1,1,1,1)
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend (""Src Blend"", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend (""Dst Blend"", Float) = 10
    }

    SubShader
    {
        Tags { ""Queue""=""Transparent"" ""RenderType""=""Transparent"" ""IgnoreProjector""=""True"" ""PreviewType""=""Plane"" }
        Blend [_SrcBlend] [_DstBlend]
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_particles
            #include ""UnityCG.cginc""

            sampler2D _MainTex;
            float _TilesX;
            float _TilesY;
            float _TotalFrames;
            float _FPS;
            fixed4 _TintColor;

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float agePercent : TEXCOORD1;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;

                float frame = floor(v.agePercent * _TotalFrames);
                frame = min(frame, _TotalFrames - 1.0);

                float col = fmod(frame, _TilesX);
                float row = floor(frame / _TilesX);
                row = (_TilesY - 1.0) - row;

                float2 tiling = float2(1.0 / _TilesX, 1.0 / _TilesY);
                float2 offset = float2(col * tiling.x, row * tiling.y);

                o.uv = v.uv * tiling + offset;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color * _TintColor;
                return col;
            }
            ENDCG
        }
    }
    FallBack ""Particles/Alpha Blended""
}";
        }

        private void ApplyPrefabChanges()
        {
            if (targetGO == null) return;

            var prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(targetGO);
            if (prefabRoot != null)
                PrefabUtility.ApplyPrefabInstance(prefabRoot, InteractionMode.UserAction);
        }
    }
}
