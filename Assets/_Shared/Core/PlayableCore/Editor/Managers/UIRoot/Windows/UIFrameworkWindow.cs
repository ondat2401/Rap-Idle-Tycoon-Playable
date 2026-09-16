using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using Amanotes.Core;

namespace Amanotes.Core.Editor
{
    public class UIFrameworkWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private GUIManager guiManager;
        private List<GUIBase> allScreens = new List<GUIBase>();
        private string searchFilter = "";

        [MenuItem("Tools/Playable Standard Pipeline/UI Framework/UI Manager Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<UIFrameworkWindow>("UI Framework");
            window.minSize = new Vector2(400, 300);
        }

        private void OnEnable()
        {
            RefreshScreenList();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawToolbar();
            EditorGUILayout.Space(5);
            DrawScreenList();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("UI Framework Manager", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Manage and test UI screens", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(80)))
                RefreshScreenList();

            if (GUILayout.Button("Create New Screen", EditorStyles.toolbarButton, GUILayout.Width(130)))
                UIScreenCreationWizard.ShowWindow();

            GUILayout.FlexibleSpace();
            searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(200));

            EditorGUILayout.EndHorizontal();
        }

        private void DrawScreenList()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (allScreens.Count == 0)
            {
                EditorGUILayout.HelpBox("No UI screens found in the scene", MessageType.Info);
            }
            else
            {
                var filteredScreens = string.IsNullOrEmpty(searchFilter)
                    ? allScreens
                    : allScreens.Where(s => s.ScreenId.ToLower().Contains(searchFilter.ToLower())).ToList();

                foreach (var screen in filteredScreens)
                    DrawScreenItem(screen);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawScreenItem(GUIBase screen)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(screen.ScreenId, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Type: {screen.GetType().Name}", EditorStyles.miniLabel);

            if (Application.isPlaying)
                EditorGUILayout.LabelField($"State: {screen.State}", EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginVertical();
            if (GUILayout.Button("Select", GUILayout.Width(80)))
            {
                Selection.activeGameObject = screen.gameObject;
                EditorGUIUtility.PingObject(screen.gameObject);
            }

            if (Application.isPlaying)
            {
                if (GUILayout.Button("Show", GUILayout.Width(80)))
                    screen.Show();
                if (GUILayout.Button("Hide", GUILayout.Width(80)))
                    screen.Hide();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void RefreshScreenList()
        {
            allScreens = FindObjectsOfType<GUIBase>().ToList();
            guiManager = FindObjectOfType<GUIManager>();
        }
    }
}
