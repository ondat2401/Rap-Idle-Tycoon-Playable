using UnityEngine;
using UnityEditor;
using Amanotes.Core;

namespace Amanotes.Core.Editor
{
    [CustomEditor(typeof(GUIBase), true)]
    public class UIScreenBaseEditor : UnityEditor.Editor
    {
        private SerializedProperty screenIdProp;

        private void OnEnable()
        {
            screenIdProp = serializedObject.FindProperty("screenId");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("UI Screen Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(screenIdProp, new GUIContent("Screen ID"));

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Auto Generate ID", GUILayout.Width(150)))
            {
                screenIdProp.stringValue = target.GetType().Name.Replace("Screen", "");
                serializedObject.ApplyModifiedProperties();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            if (Application.isPlaying)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Show"))
                    ((GUIBase)target).Show();
                if (GUILayout.Button("Hide"))
                    ((GUIBase)target).Hide();
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to test Show/Hide", MessageType.Info);
            }

            EditorGUILayout.Space(10);
            DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
