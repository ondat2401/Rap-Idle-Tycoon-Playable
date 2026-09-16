using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using Amanotes.Core;
using Amanotes.LunaFieldGenerator;

namespace Amanotes.LunaFieldGenerator.Editor
{
    /// <summary>
    /// Partial: Automation pipeline for creating fields after script compilation.
    /// Handles pending state persistence across domain reloads.
    /// </summary>
    public partial class LunaFieldControllerEditor
    {
        #region Automation State

        private int automationRetryCount = 0;
        private const int AutomationMaxRetries = 5;

        #endregion

        #region Automation Pipeline

        internal void CheckPendingAutomation()
        {
            pendingFieldClassName = EditorPrefs.GetString("LunaField_PendingClassName", "");
            pendingFieldControllerInstanceID = EditorPrefs.GetString("LunaField_PendingControllerID", "");
            string pendingTypeStr = EditorPrefs.GetString("LunaField_PendingFieldType", "");
            pendingParentPath = EditorPrefs.GetString("LunaField_PendingParentPath", "");
            pendingSubFolderName = EditorPrefs.GetString("LunaField_PendingSubFolder", "");
            pendingWithTexture = EditorPrefs.GetBool("LunaField_PendingWithTexture", true);
            pendingAttachToExistingGO = EditorPrefs.GetBool("LunaField_PendingAttachToExisting", false);

            if (!string.IsNullOrEmpty(pendingTypeStr))
                Enum.TryParse(pendingTypeStr, out pendingFieldObjectType);

            if (!string.IsNullOrEmpty(pendingFieldClassName) &&
                !string.IsNullOrEmpty(pendingFieldControllerInstanceID))
            {
                EditorApplication.delayCall += TryContinueAutomationWithRetry;
            }
        }

        private void TryContinueAutomationWithRetry()
        {
            if (string.IsNullOrEmpty(pendingFieldClassName)) return;

            string fullClassName = pendingFieldClassName + "_LunaField";
            Type type = FieldTypeScanner.FindTypeByName(fullClassName);

            if (type != null)
            {
                automationRetryCount = 0;
                ContinueAutomation();
            }
            else if (automationRetryCount < AutomationMaxRetries)
            {
                automationRetryCount++;
                SDebug.Log($"[LunaFieldEditor] Type '{fullClassName}' not ready, retrying ({automationRetryCount}/{AutomationMaxRetries})...");
                EditorApplication.delayCall += TryContinueAutomationWithRetry;
            }
            else
            {
                automationRetryCount = 0;
                SDebug.LogError($"[LunaFieldEditor] Type not found after {AutomationMaxRetries} retries: {fullClassName}");
                EditorUtility.DisplayDialog("Automation Failed",
                    $"Could not find type '{fullClassName}' after compilation.\nCheck Console for errors.", "OK");
                ClearPendingAutomation();
            }
        }

        private void ContinueAutomation()
        {
            if (string.IsNullOrEmpty(pendingFieldClassName)) return;

            try
            {
                LunaFieldController controller = EditorUtility.InstanceIDToObject(
                    int.Parse(pendingFieldControllerInstanceID)) as LunaFieldController;

                if (controller == null)
                {
                    controller = FindObjectOfType<LunaFieldController>();
                    if (controller == null)
                    {
                        SDebug.LogError("[LunaFieldEditor] Could not find LunaFieldController");
                        ClearPendingAutomation();
                        return;
                    }
                }

                Transform parentForCreate = null;
                if (!string.IsNullOrEmpty(pendingParentPath))
                {
                    GameObject parentObj = GameObject.Find(pendingParentPath);
                    if (parentObj != null)
                        parentForCreate = parentObj.transform;
                }

                string fullClassName = pendingFieldClassName + "_LunaField";

                if (pendingAttachToExistingGO && parentForCreate != null)
                {
                    ScanForFieldTypes();
                    Texture2D tex = pendingWithTexture ? dummyManager.GetOrCreateForClass(fullClassName) : null;
                    AddFieldToController(controller, fullClassName, parentForCreate, tex);
                    ValidateDummies(controller);

                    Selection.activeGameObject = parentForCreate.gameObject;
                    EditorGUIUtility.PingObject(parentForCreate.gameObject);

                    EditorUtility.DisplayDialog("Complete",
                        $"Added '{fullClassName}' to '{parentForCreate.name}'.", "OK");
                }
                else
                {
                    CompleteAutomatedFieldCreation(controller, fullClassName,
                        pendingFieldObjectType, parentForCreate, pendingSubFolderName, pendingWithTexture);
                }
            }
            catch (Exception e)
            {
                SDebug.LogError($"[LunaFieldEditor] Automation error: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("Automation Failed", e.Message, "OK");
            }
            finally
            {
                ClearPendingAutomation();
            }
        }

        private void CompleteAutomatedFieldCreation(
            LunaFieldController controller, string fullClassName,
            FieldObjectType fieldType, Transform parentForCreate,
            string subFolderName = null, bool withTexture = true)
        {
            try
            {
                LunaProgressWindow.Show("Luna Field Automation");
                LunaProgressWindow.Update("Rescanning types...", 0.1f);
                ScanForFieldTypes();

                LunaProgressWindow.Update("Resolving parent...", 0.3f);

                Transform parentToUse;
                if (parentForCreate == null && !string.IsNullOrEmpty(subFolderName))
                    parentToUse = SubfolderResolver.ResolveSubFolderParent(subFolderName, null);
                else
                    parentToUse = SubfolderResolver.ResolveSubFolderParent(subFolderName,
                        parentForCreate != null ? parentForCreate : controller.transform);

                LunaProgressWindow.Update($"Creating GameObject...", 0.5f);

                Transform existingTransform = SubfolderResolver.FindTransformByName(fullClassName, parentToUse);
                GameObject newGameObject;

                if (existingTransform == null)
                {
                    newGameObject = withTexture
                        ? CreateFieldGameObject(fullClassName, parentToUse, fieldType)
                        : CreateFieldGameObjectNoTexture(fullClassName, parentToUse);
                    Undo.RegisterCreatedObjectUndo(newGameObject, "Automated Field Creation");
                    existingTransform = newGameObject.transform;
                }
                else
                {
                    newGameObject = existingTransform.gameObject;
                }

                LunaProgressWindow.Update("Registering field...", 0.8f);

                Texture2D texture = withTexture ? dummyManager.GetOrCreateForClass(fullClassName) : null;
                AddFieldToController(controller, fullClassName, existingTransform, texture);
                ValidateDummies(controller);

                LunaProgressWindow.Dismiss();

                Selection.activeGameObject = newGameObject;
                EditorGUIUtility.PingObject(newGameObject);

                EditorUtility.DisplayDialog("Automation Complete!",
                    $"Class: {fullClassName}\nGameObject: {newGameObject.name}\nParent: {parentToUse.name}", "OK");
            }
            catch (Exception e)
            {
                LunaProgressWindow.Dismiss();
                SDebug.LogError($"[LunaFieldEditor] Automation error: {e.Message}");
                EditorUtility.DisplayDialog("Automation Failed", e.Message, "OK");
            }
        }

        internal void ClearPendingAutomation()
        {
            pendingFieldClassName = null;
            pendingFieldControllerInstanceID = null;
            pendingParentPath = null;
            pendingSubFolderName = null;
            pendingWithTexture = true;
            pendingAttachToExistingGO = false;

            EditorPrefs.DeleteKey("LunaField_PendingClassName");
            EditorPrefs.DeleteKey("LunaField_PendingControllerID");
            EditorPrefs.DeleteKey("LunaField_PendingFieldType");
            EditorPrefs.DeleteKey("LunaField_PendingParentPath");
            EditorPrefs.DeleteKey("LunaField_PendingSubFolder");
            EditorPrefs.DeleteKey("LunaField_PendingWithTexture");
            EditorPrefs.DeleteKey("LunaField_PendingAttachToExisting");
            EditorPrefs.DeleteKey("LunaField_PendingTargetGO");
        }

        internal void StartAutomation(LunaFieldController controller, string baseName, Transform parent, string subFolderName = null)
        {
            pendingFieldClassName = baseName;
            pendingFieldControllerInstanceID = controller.GetInstanceID().ToString();
            pendingFieldObjectType = createFieldType;
            pendingWithTexture = createWithTexture || createAsBackground;
            pendingParentPath = parent != null ? SubfolderResolver.GetGameObjectPath(parent.gameObject) : "";
            pendingSubFolderName = subFolderName ?? "";

            EditorPrefs.SetString("LunaField_PendingClassName", baseName);
            EditorPrefs.SetString("LunaField_PendingControllerID", controller.GetInstanceID().ToString());
            EditorPrefs.SetString("LunaField_PendingFieldType", createFieldType.ToString());
            EditorPrefs.SetBool("LunaField_PendingWithTexture", pendingWithTexture);
            EditorPrefs.SetString("LunaField_PendingParentPath", pendingParentPath);
            EditorPrefs.SetString("LunaField_PendingSubFolder", pendingSubFolderName);
        }

        #endregion

        #region Field Creation Helpers

        internal GameObject CreateFieldGameObject(string goName, Transform parent, FieldObjectType fieldType)
        {
            if (fieldType == FieldObjectType.RawImage)
                parent = SubfolderResolver.EnsureParentInsideCanvas(parent);

            GameObject go = new GameObject(goName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            Texture2D dummyTex = dummyManager.GetOrCreateForClass(goName);
            if (fieldType == FieldObjectType.RawImage)
                SetupRawImageOnNewObject(go, dummyTex);
            else if (fieldType == FieldObjectType.SpriteRenderer)
                CreateSpriteRendererComponent(go, dummyTex);

            return go;
        }

        internal GameObject CreateFieldGameObjectNoTexture(string goName, Transform parent)
        {
            GameObject go = new GameObject(goName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go;
        }

        internal void AddFieldToController(LunaFieldController controller, string fullClassName, Transform fieldTransform, Texture2D texture)
        {
            Type fieldType = FieldTypeScanner.FindTypeByName(fullClassName);
            if (fieldType == null)
            {
                SDebug.LogError($"[LunaFieldEditor] Type not found: {fullClassName}");
                return;
            }

            GameObject go = fieldTransform.gameObject;
            FieldBase field = go.GetComponent(fieldType) as FieldBase;
            if (field == null)
                field = Undo.AddComponent(go, fieldType) as FieldBase;

            if (field == null)
            {
                SDebug.LogError($"[LunaFieldEditor] Failed to add component: {fieldType.Name}");
                return;
            }

            // Assign texture via reflection
            var texField = fieldType.GetField("tex", BindingFlags.Public | BindingFlags.Instance);
            if (texField != null && texField.FieldType == typeof(Texture2D))
                texField.SetValue(field, texture);

            Undo.RecordObject(controller, "Add Field");
            controller.AddField(field);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(field);

            SDebug.Log($"[LunaFieldEditor] Added {fieldType.Name} to controller ({controller.FieldCount} total)");
        }

        #endregion
    }
}
