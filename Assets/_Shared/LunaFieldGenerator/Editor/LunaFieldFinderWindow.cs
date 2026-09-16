using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Amanotes.Core;
using Amanotes.LunaFieldGenerator;

namespace Amanotes.LunaFieldGenerator.Editor
{
    /// <summary>
    /// EditorWindow that scans the scene for FieldBase components not yet registered
    /// in a LunaFieldController's fields array. Allows checkbox selection and batch add.
    /// </summary>
    public class LunaFieldFinderWindow : EditorWindow
    {
        private LunaFieldController targetController;
        private Vector2 scrollPosition;
        private List<FieldBaseEntry> foundEntries = new List<FieldBaseEntry>();
        private bool selectAll;

        private class FieldBaseEntry
        {
            public FieldBase field;
            public bool selected;
        }

        public static void Open(LunaFieldController controller)
        {
            var window = GetWindow<LunaFieldFinderWindow>("Field Finder");
            window.targetController = controller;
            window.minSize = new Vector2(400, 300);
            window.Scan();
            window.Show();
        }

        private void OnGUI()
        {
            if (targetController == null)
            {
                EditorGUILayout.HelpBox("No LunaFieldController selected.", MessageType.Warning);
                if (GUILayout.Button("Find in Scene"))
                {
                    targetController = FindObjectOfType<LunaFieldController>();
                    if (targetController != null) Scan();
                }
                return;
            }

            // Header
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Field Finder", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Controller: " + targetController.name, EditorStyles.miniLabel);
            EditorGUILayout.Space(5);

            // Toolbar
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Rescan", GUILayout.Height(28)))
                Scan();

            int selectedCount = foundEntries.Count(e => e.selected);
            EditorGUI.BeginDisabledGroup(selectedCount == 0);
            GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
            if (GUILayout.Button("Add Selected (" + selectedCount + ")", GUILayout.Height(28)))
                AddSelectedFields();
            GUI.backgroundColor = Color.white;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);

            if (foundEntries.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No unregistered FieldBase found in scene.\n" +
                    "All fields are already registered or no FieldBase components exist.",
                    MessageType.Info);
                return;
            }

            // Select All toggle
            EditorGUILayout.BeginHorizontal("box");
            EditorGUI.BeginChangeCheck();
            selectAll = EditorGUILayout.ToggleLeft(
                "Select All (" + foundEntries.Count + " unregistered fields)",
                selectAll, EditorStyles.boldLabel);
            if (EditorGUI.EndChangeCheck())
            {
                for (int i = 0; i < foundEntries.Count; i++)
                    foundEntries[i].selected = selectAll;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            // List
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < foundEntries.Count; i++)
            {
                var entry = foundEntries[i];
                if (entry.field == null) continue;

                EditorGUILayout.BeginHorizontal("helpbox");

                entry.selected = EditorGUILayout.Toggle(entry.selected, GUILayout.Width(20));

                // Type name
                EditorGUILayout.LabelField(entry.field.GetType().Name, EditorStyles.boldLabel, GUILayout.Width(220));

                // GameObject name + path
                string path = GetHierarchyPath(entry.field.transform);
                EditorGUILayout.LabelField(path, EditorStyles.miniLabel);

                // Ping button
                if (GUILayout.Button("", GUILayout.Width(28), GUILayout.Height(18)))
                {
                    Selection.activeGameObject = entry.field.gameObject;
                    EditorGUIUtility.PingObject(entry.field.gameObject);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void Scan()
        {
            foundEntries.Clear();
            selectAll = false;

            if (targetController == null) return;

            // Build set of already-registered fields
            var registeredSet = new HashSet<FieldBase>();
            if (targetController.fields != null)
            {
                for (int i = 0; i < targetController.fields.Length; i++)
                {
                    if (targetController.fields[i] != null)
                        registeredSet.Add(targetController.fields[i]);
                }
            }

            // Find all FieldBase in scene
            FieldBase[] allFields = FindObjectsOfType<FieldBase>(true);
            for (int i = 0; i < allFields.Length; i++)
            {
                if (!registeredSet.Contains(allFields[i]))
                {
                    foundEntries.Add(new FieldBaseEntry
                    {
                        field = allFields[i],
                        selected = false
                    });
                }
            }

            // Sort by type name
            foundEntries.Sort((a, b) => string.Compare(a.field.GetType().Name, b.field.GetType().Name));
        }

        private void AddSelectedFields()
        {
            if (targetController == null) return;

            var toAdd = new List<FieldBase>();
            for (int i = 0; i < foundEntries.Count; i++)
            {
                if (foundEntries[i].selected && foundEntries[i].field != null)
                    toAdd.Add(foundEntries[i].field);
            }

            if (toAdd.Count == 0) return;

            Undo.RecordObject(targetController, "Add Fields from Finder");
            var list = new List<FieldBase>(targetController.fields ?? new FieldBase[0]);
            list.AddRange(toAdd);
            targetController.fields = list.ToArray();
            EditorUtility.SetDirty(targetController);

            Debug.Log("[LunaFieldFinder] Added " + toAdd.Count + " field(s) to " + targetController.name);

            // Re-scan to refresh the list
            Scan();
        }

        private string GetHierarchyPath(Transform t)
        {
            string path = t.name;
            Transform current = t.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }
    }
}
