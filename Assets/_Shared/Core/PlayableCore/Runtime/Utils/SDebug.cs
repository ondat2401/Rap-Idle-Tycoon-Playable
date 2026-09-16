using UnityEngine;

namespace Amanotes.Core
{
    public static class SDebug
    {
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Log(object message)
        {
            Debug.Log(message);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogWarning(object message)
        {
            Debug.LogWarning(message);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogError(object message)
        {
            Debug.LogError(message);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogColored(string message, string color = "cyan")
        {
            Debug.Log($"<color={color}>{message}</color>");
        }
    }
}
