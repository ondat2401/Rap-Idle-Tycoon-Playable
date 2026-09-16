using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace _Playable.Editor
{
    /// <summary>
    /// CONG CU PHAT TRIEN - XOA FILE NAY TRUOC KHI EXPORT .unitypackage.
    ///
    /// Cho phep chay <see cref="PlayableSceneBuilder.BuildAll"/> tu dong dong lenh: ghi mot request id
    /// vao .unity-agent/playable-build-request.json, bridge se chay builder roi ghi ket qua
    /// (kem log warning/error) vao .unity-agent/playable-build-result.json.
    ///
    /// Khong lien quan gi den runtime cua playable - chi la ha tang de agent tu kiem chung.
    /// </summary>
    [InitializeOnLoad]
    internal static class PlayableAgentBridge
    {
        private const string RuntimeDirectoryName = ".unity-agent";
        private const string RequestFileName = "playable-build-request.json";
        private const string ResultFileName = "playable-build-result.json";
        private const double PollIntervalSeconds = 0.25d;

        private static double s_nextPollAt;
        private static string s_handledRequestId;

        static PlayableAgentBridge()
        {
            EditorApplication.update += Poll;
        }

        private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        private static string RequestPath =>
            Path.Combine(ProjectRoot, RuntimeDirectoryName, RequestFileName);

        private static string ResultPath =>
            Path.Combine(ProjectRoot, RuntimeDirectoryName, ResultFileName);

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < s_nextPollAt)
            {
                return;
            }

            s_nextPollAt = EditorApplication.timeSinceStartup + PollIntervalSeconds;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(RequestPath))
            {
                return;
            }

            string raw;
            try
            {
                raw = File.ReadAllText(RequestPath).Trim();
            }
            catch (IOException)
            {
                return;
            }

            // Dinh dang request: "<id>" hoac "<id>|<command>". Command mac dinh la "build".
            string requestId = raw;
            string command = "build";
            int separator = raw.IndexOf('|');
            if (separator >= 0)
            {
                requestId = raw[..separator].Trim();
                command = raw[(separator + 1)..].Trim();
            }

            if (string.IsNullOrEmpty(requestId) || requestId == s_handledRequestId)
            {
                return;
            }

            if (File.Exists(ResultPath) && File.ReadAllText(ResultPath).Contains(requestId))
            {
                s_handledRequestId = requestId;
                return;
            }

            s_handledRequestId = requestId;
            Run(requestId, command);
        }

        private static void Run(string requestId, string command)
        {
            var logs = new List<string>();

            void Capture(string condition, string stackTrace, LogType type)
            {
                if (type is LogType.Warning or LogType.Error or LogType.Exception or LogType.Assert)
                {
                    logs.Add($"{type}: {condition}");
                }
            }

            Application.logMessageReceived += Capture;

            bool ok = true;
            string failure = string.Empty;

            try
            {
                switch (command)
                {
                    case "optimize":
                        PlayableTextureOptimizer.Optimize();
                        break;
                    case "validate":
                        PlayableSceneBuilder.ValidateWiring();
                        break;
                    default:
                        PlayableSceneBuilder.BuildAll();
                        break;
                }
            }
            catch (Exception exception)
            {
                ok = false;
                failure = exception.ToString();
            }
            finally
            {
                Application.logMessageReceived -= Capture;
            }

            var builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine($"  \"requestId\": \"{requestId}\",");
            builder.AppendLine($"  \"ok\": {(ok ? "true" : "false")},");
            builder.AppendLine($"  \"logCount\": {logs.Count},");
            builder.AppendLine($"  \"failure\": \"{Escape(failure)}\",");
            builder.AppendLine("  \"logs\": [");
            for (int i = 0; i < logs.Count; i++)
            {
                string comma = i == logs.Count - 1 ? string.Empty : ",";
                builder.AppendLine($"    \"{Escape(logs[i])}\"{comma}");
            }

            builder.AppendLine("  ]");
            builder.AppendLine("}");

            Directory.CreateDirectory(Path.GetDirectoryName(ResultPath)!);
            File.WriteAllText(ResultPath, builder.ToString());
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("\t", " ");
        }
    }
}
