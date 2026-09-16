using UnityEngine;
using UnityEditor;
using OrientationTracking.Utils;
using OrientationTracking.Core;

namespace OrientationTracking.Editor
{
    [CustomEditor(typeof(OrientationTrackerInitializer))]
    public class OrientationTrackerEditor : UnityEditor.Editor
    {
        private SerializedProperty _checkInterval;
        private SerializedProperty _logChanges;

        private void OnEnable()
        {
            _checkInterval = serializedObject.FindProperty("_checkInterval");
            _logChanges = serializedObject.FindProperty("_logChanges");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Orientation Tracker", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(_checkInterval,
                new GUIContent("Check Interval (s)", "How often to poll for orientation changes"));

            if (_checkInterval.floatValue < 0.1f)
            {
                EditorGUILayout.HelpBox("Very low interval may affect performance.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(_logChanges,
                new GUIContent("Log Changes", "Log orientation changes to console"));

            EditorGUILayout.Space(10);

            // Live status
            if (Application.isPlaying && OrientationTracker.IsInitialized)
            {
                var tracker = OrientationTracker.Instance;
                EditorGUILayout.LabelField("Live Status", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(string.Format("Device: {0}", tracker.IsTablet ? "Tablet" : "Phone"));
                EditorGUILayout.LabelField(string.Format("Orientation: {0}", tracker.CurrentOrientation));
                EditorGUILayout.LabelField(string.Format("Screen: {0}x{1}", Screen.width, Screen.height));
                EditorGUILayout.EndVertical();

                if (GUILayout.Button("Force Refresh"))
                {
                    tracker.RefreshState();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see live status.", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
