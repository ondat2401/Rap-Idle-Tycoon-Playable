using UnityEditor;
using UnityEngine;
using OrientationTracking.Core;
using System;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using Amanotes.Core;
using Amanotes.LunaFieldGenerator;

namespace Amanotes.LunaFieldGenerator.Editor
{
    /// <summary>
    /// Custom editor for all FieldBase subclasses.
    /// Draws default inspector + Capture tool + Add/Remove property groups.
    /// </summary>
    [CustomEditor(typeof(FieldBase), true)]
    public class FieldBaseEditor : UnityEditor.Editor
    {
        #region Theme & State

        private static readonly Color ColorPrimary = new Color(0.3f, 0.6f, 0.9f);
        private static readonly Color ColorSuccess = new Color(0.3f, 0.8f, 0.4f);
        private static readonly Color ColorWarning = new Color(0.9f, 0.7f, 0.2f);
        private static readonly Color ColorDanger = new Color(0.9f, 0.3f, 0.3f);

        // Cached property detection
        private bool hasPosition;
        private bool hasScale;
        private bool hasRotation;
        private bool hasTexture;
        private bool propertiesScanned;

        // Priority editing state
        private PriorityEntry[] priorityEntries;
        private bool priorityScanned;
        private bool priorityDirty;

        // Foldout states
        private bool foldoutPriority = false;
        private bool foldoutCapture = false;
        private bool foldoutDummy = false;
        private bool foldoutProperties = false;

        private class PriorityEntry
        {
            public string fieldName;
            public int originalPriority;
            public int currentPriority;
            public bool isAsset; // LunaPlaygroundAsset vs LunaPlaygroundField
        }

        #endregion

        private void OnEnable()
        {
            propertiesScanned = false;
            priorityScanned = false;
            priorityDirty = false;
            priorityEntries = null;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            FieldBase field = (FieldBase)target;
            if (field == null) return;

            if (!propertiesScanned) ScanFieldProperties(field);

            EditorGUILayout.Space(10);
            DrawPrioritySection(field);
            EditorGUILayout.Space(10);
            DrawCaptureSection(field);
            EditorGUILayout.Space(10);
            DrawPropertyManagementSection(field);
        }

        #region Property Detection

        private void ScanFieldProperties(FieldBase field)
        {
            // Prefer virtual metadata if overridden
            hasPosition = field.HasPosition;
            hasScale = field.HasScale;
            hasRotation = field.HasRotation;
            hasTexture = field.HasTexture;

            // Fallback: reflection for legacy fields without overrides
            if (!hasPosition && !hasScale && !hasRotation && !hasTexture)
            {
                Type t = field.GetType();
                BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
                hasPosition = t.GetField("phone_Position_PT", flags) != null;
                hasScale = t.GetField("phone_Scale_PT", flags) != null;
                hasRotation = t.GetField("phone_Rotation_PT", flags) != null;
                hasTexture = t.GetField("tex", flags) != null;
            }

            propertiesScanned = true;
        }

        #endregion

        #region Priority Section

        private static readonly Regex PriorityAttrRegex = new Regex(
            @"\[LunaPlayground(Asset|Field)\s*\(\s*""([^""]*)""\s*,\s*(\d+)",
            RegexOptions.Compiled);

        private void DrawPrioritySection(FieldBase field)
        {
            string scriptPath = FindScriptPath(field.GetType());
            if (string.IsNullOrEmpty(scriptPath)) return;

            if (!priorityScanned)
                ScanPriorities(scriptPath);

            if (priorityEntries == null || priorityEntries.Length == 0) return;

            GUI.backgroundColor = new Color(0.3f, 0.3f, 0.45f);
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = Color.white;

            string dirtyMark = priorityDirty ? " *" : "";
            foldoutPriority = EditorGUILayout.Foldout(foldoutPriority, $"Field Priorities ({priorityEntries.Length}){dirtyMark}", true, EditorStyles.foldoutHeader);

            if (foldoutPriority)
            {
                EditorGUILayout.Space(3);

                for (int i = 0; i < priorityEntries.Length; i++)
                {
                    PriorityEntry entry = priorityEntries[i];
                    EditorGUILayout.BeginHorizontal();

                    string typeLabel = entry.isAsset ? "[Asset]" : "[Field]";
                    GUIStyle typeStyle = new GUIStyle(EditorStyles.miniLabel)
                    {
                        normal = { textColor = entry.isAsset ? ColorWarning : ColorPrimary }
                    };
                    EditorGUILayout.LabelField(typeLabel, typeStyle, GUILayout.Width(45));
                    EditorGUILayout.LabelField(entry.fieldName, GUILayout.Width(180));

                    EditorGUI.BeginChangeCheck();
                    entry.currentPriority = EditorGUILayout.IntField(entry.currentPriority, GUILayout.Width(60));
                    if (EditorGUI.EndChangeCheck())
                        UpdatePriorityDirtyState();

                    if (entry.currentPriority != entry.originalPriority)
                    {
                        GUIStyle changeStyle = new GUIStyle(EditorStyles.miniLabel)
                        {
                            normal = { textColor = ColorWarning }
                        };
                        EditorGUILayout.LabelField($"(was {entry.originalPriority})", changeStyle, GUILayout.Width(70));
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(5);

                EditorGUI.BeginDisabledGroup(!priorityDirty);
                GUI.backgroundColor = priorityDirty ? ColorSuccess : new Color(0.4f, 0.4f, 0.4f);
                if (GUILayout.Button("Apply Priority Changes", GUILayout.Height(28)))
                    ApplyPriorityChanges(scriptPath);
                GUI.backgroundColor = Color.white;
                EditorGUI.EndDisabledGroup();
            }

            EditorGUILayout.EndVertical();
        }

        private void ScanPriorities(string scriptPath)
        {
            priorityScanned = true;
            priorityDirty = false;

            if (!File.Exists(scriptPath))
            {
                priorityEntries = new PriorityEntry[0];
                return;
            }

            string content = File.ReadAllText(scriptPath);
            MatchCollection matches = PriorityAttrRegex.Matches(content);

            priorityEntries = new PriorityEntry[matches.Count];
            for (int i = 0; i < matches.Count; i++)
            {
                Match m = matches[i];
                priorityEntries[i] = new PriorityEntry
                {
                    isAsset = m.Groups[1].Value == "Asset",
                    fieldName = m.Groups[2].Value,
                    originalPriority = int.Parse(m.Groups[3].Value),
                    currentPriority = int.Parse(m.Groups[3].Value)
                };
            }
        }

        private void UpdatePriorityDirtyState()
        {
            priorityDirty = false;
            if (priorityEntries == null) return;
            for (int i = 0; i < priorityEntries.Length; i++)
            {
                if (priorityEntries[i].currentPriority != priorityEntries[i].originalPriority)
                {
                    priorityDirty = true;
                    return;
                }
            }
        }

        private void ApplyPriorityChanges(string scriptPath)
        {
            if (!File.Exists(scriptPath)) return;

            string content = File.ReadAllText(scriptPath);
            int entryIndex = 0;

            content = PriorityAttrRegex.Replace(content, match =>
            {
                if (entryIndex >= priorityEntries.Length) return match.Value;

                PriorityEntry entry = priorityEntries[entryIndex];
                entryIndex++;

                string attrType = match.Groups[1].Value;
                string fieldName = match.Groups[2].Value;

                return $"[LunaPlayground{attrType}(\"{fieldName}\", {entry.currentPriority}";
            });

            File.WriteAllText(scriptPath, content);
            AssetDatabase.Refresh();

            // Update original values
            for (int i = 0; i < priorityEntries.Length; i++)
                priorityEntries[i].originalPriority = priorityEntries[i].currentPriority;

            priorityDirty = false;
            SDebug.Log($"[FieldBaseEditor] Priority changes applied to {Path.GetFileName(scriptPath)}");
        }

        #endregion

        #region Capture Section

        private void DrawCaptureSection(FieldBase field)
        {
            if (!hasPosition && !hasScale && !hasRotation) return;

            GUI.backgroundColor = ColorPrimary;
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = Color.white;

            foldoutCapture = EditorGUILayout.Foldout(foldoutCapture, "Capture Tool", true, EditorStyles.foldoutHeader);

            if (foldoutCapture)
            {
                EditorGUILayout.Space(3);

            DrawOrientationInfo();
            EditorGUILayout.Space(5);

            bool isPhone = OrientationHelper.IsPhone();
            bool isPortrait = OrientationHelper.IsPortrait();
            string slotLabel = OrientationHelper.GetSlotLabel(isPhone, isPortrait);

            // Show current transform values
            Transform t = field.targetTransform;
            if (t != null)
            {
                var sb = new System.Text.StringBuilder("Current → ");
                if (hasPosition)
                    sb.Append($"Pos({t.localPosition.x:F1}, {t.localPosition.y:F1})  ");
                if (hasScale)
                    sb.Append($"Scale({t.localScale.x:F2}, {t.localScale.y:F2})  ");
                if (hasRotation)
                    sb.Append($"Rot({t.localEulerAngles.z:F1}°)");
                EditorGUILayout.LabelField(sb.ToString(), EditorStyles.miniLabel);
            }

            DrawStoredSlotValues(field, isPhone, isPortrait);
            EditorGUILayout.Space(5);

            GUI.backgroundColor = ColorSuccess;
            if (GUILayout.Button($"Capture → {slotLabel}", GUILayout.Height(35)))
                CaptureCurrentValues(field, isPhone, isPortrait);
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(3);

            GUI.backgroundColor = ColorWarning;
            if (GUILayout.Button("Capture → All 4 Slots", GUILayout.Height(28)))
            {
                if (EditorUtility.DisplayDialog("Capture All Slots",
                    "Overwrite all 4 slots with current transform values?", "Yes", "Cancel"))
                    CaptureAllSlots(field);
            }
            GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawOrientationInfo()
        {
            bool isPhone = OrientationHelper.IsPhone();
            bool isPortrait = OrientationHelper.IsPortrait();
            Vector2 size = OrientationHelper.GetGameViewSize();
            bool trackerReady = OrientationHelper.IsTrackerReady;

            EditorGUILayout.BeginHorizontal();
            DrawMiniLabel("Game View", $"{size.x:F0}x{size.y:F0}", ColorPrimary);
            string deviceStr = isPhone ? "Phone" : "Tablet";
            DrawMiniLabel("Device", deviceStr + (trackerReady ? "" : " (override)"), isPhone ? ColorPrimary : ColorWarning);
            string orientStr = isPortrait ? "Portrait" : "Landscape";
            DrawMiniLabel("Orientation", orientStr, isPortrait ? ColorPrimary : ColorSuccess);
            EditorGUILayout.EndHorizontal();

            if (!trackerReady)
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.BeginHorizontal("helpbox");
                EditorGUILayout.LabelField("Device Override:", GUILayout.Width(105));
                bool editorIsPhone = OrientationHelper.EditorDeviceIsPhone;
                EditorGUI.BeginChangeCheck();
                editorIsPhone = EditorGUILayout.ToggleLeft("Phone", editorIsPhone, GUILayout.Width(60));
                bool isTablet = EditorGUILayout.ToggleLeft("Tablet", !editorIsPhone, GUILayout.Width(60));
                if (EditorGUI.EndChangeCheck())
                {
                    OrientationHelper.EditorDeviceIsPhone = isTablet ? false : editorIsPhone;
                    Repaint();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(3);
            GUI.backgroundColor = new Color(0.4f, 0.4f, 0.5f);
            if (GUILayout.Button("Refresh Orientation", GUILayout.Height(22)))
            {
                OrientationHelper.RefreshTracker();
                Repaint();
            }
            GUI.backgroundColor = Color.white;
        }

        private void DrawStoredSlotValues(FieldBase field, bool isPhone, bool isPortrait)
        {
            Type fieldType = field.GetType();
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            var sb = new System.Text.StringBuilder("Stored  → ");
            bool hasAny = false;

            if (hasPosition)
            {
                FieldInfo posField = fieldType.GetField(OrientationHelper.GetPositionFieldName(isPhone, isPortrait), flags);
                if (posField != null)
                {
                    Vector2 v = (Vector2)posField.GetValue(field);
                    sb.Append($"Pos({v.x:F1}, {v.y:F1})  ");
                    hasAny = true;
                }
            }
            if (hasScale)
            {
                FieldInfo scaleField = fieldType.GetField(OrientationHelper.GetScaleFieldName(isPhone, isPortrait), flags);
                if (scaleField != null)
                {
                    Vector2 v = (Vector2)scaleField.GetValue(field);
                    sb.Append($"Scale({v.x:F2}, {v.y:F2})  ");
                    hasAny = true;
                }
            }
            if (hasRotation)
            {
                FieldInfo rotField = fieldType.GetField(OrientationHelper.GetRotationFieldName(isPhone, isPortrait), flags);
                if (rotField != null)
                {
                    float v = (float)rotField.GetValue(field);
                    sb.Append($"Rot({v:F1}°)");
                    hasAny = true;
                }
            }

            if (hasAny)
            {
                GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = new Color(0.5f, 0.8f, 0.5f) }
                };
                EditorGUILayout.LabelField(sb.ToString(), style);
            }
        }

        #endregion

        #region Capture Logic

        private void CaptureCurrentValues(FieldBase field, bool isPhone, bool isPortrait)
        {
            Transform t = field.targetTransform;
            if (t == null) return;

            Type fieldType = field.GetType();
            Undo.RecordObject(field, $"Capture {fieldType.Name}");

            SetSlotValues(field, fieldType, isPhone, isPortrait, t);

            EditorUtility.SetDirty(field);
            SDebug.Log($"[FieldBaseEditor] Captured {fieldType.Name} → {OrientationHelper.GetSlotLabel(isPhone, isPortrait)}");
        }

        private void CaptureAllSlots(FieldBase field)
        {
            Transform t = field.targetTransform;
            if (t == null) return;

            Type fieldType = field.GetType();
            Undo.RecordObject(field, $"Capture All Slots {fieldType.Name}");

            SetSlotValues(field, fieldType, true, true, t);
            SetSlotValues(field, fieldType, true, false, t);
            SetSlotValues(field, fieldType, false, true, t);
            SetSlotValues(field, fieldType, false, false, t);

            EditorUtility.SetDirty(field);
            SDebug.Log($"[FieldBaseEditor] Captured {fieldType.Name} → All 4 slots");
        }

        private void SetSlotValues(FieldBase field, Type fieldType, bool isPhone, bool isPortrait, Transform t)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            if (hasPosition)
            {
                FieldInfo f = fieldType.GetField(OrientationHelper.GetPositionFieldName(isPhone, isPortrait), flags);
                if (f != null) f.SetValue(field, new Vector2(t.localPosition.x, t.localPosition.y));
            }
            if (hasScale)
            {
                FieldInfo f = fieldType.GetField(OrientationHelper.GetScaleFieldName(isPhone, isPortrait), flags);
                if (f != null) f.SetValue(field, new Vector2(t.localScale.x, t.localScale.y));
            }
            if (hasRotation)
            {
                FieldInfo f = fieldType.GetField(OrientationHelper.GetRotationFieldName(isPhone, isPortrait), flags);
                if (f != null) f.SetValue(field, t.localEulerAngles.z);
            }
        }

        #endregion

        #region Property Management Section

        private void DrawPropertyManagementSection(FieldBase field)
        {
            // Dummy Texture Validation
            DrawDummyValidationSection(field);
            EditorGUILayout.Space(5);

            GUI.backgroundColor = new Color(0.35f, 0.35f, 0.45f);
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = Color.white;

            foldoutProperties = EditorGUILayout.Foldout(foldoutProperties, "Property Groups", true, EditorStyles.foldoutHeader);

            if (foldoutProperties)
            {
                EditorGUILayout.Space(3);

            // Status badges
            EditorGUILayout.BeginHorizontal();
            DrawPropertyBadge("Texture", hasTexture);
            DrawPropertyBadge("Position", hasPosition);
            DrawPropertyBadge("Scale", hasScale);
            DrawPropertyBadge("Rotation", hasRotation);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            string scriptPath = FindScriptPath(field.GetType());
            if (string.IsNullOrEmpty(scriptPath))
            {
                EditorGUILayout.HelpBox("Cannot find source .cs file for this FieldBase.", MessageType.Warning);
                EditorGUILayout.EndVertical();
                return;
            }

            // Add buttons
            bool anyChanged = false;
            EditorGUILayout.BeginHorizontal();
            if (!hasTexture) { GUI.backgroundColor = ColorSuccess; if (GUILayout.Button("+ Texture", GUILayout.Height(25))) { AddPropertyGroup(scriptPath, "texture"); anyChanged = true; } }
            if (!hasPosition) { GUI.backgroundColor = ColorSuccess; if (GUILayout.Button("+ Position", GUILayout.Height(25))) { AddPropertyGroup(scriptPath, "position"); anyChanged = true; } }
            if (!hasScale) { GUI.backgroundColor = ColorSuccess; if (GUILayout.Button("+ Scale", GUILayout.Height(25))) { AddPropertyGroup(scriptPath, "scale"); anyChanged = true; } }
            if (!hasRotation) { GUI.backgroundColor = ColorSuccess; if (GUILayout.Button("+ Rotation", GUILayout.Height(25))) { AddPropertyGroup(scriptPath, "rotation"); anyChanged = true; } }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Remove buttons
            EditorGUILayout.BeginHorizontal();
            if (hasTexture) { GUI.backgroundColor = ColorDanger; if (GUILayout.Button("- Texture", GUILayout.Height(25))) { RemovePropertyGroup(scriptPath, "texture"); anyChanged = true; } }
            if (hasPosition) { GUI.backgroundColor = ColorDanger; if (GUILayout.Button("- Position", GUILayout.Height(25))) { RemovePropertyGroup(scriptPath, "position"); anyChanged = true; } }
            if (hasScale) { GUI.backgroundColor = ColorDanger; if (GUILayout.Button("- Scale", GUILayout.Height(25))) { RemovePropertyGroup(scriptPath, "scale"); anyChanged = true; } }
            if (hasRotation) { GUI.backgroundColor = ColorDanger; if (GUILayout.Button("- Rotation", GUILayout.Height(25))) { RemovePropertyGroup(scriptPath, "rotation"); anyChanged = true; } }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            if (anyChanged)
            {
                AssetDatabase.Refresh();
                propertiesScanned = false;
            }

            EditorGUILayout.Space(3);
            EditorGUILayout.HelpBox("Add/Remove modifies the .cs source file directly.\nUnity will recompile after changes.", MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPropertyBadge(string label, bool active)
        {
            Color bg = active ? ColorSuccess : new Color(0.4f, 0.4f, 0.4f);
            GUI.backgroundColor = bg;
            GUIStyle style = new GUIStyle(EditorStyles.miniButton)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = active ? Color.white : new Color(0.6f, 0.6f, 0.6f) }
            };
            GUILayout.Label(active ? $"[v] {label}" : $"✗ {label}", style);
            GUI.backgroundColor = Color.white;
        }

        #endregion

        #region Dummy Validation

        private void DrawDummyValidationSection(FieldBase field)
        {
            if (!hasTexture) return;

            GUI.backgroundColor = new Color(0.3f, 0.35f, 0.45f);
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = Color.white;

            foldoutDummy = EditorGUILayout.Foldout(foldoutDummy, "Dummy Texture", true, EditorStyles.foldoutHeader);

            if (foldoutDummy)
            {
                EditorGUILayout.Space(3);

                // Show current tex status
                Type fieldType = field.GetType();
                FieldInfo texField = fieldType.GetField("tex", BindingFlags.Public | BindingFlags.Instance);
                if (texField != null && texField.FieldType == typeof(Texture2D))
                {
                    Texture2D currentTex = texField.GetValue(field) as Texture2D;
                    if (currentTex != null)
                        EditorGUILayout.LabelField($"Current: {currentTex.name}", EditorStyles.miniLabel);
                    else
                        EditorGUILayout.LabelField("Current: (none)", EditorStyles.miniLabel);
                }

                EditorGUILayout.Space(3);

                EditorGUILayout.BeginHorizontal();
                GUI.backgroundColor = new Color(0.6f, 0.85f, 1f);
                if (GUILayout.Button("Fill Dummy if Empty", GUILayout.Height(25)))
                {
                    if (ValidateSingleFieldNone(field))
                        SDebug.Log($"[FieldBaseEditor] Filled dummy for {field.GetType().Name}");
                    else
                        EditorUtility.DisplayDialog("No Change", "Texture is already assigned.", "OK");
                }
                GUI.backgroundColor = new Color(1f, 0.6f, 0.3f);
                if (GUILayout.Button("Force Assign Dummy", GUILayout.Height(25)))
                {
                    if (ValidateSingleFieldForce(field))
                        SDebug.Log($"[FieldBaseEditor] Force assigned dummy for {field.GetType().Name}");
                    else
                        EditorUtility.DisplayDialog("Error", "Could not assign dummy texture.", "OK");
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private bool ValidateSingleFieldNone(FieldBase field)
        {
            if (field == null) return false;

            Type fieldType = field.GetType();
            FieldInfo texField = fieldType.GetField("tex", BindingFlags.Public | BindingFlags.Instance);
            if (texField == null || texField.FieldType != typeof(Texture2D)) return false;

            Texture2D currentTex = texField.GetValue(field) as Texture2D;
            if (currentTex != null) return false;

            string dummyPath = EditorPrefs.GetString("LunaFieldEditor_DummyPath", "Assets/_LunaFieldGenerator/Dummys");
            var manager = new DummyTextureManager(dummyPath);
            manager.ReloadDefaultTexture();
            manager.EnsureFolderExists();

            Texture2D correctTex = manager.GetOrCreateForClass(fieldType.Name);
            if (correctTex == null) return false;

            Undo.RecordObject(field, "Validate Dummy");
            texField.SetValue(field, correctTex);
            AssignTextureToVisualComponent(field, correctTex, manager);
            EditorUtility.SetDirty(field);
            return true;
        }

        private bool ValidateSingleFieldForce(FieldBase field)
        {
            if (field == null) return false;

            Type fieldType = field.GetType();
            FieldInfo texField = fieldType.GetField("tex", BindingFlags.Public | BindingFlags.Instance);
            if (texField == null || texField.FieldType != typeof(Texture2D)) return false;

            string dummyPath = EditorPrefs.GetString("LunaFieldEditor_DummyPath", "Assets/_LunaFieldGenerator/Dummys");
            var manager = new DummyTextureManager(dummyPath);
            manager.ReloadDefaultTexture();
            manager.EnsureFolderExists();

            Texture2D correctTex = manager.GetOrCreateForClass(fieldType.Name);
            if (correctTex == null) return false;

            Undo.RecordObject(field, "Force Validate Dummy");
            texField.SetValue(field, correctTex);
            AssignTextureToVisualComponent(field, correctTex, manager);
            EditorUtility.SetDirty(field);
            EditorUtility.SetDirty(field.gameObject);
            return true;
        }

        private void AssignTextureToVisualComponent(FieldBase field, Texture2D tex, DummyTextureManager manager)
        {
            UnityEngine.UI.RawImage rawImage = field.gameObject.GetComponent<UnityEngine.UI.RawImage>();
            if (rawImage != null)
            {
                Undo.RecordObject(rawImage, "Assign Dummy Visual");
                rawImage.texture = tex;
            }

            SpriteRenderer sr = field.gameObject.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Sprite sprite = manager.CreateSpriteFromTexture(tex);
                if (sprite != null)
                {
                    Undo.RecordObject(sr, "Assign Dummy Visual");
                    sr.sprite = sprite;
                }
            }
        }

        #endregion

        #region Add/Remove Property Groups

        private void AddPropertyGroup(string scriptPath, string group)
        {
            if (!File.Exists(scriptPath)) return;

            string content = File.ReadAllText(scriptPath);
            int nextIdx = GetNextAvailableIndex(content);

            string fieldsBlock = "";

            switch (group)
            {
                case "texture":
                    if (content.Contains("public Texture2D tex")) return;
                    fieldsBlock = @"
        [Header(""Texture"")]
        [LunaPlaygroundAsset(""Texture"", 0)]
        public Texture2D tex = null;
";
                    if (!content.Contains("using UnityEngine.UI;"))
                        content = content.Replace("using UnityEngine;", "using UnityEngine;\nusing UnityEngine.UI;");
                    break;

                case "position":
                    if (content.Contains("phone_Position_PT")) return;
                    fieldsBlock = $@"
        [Header(""Phone Position"")]
        [LunaPlaygroundField(""Phone_Position_PT"", {nextIdx})]
        public Vector2 phone_Position_PT = Vector2.zero;
        [LunaPlaygroundField(""Phone_Position_LS"", {nextIdx + 1})]
        public Vector2 phone_Position_LS = Vector2.zero;

        [Header(""Tablet Position"")]
        [LunaPlaygroundField(""Tablet_Position_PT"", {nextIdx + 10})]
        public Vector2 tablet_Position_PT = Vector2.zero;
        [LunaPlaygroundField(""Tablet_Position_LS"", {nextIdx + 11})]
        public Vector2 tablet_Position_LS = Vector2.zero;
";
                    break;

                case "scale":
                    if (content.Contains("phone_Scale_PT")) return;
                    fieldsBlock = $@"
        [Header(""Phone Scale"")]
        [LunaPlaygroundField(""Phone_Scale_PT"", {nextIdx})]
        public Vector2 phone_Scale_PT = Vector2.one;
        [LunaPlaygroundField(""Phone_Scale_LS"", {nextIdx + 1})]
        public Vector2 phone_Scale_LS = Vector2.one;

        [Header(""Tablet Scale"")]
        [LunaPlaygroundField(""Tablet_Scale_PT"", {nextIdx + 10})]
        public Vector2 tablet_Scale_PT = Vector2.one;
        [LunaPlaygroundField(""Tablet_Scale_LS"", {nextIdx + 11})]
        public Vector2 tablet_Scale_LS = Vector2.one;
";
                    break;

                case "rotation":
                    if (content.Contains("phone_Rotation_PT")) return;
                    fieldsBlock = $@"
        [Header(""Phone Rotation"")]
        [LunaPlaygroundField(""Phone_Rotation_PT"", {nextIdx})]
        public float phone_Rotation_PT = 0f;
        [LunaPlaygroundField(""Phone_Rotation_LS"", {nextIdx + 1})]
        public float phone_Rotation_LS = 0f;

        [Header(""Tablet Rotation"")]
        [LunaPlaygroundField(""Tablet_Rotation_PT"", {nextIdx + 10})]
        public float tablet_Rotation_PT = 0f;
        [LunaPlaygroundField(""Tablet_Rotation_LS"", {nextIdx + 11})]
        public float tablet_Rotation_LS = 0f;
";
                    break;
            }

            content = InsertFieldsBlock(content, fieldsBlock);

            if (group == "position" || group == "scale" || group == "rotation")
                content = RebuildApplyOrientation(content);
            else if (group == "texture")
                content = InsertApplyTextureIfMissing(content);

            // Update metadata overrides
            content = UpdateMetadataOverrides(content);

            File.WriteAllText(scriptPath, content);
            SDebug.Log($"[FieldBaseEditor] Added '{group}' to {Path.GetFileName(scriptPath)}");
        }

        private void RemovePropertyGroup(string scriptPath, string group)
        {
            if (!File.Exists(scriptPath)) return;

            if (!EditorUtility.DisplayDialog("Remove Property Group",
                $"Remove all '{group}' fields and related code?", "Yes", "Cancel"))
                return;

            string content = File.ReadAllText(scriptPath);

            switch (group)
            {
                case "texture":
                    content = RemoveFieldBlock(content, "Texture");
                    content = RemoveMethod(content, "ApplyTexture");
                    break;
                case "position":
                    content = RemoveFieldBlock(content, "Phone Position");
                    content = RemoveFieldBlock(content, "Tablet Position");
                    break;
                case "scale":
                    content = RemoveFieldBlock(content, "Phone Scale");
                    content = RemoveFieldBlock(content, "Tablet Scale");
                    break;
                case "rotation":
                    content = RemoveFieldBlock(content, "Phone Rotation");
                    content = RemoveFieldBlock(content, "Tablet Rotation");
                    break;
            }

            if (group == "position" || group == "scale" || group == "rotation")
                content = RebuildApplyOrientation(content);

            content = UpdateMetadataOverrides(content);
            content = Regex.Replace(content, @"\n{3,}", "\n\n");

            File.WriteAllText(scriptPath, content);
            SDebug.Log($"[FieldBaseEditor] Removed '{group}' from {Path.GetFileName(scriptPath)}");
        }

        #endregion

        #region Source File Helpers

        private int GetNextAvailableIndex(string content)
        {
            int max = 0;
            var matches = Regex.Matches(content, @"\[LunaPlayground(?:Asset|Field)\s*\(\s*""[^""]*""\s*,\s*(\d+)");
            foreach (Match m in matches)
            {
                if (int.TryParse(m.Groups[1].Value, out int idx) && idx > max)
                    max = idx;
            }
            return ((max / 20) + 1) * 20 + 1;
        }

        private string InsertFieldsBlock(string content, string fieldsBlock)
        {
            int insertPos = content.IndexOf("public override void Apply");
            if (insertPos < 0)
            {
                // Find before #region or closing brace
                int regionPos = content.IndexOf("#region Metadata");
                if (regionPos > 0)
                    insertPos = regionPos;
                else
                {
                    int lastBrace = content.LastIndexOf('}');
                    if (lastBrace > 0) lastBrace = content.LastIndexOf('}', lastBrace - 1);
                    insertPos = lastBrace > 0 ? lastBrace : content.Length;
                }
            }
            else
            {
                while (insertPos > 0 && content[insertPos - 1] != '\n') insertPos--;
            }

            return content.Insert(insertPos, fieldsBlock + "\n");
        }

        private string InsertApplyTextureIfMissing(string content)
        {
            if (content.Contains("override void ApplyTexture")) return content;

            string method = @"
        public override void ApplyTexture()
        {
            if (tex == null) return;
            RawImage img = GetComponent<RawImage>();
            if (img != null) img.texture = tex;
        }
";
            int lastBrace = content.LastIndexOf('}');
            if (lastBrace > 0) lastBrace = content.LastIndexOf('}', lastBrace - 1);
            if (lastBrace > 0)
                return content.Insert(lastBrace, method + "\n");
            return content;
        }

        private string RemoveFieldBlock(string content, string headerName)
        {
            string pattern = $@"\s*\[Header\(""{Regex.Escape(headerName)}""\)\].*?(?=\[Header|public override|#region|$)";
            return Regex.Replace(content, pattern, "\n", RegexOptions.Singleline);
        }

        private string RemoveMethod(string content, string methodName)
        {
            string pattern = $@"\s*public override void {methodName}\s*\(\s*\)\s*\{{[^}}]*\}}";
            return Regex.Replace(content, pattern, "", RegexOptions.Singleline);
        }

        private string RebuildApplyOrientation(string content)
        {
            content = RemoveMethod(content, "ApplyOrientation");

            bool hasPos = content.Contains("phone_Position_PT");
            bool hasScl = content.Contains("phone_Scale_PT");
            bool hasRot = content.Contains("phone_Rotation_PT");

            if (!hasPos && !hasScl && !hasRot) return content;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(@"
        public override void ApplyOrientation()
        {
            bool isPhone = OrientationTracker.Instance.IsPhone;
            bool isPortrait = OrientationTracker.Instance.IsPortrait;
");
            if (hasPos)
            {
                sb.AppendLine(@"            Vector2 pos;
            if (isPhone) pos = isPortrait ? phone_Position_PT : phone_Position_LS;
            else pos = isPortrait ? tablet_Position_PT : tablet_Position_LS;
            targetTransform.localPosition = new Vector3(pos.x, pos.y, 0);
");
            }
            if (hasScl)
            {
                sb.AppendLine(@"            Vector2 scale;
            if (isPhone) scale = isPortrait ? phone_Scale_PT : phone_Scale_LS;
            else scale = isPortrait ? tablet_Scale_PT : tablet_Scale_LS;
            targetTransform.localScale = new Vector3(scale.x, scale.y, 1);
");
            }
            if (hasRot)
            {
                sb.AppendLine(@"            float rot;
            if (isPhone) rot = isPortrait ? phone_Rotation_PT : phone_Rotation_LS;
            else rot = isPortrait ? tablet_Rotation_PT : tablet_Rotation_LS;
            targetTransform.localRotation = Quaternion.Euler(0, 0, rot);
");
            }
            sb.AppendLine("        }");

            // Insert before last two closing braces
            int lastBrace = content.LastIndexOf('}');
            if (lastBrace > 0) lastBrace = content.LastIndexOf('}', lastBrace - 1);
            if (lastBrace > 0)
                return content.Insert(lastBrace, sb.ToString() + "\n");
            return content;
        }

        /// <summary>
        /// Update HasPosition/HasScale/HasRotation/HasTexture overrides based on current content.
        /// </summary>
        private string UpdateMetadataOverrides(string content)
        {
            // Remove existing metadata region if present
            content = Regex.Replace(content,
                @"\s*#region Metadata.*?#endregion",
                "", RegexOptions.Singleline);

            bool hasPos = content.Contains("phone_Position_PT");
            bool hasScl = content.Contains("phone_Scale_PT");
            bool hasRot = content.Contains("phone_Rotation_PT");
            bool hasTex = content.Contains("public Texture2D tex");

            if (!hasPos && !hasScl && !hasRot && !hasTex) return content;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("\n        #region Metadata");
            if (hasTex) sb.AppendLine("        public override bool HasTexture => true;");
            if (hasPos) sb.AppendLine("        public override bool HasPosition => true;");
            if (hasScl) sb.AppendLine("        public override bool HasScale => true;");
            if (hasRot) sb.AppendLine("        public override bool HasRotation => true;");
            sb.AppendLine("        #endregion\n");

            // Insert after class opening brace + first field block
            int insertPos = content.IndexOf("public override void Apply");
            if (insertPos < 0)
            {
                int lastBrace = content.LastIndexOf('}');
                if (lastBrace > 0) lastBrace = content.LastIndexOf('}', lastBrace - 1);
                insertPos = lastBrace > 0 ? lastBrace : content.Length;
            }
            else
            {
                while (insertPos > 0 && content[insertPos - 1] != '\n') insertPos--;
            }

            return content.Insert(insertPos, sb.ToString());
        }

        #endregion

        #region Helpers

        private string FindScriptPath(Type type)
        {
            string[] guids = AssetDatabase.FindAssets($"{type.Name} t:MonoScript");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                MonoScript ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (ms != null && ms.GetClass() == type)
                    return path;
            }
            return null;
        }

        private void DrawMiniLabel(string label, string value, Color color)
        {
            GUI.backgroundColor = color;
            EditorGUILayout.BeginVertical("box");
            GUI.backgroundColor = Color.white;

            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            GUIStyle valueStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 11 };

            EditorGUILayout.LabelField(label, labelStyle);
            EditorGUILayout.LabelField(value, valueStyle);

            EditorGUILayout.EndVertical();
        }

        #endregion
    }
}
