using UnityEditor;
using UnityEngine;
using OrientationTracking.Core;
using System;
using System.Reflection;
using Amanotes.Core;
using Amanotes.LunaFieldGenerator;

namespace Amanotes.LunaFieldGenerator.Editor
{
    /// <summary>
    /// Partial: Capture tab — captures current transform values into FieldBase orientation slots.
    /// Uses OrientationHelper for shared device/orientation detection.
    /// </summary>
    public partial class LunaFieldControllerEditor
    {
        #region Capture State

        private bool showCaptureSection = true;
        private Vector2 captureScrollPosition;

        #endregion

        #region Capture Tab UI

        private void DrawCaptureTab(LunaFieldController controller)
        {
            EditorGUILayout.BeginVertical("box");

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.LabelField("Capture Tool", titleStyle);
            EditorGUILayout.Space(5);

            DrawOrientationStatus();
            EditorGUILayout.Space(10);
            DrawCaptureAllSection(controller);
            EditorGUILayout.Space(10);
            DrawPerFieldCaptureSection(controller);

            EditorGUILayout.EndVertical();
        }

        private void DrawOrientationStatus()
        {
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Current Orientation", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            bool trackerReady = OrientationHelper.IsTrackerReady;

            if (!trackerReady)
            {
                EditorGUILayout.HelpBox(
                    "OrientationTracker not initialized.\n" +
                    "Play the scene for tracker to work, or capture will use Game view size.",
                    MessageType.Warning);

                Vector2 gameViewSize = OrientationHelper.GetGameViewSize();
                bool fallbackPortrait = gameViewSize.y > gameViewSize.x;

                EditorGUILayout.BeginHorizontal();
                DrawStatusLabel("Game View", $"{gameViewSize.x:F0} x {gameViewSize.y:F0}", Theme.Info);
                DrawStatusLabel("Detected", fallbackPortrait ? "Portrait" : "Landscape",
                    fallbackPortrait ? Theme.Primary : Theme.Warning);
                EditorGUILayout.EndHorizontal();

                // Device override toggle
                EditorGUILayout.Space(3);
                EditorGUILayout.BeginHorizontal("helpbox");
                EditorGUILayout.LabelField("Device Override:", GUILayout.Width(105));
                bool isPhone = OrientationHelper.EditorDeviceIsPhone;
                EditorGUI.BeginChangeCheck();
                bool phone = EditorGUILayout.ToggleLeft("Phone", isPhone, GUILayout.Width(60));
                bool tablet = EditorGUILayout.ToggleLeft("Tablet", !isPhone, GUILayout.Width(60));
                if (EditorGUI.EndChangeCheck())
                {
                    OrientationHelper.EditorDeviceIsPhone = tablet ? false : phone;
                    Repaint();
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                var tracker = OrientationTracker.Instance;
                string deviceStr = tracker.IsPhone ? "Phone" : "Tablet";
                string orientStr = tracker.IsPortrait ? "Portrait" : "Landscape";

                EditorGUILayout.BeginHorizontal();
                DrawStatusLabel("Device", deviceStr, tracker.IsPhone ? Theme.Info : Theme.Warning);
                DrawStatusLabel("Orientation", orientStr, tracker.IsPortrait ? Theme.Primary : Theme.Success);
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

            EditorGUILayout.EndVertical();
        }

        private void DrawCaptureAllSection(LunaFieldController controller)
        {
            int fieldCount = controller.FieldCount;
            if (fieldCount == 0)
            {
                EditorGUILayout.HelpBox("No fields available. Add fields first.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Capture All Fields", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            bool isPhone = OrientationHelper.IsPhone();
            bool isPortrait = OrientationHelper.IsPortrait();
            string targetSlot = OrientationHelper.GetSlotLabel(isPhone, isPortrait);

            EditorGUILayout.HelpBox(
                $"Capture all {fieldCount} field(s) to slot: {targetSlot}\n" +
                $"Each field will store current localPosition, localScale, localRotation.",
                MessageType.Info);

            EditorGUILayout.Space(5);

            GUI.backgroundColor = Theme.Success;
            if (GUILayout.Button($"Capture All → {targetSlot}", GUILayout.Height(40)))
                CaptureAllFields(controller, isPhone, isPortrait);
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(3);

            GUI.backgroundColor = Theme.Warning;
            if (GUILayout.Button("Capture All → All 4 Slots", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Capture All Slots",
                    "Overwrite all 4 slots for all fields with current transform values?", "Yes", "Cancel"))
                    CaptureAllFieldsAllSlots(controller);
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndVertical();
        }

        private void DrawPerFieldCaptureSection(LunaFieldController controller)
        {
            int fieldCount = controller.FieldCount;
            if (fieldCount == 0) return;

            DrawSectionHeader(ref showCaptureSection, $"Per-Field Capture ({fieldCount})", Theme.Primary);
            if (!showCaptureSection) return;

            bool isPhone = OrientationHelper.IsPhone();
            bool isPortrait = OrientationHelper.IsPortrait();
            string targetSlot = OrientationHelper.GetSlotLabel(isPhone, isPortrait);

            captureScrollPosition = EditorGUILayout.BeginScrollView(captureScrollPosition, GUILayout.MaxHeight(400));

            for (int i = 0; i < fieldCount; i++)
            {
                FieldBase field = controller.fields[i];
                if (field == null) continue;

                DrawFieldCaptureItem(field, i, isPhone, isPortrait, targetSlot);
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawFieldCaptureItem(FieldBase field, int index, bool isPhone, bool isPortrait, string targetSlot)
        {
            Type fieldType = field.GetType();
            Transform t = field.targetTransform;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"{index + 1}. {fieldType.Name}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            // Current values preview
            if (t != null)
            {
                Vector2 pos = new Vector2(t.localPosition.x, t.localPosition.y);
                Vector2 scl = new Vector2(t.localScale.x, t.localScale.y);

                GUIStyle previewStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    normal = { textColor = Color.gray }
                };
                EditorGUILayout.LabelField($"P({pos.x:F1},{pos.y:F1}) S({scl.x:F2},{scl.y:F2})", previewStyle, GUILayout.Width(200));
            }

            GUI.backgroundColor = Theme.Success;
            if (GUILayout.Button($"{targetSlot}", GUILayout.Width(140), GUILayout.Height(22)))
                CaptureField(field, isPhone, isPortrait);
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            // Show stored values
            DrawStoredValues(field, isPhone, isPortrait);

            EditorGUILayout.EndVertical();
        }

        private void DrawStoredValues(FieldBase field, bool isPhone, bool isPortrait)
        {
            // Use FieldBase virtual API if available, fallback to reflection
            bool hasPos = field.HasPosition;
            bool hasScale = field.HasScale;

            if (!hasPos && !hasScale)
            {
                // Fallback: check via reflection for legacy fields
                Type fieldType = field.GetType();
                BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
                hasPos = fieldType.GetField(OrientationHelper.GetPositionFieldName(isPhone, isPortrait), flags) != null;
                hasScale = fieldType.GetField(OrientationHelper.GetScaleFieldName(isPhone, isPortrait), flags) != null;
            }

            if (!hasPos && !hasScale) return;

            var sb = new System.Text.StringBuilder("   Stored → ");

            if (hasPos)
            {
                Vector2 storedPos = GetStoredPositionViaReflection(field, isPhone, isPortrait);
                sb.Append($"Pos({storedPos.x:F1},{storedPos.y:F1})  ");
            }
            if (hasScale)
            {
                Vector2 storedScale = GetStoredScaleViaReflection(field, isPhone, isPortrait);
                sb.Append($"Scale({storedScale.x:F2},{storedScale.y:F2})");
            }

            GUIStyle storedStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.5f, 0.8f, 0.5f) }
            };
            EditorGUILayout.LabelField(sb.ToString(), storedStyle);
        }

        #endregion

        #region Capture Logic

        private void CaptureAllFields(LunaFieldController controller, bool isPhone, bool isPortrait)
        {
            if (controller.fields == null || controller.fields.Length == 0) return;

            int captured = 0;
            string targetSlot = OrientationHelper.GetSlotLabel(isPhone, isPortrait);

            for (int i = 0; i < controller.fields.Length; i++)
            {
                FieldBase field = controller.fields[i];
                if (field == null) continue;
                if (CaptureField(field, isPhone, isPortrait))
                    captured++;
            }

            EditorUtility.DisplayDialog("Capture Complete",
                $"Captured {captured}/{controller.fields.Length} field(s) → {targetSlot}", "OK");
        }

        private void CaptureAllFieldsAllSlots(LunaFieldController controller)
        {
            if (controller.fields == null || controller.fields.Length == 0) return;

            int captured = 0;
            for (int i = 0; i < controller.fields.Length; i++)
            {
                FieldBase field = controller.fields[i];
                if (field == null) continue;

                Undo.RecordObject(field, $"Capture All Slots {field.GetType().Name}");

                // Try virtual API first
                field.CaptureAllSlots();

                // Fallback: reflection-based capture for legacy fields
                CaptureFieldViaReflection(field, true, true);
                CaptureFieldViaReflection(field, true, false);
                CaptureFieldViaReflection(field, false, true);
                CaptureFieldViaReflection(field, false, false);

                EditorUtility.SetDirty(field);
                captured++;
            }

            EditorUtility.DisplayDialog("Capture Complete",
                $"Captured {captured} field(s) → All 4 Slots", "OK");
        }

        private bool CaptureField(FieldBase field, bool isPhone, bool isPortrait)
        {
            if (field == null) return false;

            Transform t = field.targetTransform;
            if (t == null) return false;

            Undo.RecordObject(field, $"Capture {field.GetType().Name}");

            // Use virtual API
            field.CaptureSlot(isPhone, isPortrait);

            // Also do reflection-based capture for legacy fields without override
            CaptureFieldViaReflection(field, isPhone, isPortrait);

            EditorUtility.SetDirty(field);
            return true;
        }

        /// <summary>
        /// Reflection-based capture for legacy FieldBase subclasses that don't override CaptureSlotFrom.
        /// </summary>
        private void CaptureFieldViaReflection(FieldBase field, bool isPhone, bool isPortrait)
        {
            Transform t = field.targetTransform;
            if (t == null) return;

            Type fieldType = field.GetType();
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            string posName = OrientationHelper.GetPositionFieldName(isPhone, isPortrait);
            FieldInfo posField = fieldType.GetField(posName, flags);
            if (posField != null && posField.FieldType == typeof(Vector2))
                posField.SetValue(field, new Vector2(t.localPosition.x, t.localPosition.y));

            string scaleName = OrientationHelper.GetScaleFieldName(isPhone, isPortrait);
            FieldInfo scaleField = fieldType.GetField(scaleName, flags);
            if (scaleField != null && scaleField.FieldType == typeof(Vector2))
                scaleField.SetValue(field, new Vector2(t.localScale.x, t.localScale.y));

            string rotName = OrientationHelper.GetRotationFieldName(isPhone, isPortrait);
            FieldInfo rotField = fieldType.GetField(rotName, flags);
            if (rotField != null && rotField.FieldType == typeof(float))
                rotField.SetValue(field, t.localEulerAngles.z);
        }

        private Vector2 GetStoredPositionViaReflection(FieldBase field, bool isPhone, bool isPortrait)
        {
            Type fieldType = field.GetType();
            string posName = OrientationHelper.GetPositionFieldName(isPhone, isPortrait);
            FieldInfo posField = fieldType.GetField(posName, BindingFlags.Public | BindingFlags.Instance);
            if (posField != null && posField.FieldType == typeof(Vector2))
                return (Vector2)posField.GetValue(field);
            return Vector2.zero;
        }

        private Vector2 GetStoredScaleViaReflection(FieldBase field, bool isPhone, bool isPortrait)
        {
            Type fieldType = field.GetType();
            string scaleName = OrientationHelper.GetScaleFieldName(isPhone, isPortrait);
            FieldInfo scaleField = fieldType.GetField(scaleName, BindingFlags.Public | BindingFlags.Instance);
            if (scaleField != null && scaleField.FieldType == typeof(Vector2))
                return (Vector2)scaleField.GetValue(field);
            return Vector2.one;
        }

        #endregion
    }
}
