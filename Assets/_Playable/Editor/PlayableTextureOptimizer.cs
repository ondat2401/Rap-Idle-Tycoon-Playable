using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace _Playable.Editor
{
    /// <summary>
    /// Thu nho file PNG nguon cua atlas Spine trong ban copy o <c>_Playable/Art</c>.
    ///
    /// Vi sao an toan: UV cua Spine duoc chuan hoa theo dong <c>size:</c> trong file .atlas, khong theo
    /// so pixel that cua texture - dung co che ma Unity dang dung khi ap maxTextureSize. Import setting
    /// von da gioi han 2048 nen hinh anh luc chay khong doi; viec nay chi cat dung lung file nguon
    /// (character1.png dang 14.6 MB) cho .unitypackage nhe di.
    ///
    /// Chi can chay MOT LAN sau khi copy art. Chay lai nhieu lan se tiep tuc re-encode vo ich.
    /// </summary>
    internal static class PlayableTextureOptimizer
    {
        private const int MaxSize = 2048;

        private static readonly string[] Targets =
        {
            PlayableBuildUtility.ArtRoot + "/Spine/Man/character1.png",
            PlayableBuildUtility.ArtRoot + "/Spine/Lover3/lover_3.png",
            PlayableBuildUtility.ArtRoot + "/Spine/Lover4/lover_4.png"
        };

        [MenuItem("Tools/Playable/Optimize Spine Textures")]
        public static void Optimize()
        {
            var report = new List<string>();

            foreach (string path in Targets)
            {
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[PlayableTextureOptimizer] Khong thay {path}");
                    continue;
                }

                long before = new FileInfo(path).Length;
                if (!Resize(path))
                {
                    continue;
                }

                long after = new FileInfo(path).Length;
                report.Add($"{Path.GetFileName(path)}: {before / 1024}KB -> {after / 1024}KB");
            }

            AssetDatabase.Refresh();
            Debug.Log($"[PlayableTextureOptimizer] {string.Join(" | ", report)}");
        }

        private static bool Resize(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                return false;
            }

            var source = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (source == null)
            {
                return false;
            }

            // Chu y: source.width la kich thuoc SAU khi importer ap maxTextureSize, khong phai kich thuoc
            // that cua file. Phai doc header PNG moi biet file nguon co thuc su qua kho hay khong.
            if (!TryReadPngSize(path, out int fileWidth, out int fileHeight))
            {
                Debug.LogWarning($"[PlayableTextureOptimizer] Khong doc duoc kich thuoc PNG: {path}");
                return false;
            }

            if (fileWidth <= MaxSize && fileHeight <= MaxSize)
            {
                Debug.Log(
                    $"[PlayableTextureOptimizer] {Path.GetFileName(path)} da {fileWidth}x{fileHeight}, bo qua.");
                return false;
            }

            // Texture da nap san o muc <= MaxSize, ghi lai dung kich thuoc do.
            int width = source.width;
            int height = source.height;

            // Blit qua RenderTexture linear de giu nguyen kenh alpha cua atlas.
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture temp = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear);

            var resized = new Texture2D(width, height, TextureFormat.RGBA32, false, true);

            try
            {
                Graphics.Blit(source, temp);
                RenderTexture.active = temp;
                resized.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                resized.Apply();

                File.WriteAllBytes(path, resized.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(temp);
                Object.DestroyImmediate(resized);
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return true;
        }

        /// <summary>Doc width/height tu chunk IHDR cua file PNG (big-endian, offset 16 va 20).</summary>
        private static bool TryReadPngSize(string path, out int width, out int height)
        {
            width = 0;
            height = 0;

            byte[] header = new byte[24];
            using (FileStream stream = File.OpenRead(path))
            {
                if (stream.Read(header, 0, header.Length) != header.Length)
                {
                    return false;
                }
            }

            if (header[0] != 0x89 || header[1] != 'P' || header[2] != 'N' || header[3] != 'G')
            {
                return false;
            }

            width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
            height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
            return width > 0 && height > 0;
        }
    }
}
