using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using Amanotes.Core;
using Amanotes.LunaFieldGenerator;

namespace Amanotes.LunaFieldGenerator.Editor
{
    public partial class LunaFieldControllerEditor
    {
        #region Main Tab

        private void DrawMainTab(LunaFieldController controller)
        {
            DrawStatusSection(controller);
            EditorGUILayout.Space(10);
            DrawActionsSection(controller);
            EditorGUILayout.Space(10);
            DrawFieldListSection(controller);
        }

        #endregion

        #region Status Section

        private void DrawStatusSection(LunaFieldController controller)
        {
            DrawSectionHeader(ref showStatus, "Status Overview", Theme.Info);
            if (!showStatus) return;

            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical("helpbox");

            int currentCount = controller.FieldCount;
            int availableCount = availableFieldTypes.Count;
            int assignedTransforms = CountAssignedTransforms(controller);

            EditorGUILayout.BeginHorizontal();
            DrawStatBox("Current Fields", currentCount.ToString(), Theme.Primary);
            DrawStatBox("Available Types", availableCount.ToString(), Theme.Info);
            DrawStatBox("Transforms", $"{assignedTransforms}/{currentCount}", Theme.Success);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            if (availableCount == 0)
                EditorGUILayout.HelpBox("No FieldBase types found! Check your field path.", MessageType.Warning);
            else if (currentCount == 0)
                EditorGUILayout.HelpBox("Use 'Create' tab or drag fields below to get started.", MessageType.Info);
            else if (currentCount < availableCount)
                EditorGUILayout.HelpBox($"{availableCount - currentCount} field type(s) available to add.", MessageType.Info);
            else
                EditorGUILayout.HelpBox("All field types are added!", MessageType.Info);

            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);
        }

        #endregion

        #region Actions Section

        private void DrawActionsSection(LunaFieldController controller)
        {
            DrawSectionHeader(ref showActions, "Quick Actions", Theme.Success);
            if (!showActions) return;

            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical("box");

            // Utilities
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Field Folder", GUILayout.Height(30)))
                OpenFolderInProject(customFieldPath);
            if (GUILayout.Button("Dummy Folder", GUILayout.Height(30)))
                OpenFolderInProject(dummyFolderPath);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = new Color(0.6f, 0.85f, 1f);
            if (GUILayout.Button("Validate Dummy (fill empty)", GUILayout.Height(30)))
                ValidateDummiesNoneOnly(controller);
            GUI.backgroundColor = new Color(1f, 0.6f, 0.3f);
            if (GUILayout.Button("Force Validate Dummy", GUILayout.Height(30)))
                ValidateDummiesForce(controller);
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            GUI.backgroundColor = new Color(0.9f, 0.5f, 0.9f);
            if (GUILayout.Button("Find Unregistered Fields in Scene", GUILayout.Height(35)))
                LunaFieldFinderWindow.Open(controller);
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);
        }

        #endregion

        #region Field List Section

        private void DrawFieldListSection(LunaFieldController controller)
        {
            // Auto-clean destroyed/missing field references
            int removed = controller.RemoveNullFields();
            if (removed > 0)
            {
                EditorUtility.SetDirty(controller);
                fieldFoldouts.Clear();
            }

            int fieldCount = controller.FieldCount;
            string title = fieldCount > 0 ? $"Field List ({fieldCount})" : "Field List (Empty)";

            DrawSectionHeader(ref showFieldList, title, Theme.Primary);
            if (!showFieldList) return;

            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical("box");

            if (fieldCount == 0)
            {
                EditorGUILayout.HelpBox("No fields added yet. Use 'Create' tab or drag below.", MessageType.Info);
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
                DrawFieldDropZone(controller);
                return;
            }

            // Toolbar
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(expandAllFields ? "Collapse All" : "Expand All", GUILayout.Width(120)))
            {
                expandAllFields = !expandAllFields;
                for (int i = 0; i < fieldCount; i++)
                    fieldFoldouts[i] = expandAllFields;
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Clear All", GUILayout.Width(100)))
            {
                if (EditorUtility.DisplayDialog("Clear All Fields?", "Remove all fields from the list?", "Yes", "Cancel"))
                {
                    Undo.RecordObject(controller, "Clear All Fields");
                    controller.fields = new FieldBase[0];
                    fieldFoldouts.Clear();
                    EditorUtility.SetDirty(controller);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Build folder → index mapping
            FieldBase[] fields = controller.fields;
            var folderGroups = new Dictionary<string, List<int>>(16);
            var folderOrder = new List<string>(16);

            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i] == null) continue;
                string folder = FieldTypeScanner.GetTypeFolder(fields[i].GetType(), customFieldPath);
                if (!folderGroups.TryGetValue(folder, out var list))
                {
                    list = new List<int>(8);
                    folderGroups[folder] = list;
                    folderOrder.Add(folder);
                }
                list.Add(i);
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MaxHeight(400));

            foreach (string folder in folderOrder)
            {
                GUIStyle folderStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = Theme.Info },
                    fontSize = 11
                };
                GUI.backgroundColor = new Color(0.2f, 0.3f, 0.45f);
                EditorGUILayout.BeginVertical("box");
                GUI.backgroundColor = Color.white;
                EditorGUILayout.LabelField($"{folder}  ({folderGroups[folder].Count})", folderStyle);
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(2);

                List<int> indices = folderGroups[folder];
                for (int j = 0; j < indices.Count; j++)
                {
                    DrawFieldItem(controller, indices[j]);
                    EditorGUILayout.Space(3);
                }

                EditorGUILayout.Space(5);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            DrawFieldDropZone(controller);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);
        }

        private void DrawFieldItem(LunaFieldController controller, int index)
        {
            FieldBase field = controller.fields[index];
            Type fieldType = field.GetType();

            if (!fieldFoldouts.TryGetValue(index, out bool foldout))
                foldout = false;

            Color bgColor = EditorGUIUtility.isProSkin ? Theme.Dark : Theme.Light;
            GUI.backgroundColor = bgColor;
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = Color.white;

            EditorGUILayout.BeginHorizontal();
            fieldFoldouts[index] = EditorGUILayout.Foldout(foldout, "", true);
            EditorGUILayout.LabelField($"{index + 1}. {fieldType.Name}", EditorStyles.boldLabel);

            string lunaName = GetLunaNameForType(fieldType);
            if (!string.IsNullOrEmpty(lunaName))
            {
                GUIStyle lunaStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = Theme.Warning },
                    fontStyle = FontStyle.Bold
                };
                EditorGUILayout.LabelField($"[{lunaName}]", lunaStyle, GUILayout.Width(120));
            }

            GUILayout.FlexibleSpace();

            bool isLinked = field.transform != null;
            GUIStyle iconStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = isLinked ? Theme.Success : Theme.Warning }
            };
            EditorGUILayout.LabelField(isLinked ? "[v] LINKED" : "MISSING", iconStyle, GUILayout.Width(80));

            GUI.backgroundColor = Theme.Danger;
            if (GUILayout.Button("", GUILayout.Width(30), GUILayout.Height(20)))
            {
                if (EditorUtility.DisplayDialog("Remove Field?", $"Remove '{fieldType.Name}'?", "Yes", "Cancel"))
                {
                    Undo.RecordObject(controller, "Remove Field");
                    controller.RemoveFieldAt(index);
                    fieldFoldouts.Clear();
                    EditorUtility.SetDirty(controller);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    return;
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            if (fieldFoldouts[index])
            {
                EditorGUI.indentLevel++;
                DrawFieldProperties(field);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFieldProperties(FieldBase field)
        {
            EditorGUILayout.Space(5);

            SerializedObject serializedField = new SerializedObject(field);
            serializedField.Update();

            SerializedProperty prop = serializedField.GetIterator();
            bool enterChildren = true;

            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (prop.propertyPath == "m_Script") continue;
                EditorGUILayout.PropertyField(prop, true);
            }

            if (serializedField.ApplyModifiedProperties())
                EditorUtility.SetDirty(field);

            EditorGUILayout.Space(5);
        }

        #endregion

        #region Drag & Drop

        private void DrawFieldDropZone(LunaFieldController controller)
        {
            EditorGUILayout.Space(6);

            GUIStyle dropStyle = new GUIStyle(EditorStyles.helpBox)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(0.6f, 0.8f, 1f) }
            };

            Rect dropRect = GUILayoutUtility.GetRect(0, 36, GUILayout.ExpandWidth(true));
            bool isHovering = dropRect.Contains(Event.current.mousePosition);
            Color zoneBg = isHovering ? new Color(0.2f, 0.45f, 0.25f) : new Color(0.18f, 0.28f, 0.38f);

            EditorGUI.DrawRect(dropRect, zoneBg);
            GUI.Label(dropRect, "+ Drag FieldBase script or GameObject here to add", dropStyle);

            EventType evtType = Event.current.type;
            if ((evtType == EventType.DragUpdated || evtType == EventType.DragPerform) && isHovering)
            {
                bool valid = IsDragValid();
                DragAndDrop.visualMode = valid ? DragAndDropVisualMode.Link : DragAndDropVisualMode.Rejected;

                if (evtType == EventType.DragPerform && valid)
                {
                    DragAndDrop.AcceptDrag();
                    HandleFieldDrop(controller);
                    Event.current.Use();
                }
                else if (evtType == EventType.DragUpdated)
                {
                    Event.current.Use();
                }
            }
        }

        private bool IsDragValid()
        {
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is MonoScript ms)
                {
                    Type t = ms.GetClass();
                    if (t != null && t.IsSubclassOf(typeof(FieldBase)) && !t.IsAbstract)
                        return true;
                }
                else if (obj is GameObject go)
                {
                    if (go.GetComponent<FieldBase>() != null)
                        return true;
                }
            }
            return false;
        }

        private void HandleFieldDrop(LunaFieldController controller)
        {
            int added = 0;
            foreach (var obj in DragAndDrop.objectReferences)
            {
                if (obj is MonoScript ms)
                {
                    Type t = ms.GetClass();
                    if (t == null || !t.IsSubclassOf(typeof(FieldBase)) || t.IsAbstract) continue;

                    FieldBase existing = FindObjectOfType(t) as FieldBase;
                    if (existing != null)
                    {
                        Undo.RecordObject(controller, "Add Field via Drag");
                        if (controller.AddField(existing)) added++;
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Not Found",
                            $"No GameObject with '{t.Name}' found in the scene.\nCreate one first.", "OK");
                    }
                }
                else if (obj is GameObject go)
                {
                    FieldBase[] components = go.GetComponents<FieldBase>();
                    foreach (var comp in components)
                    {
                        Undo.RecordObject(controller, "Add Field via Drag");
                        if (controller.AddField(comp)) added++;
                    }
                }
            }

            if (added > 0)
                EditorUtility.SetDirty(controller);
        }

        #endregion
    }
}
