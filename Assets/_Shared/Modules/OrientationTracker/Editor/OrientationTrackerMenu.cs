using UnityEngine;
using UnityEditor;
using OrientationTracking.Core;
using OrientationTracking.Utils;

namespace OrientationTracking.Editor
{
    public static class OrientationTrackerMenuItems
    {
        private const string MENU_ROOT = "Tools/Orientation Tracker/";

        [MenuItem(MENU_ROOT + "Log Current State", false, 1)]
        public static void LogCurrentState()
        {
            if (!Application.isPlaying || !OrientationTracker.IsInitialized)
            {
                Debug.LogWarning("[OrientationTracker] Only works in Play Mode when initialized.");
                return;
            }

            Debug.Log("[OrientationTracker] " + OrientationTracker.Instance.GetDescription());
        }

        [MenuItem(MENU_ROOT + "Log Current State", true)]
        public static bool ValidateLogCurrentState()
        {
            return Application.isPlaying && OrientationTracker.IsInitialized;
        }

        [MenuItem(MENU_ROOT + "Force Refresh", false, 2)]
        public static void ForceRefresh()
        {
            if (!Application.isPlaying || !OrientationTracker.IsInitialized)
            {
                Debug.LogWarning("[OrientationTracker] Only works in Play Mode when initialized.");
                return;
            }

            OrientationTracker.Instance.RefreshState();
            Debug.Log("[OrientationTracker] Refreshed: " + OrientationTracker.Instance.GetDescription());
        }

        [MenuItem(MENU_ROOT + "Force Refresh", true)]
        public static bool ValidateForceRefresh()
        {
            return Application.isPlaying && OrientationTracker.IsInitialized;
        }

        [MenuItem(MENU_ROOT + "Create Initializer GameObject", false, 100)]
        public static void CreateInitializerGameObject()
        {
            GameObject go = new GameObject("OrientationTrackerInitializer");
            go.AddComponent<OrientationTrackerInitializer>();
            Selection.activeGameObject = go;
            Debug.Log("[OrientationTracker] Created Initializer GameObject.");
        }
    }
}
