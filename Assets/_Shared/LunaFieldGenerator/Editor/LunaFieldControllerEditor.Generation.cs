using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using Amanotes.Core;
using Amanotes.LunaFieldGenerator;

namespace Amanotes.LunaFieldGenerator.Editor
{
    /// <summary>
    /// Partial: Field generation, append, preview, and validation logic.
    /// </summary>
    public partial class LunaFieldControllerEditor
    {
        #region Generate All Fields

        private void GenerateAllFields(LunaFieldController controller)
        {
            List<Type> typesToGenerate = GetFilteredFieldTypes();
            if (typesToGenerate.Count == 0)
            {
                EditorUtility.DisplayDialog("No Field Types", "No field types found. Check your field path or folder filter.", "OK");
                return;
            }

            string genFolderName = GetGenerateSubFolderName();
            string folderInfo = !string.IsNullOrEmpty(genFolderName) ? $"[v] Folder filter: {genFolderName}\n" : "";

            if (!EditorUtility.DisplayDialog("Generate All Fields",
                $"This will:\n\n" +
                $"[v] Clear existing {controller.FieldCount} field(s)\n" +
                $"[v] Add {typesToGenerate.Count} field types\n" +
                folderInfo +
                $"[v] Auto-assign dummy textures\n" +
                $"[v] Auto-find & assign GameObjects\n\nContinue?",
                "Yes, Generate", "Cancel"))
                return;

            fieldFoldouts.Clear();
            controller.fields = new FieldBase[0];
            dummyManager.EnsureFolderExists();

            Transform effectiveParent = parentTransformForGenerate != null ? parentTransformForGenerate : controller.transform;
            if (!string.IsNullOrEmpty(genFolderName))
                effectiveParent = SubfolderResolver.ResolveSubFolderParent(genFolderName, effectiveParent);
            if (createFieldType == FieldObjectType.RawImage)
                effectiveParent = SubfolderResolver.EnsureParentInsideCanvas(effectiveParent);

            Dictionary<string, Transform> gameObjectLookup = BuildGameObjectLookup(controller, effectiveParent);

            var texFieldCache = new Dictionary<Type, FieldInfo>(typesToGenerate.Count);
            for (int i = 0; i < typesToGenerate.Count; i++)
            {
                FieldInfo fi = typesToGenerate[i].GetField("tex", BindingFlags.Public | BindingFlags.Instance);
                if (fi != null && fi.FieldType == typeof(Texture2D))
                    texFieldCache[typesToGenerate[i]] = fi;
            }

            LunaProgressWindow.Show("Generating Fields", typesToGenerate.Count);
            LunaProgressWindow.Update("Starting...", 0f, 0);

            var ctx = new GenerateContext
            {
                controller = controller,
                types = typesToGenerate,
                newFields = new List<FieldBase>(typesToGenerate.Count),
                addedTypes = new HashSet<Type>(),
                texFieldCache = texFieldCache,
                gameObjectLookup = gameObjectLookup,
                effectiveParent = effectiveParent,
                index = 0,
                componentsAdded = 0,
                goCreated = 0,
                autoRemoveDups = autoRemoveDuplicates,
            };

            EditorApplication.delayCall += () => GenerateFieldStep(ctx);
        }

        private class GenerateContext
        {
            public LunaFieldController controller;
            public List<Type> types;
            public List<FieldBase> newFields;
            public HashSet<Type> addedTypes;
            public Dictionary<Type, FieldInfo> texFieldCache;
            public Dictionary<string, Transform> gameObjectLookup;
            public Transform effectiveParent;
            public int index;
            public int componentsAdded;
            public int goCreated;
            public bool autoRemoveDups;
        }

        private void GenerateFieldStep(GenerateContext ctx)
        {
            while (ctx.index < ctx.types.Count &&
                   ctx.autoRemoveDups && ctx.addedTypes.Contains(ctx.types[ctx.index]))
                ctx.index++;

            if (ctx.index >= ctx.types.Count)
            {
                ctx.controller.fields = ctx.newFields.ToArray();
                EditorUtility.SetDirty(ctx.controller);
                LunaProgressWindow.Update("Done!", 1f, ctx.types.Count);
                LunaProgressWindow.Dismiss();
                EditorUtility.DisplayDialog("Success!",
                    $"Fields created: {ctx.newFields.Count}\n" +
                    $"GameObjects created: {ctx.goCreated}\n" +
                    $"Components added: {ctx.componentsAdded}", "OK");
                return;
            }

            Type type = ctx.types[ctx.index];
            float progress = 0.1f + 0.9f * ((float)ctx.index / ctx.types.Count);
            LunaProgressWindow.Update($"{type.Name}", progress, ctx.index + 1);

            try
            {
                GameObject targetGO = FindOrCreateGameObjectForType(
                    ctx.controller, type, ctx.gameObjectLookup, out bool wasCreated, ctx.effectiveParent);

                if (targetGO != null)
                {
                    if (wasCreated) ctx.goCreated++;

                    FieldBase field = targetGO.GetComponent(type) as FieldBase;
                    if (field == null)
                    {
                        field = Undo.AddComponent(targetGO, type) as FieldBase;
                        ctx.componentsAdded++;
                    }

                    if (field != null)
                    {
                        if (ctx.texFieldCache.TryGetValue(type, out FieldInfo fi))
                            fi.SetValue(field, dummyManager.GetOrCreateForClass(type.Name));

                        EditorUtility.SetDirty(field);
                        EditorUtility.SetDirty(targetGO);
                        ctx.newFields.Add(field);
                        ctx.addedTypes.Add(type);
                    }
                }
            }
            catch (Exception e)
            {
                SDebug.LogError($"[LunaFieldEditor] Failed to create {type.Name}: {e.Message}");
            }

            ctx.index++;
            EditorApplication.delayCall += () => GenerateFieldStep(ctx);
        }

        #endregion

        #region Append Fields

        private void AppendFields(LunaFieldController controller)
        {
            List<Type> typesToGenerate = GetFilteredFieldTypes();
            if (typesToGenerate.Count == 0)
            {
                EditorUtility.DisplayDialog("No Field Types", "No field types found.", "OK");
                return;
            }

            var existingTypes = new HashSet<Type>();
            if (controller.fields != null)
            {
                for (int i = 0; i < controller.fields.Length; i++)
                {
                    if (controller.fields[i] != null)
                        existingTypes.Add(controller.fields[i].GetType());
                }
            }

            var newTypes = new List<Type>(typesToGenerate.Count);
            for (int i = 0; i < typesToGenerate.Count; i++)
            {
                if (!existingTypes.Contains(typesToGenerate[i]))
                    newTypes.Add(typesToGenerate[i]);
            }

            if (newTypes.Count == 0)
            {
                EditorUtility.DisplayDialog("Nothing to Append",
                    "All types from the current folder are already in the field list.", "OK");
                return;
            }

            string genFolderName = GetGenerateSubFolderName();
            string folderLabel = !string.IsNullOrEmpty(genFolderName) ? genFolderName : "all folders";

            if (!EditorUtility.DisplayDialog("Append Fields",
                $"Will append {newTypes.Count} new field type(s) from '{folderLabel}'\n" +
                $"Existing {controller.FieldCount} field(s) will be kept.\n\nContinue?",
                "Yes, Append", "Cancel"))
                return;

            dummyManager.EnsureFolderExists();

            Transform effectiveParent = parentTransformForGenerate != null ? parentTransformForGenerate : controller.transform;
            if (!string.IsNullOrEmpty(genFolderName))
                effectiveParent = SubfolderResolver.ResolveSubFolderParent(genFolderName, effectiveParent);
            if (createFieldType == FieldObjectType.RawImage)
                effectiveParent = SubfolderResolver.EnsureParentInsideCanvas(effectiveParent);

            Dictionary<string, Transform> gameObjectLookup = BuildGameObjectLookup(controller, effectiveParent);

            var texFieldCache = new Dictionary<Type, FieldInfo>(newTypes.Count);
            for (int i = 0; i < newTypes.Count; i++)
            {
                FieldInfo fi = newTypes[i].GetField("tex", BindingFlags.Public | BindingFlags.Instance);
                if (fi != null && fi.FieldType == typeof(Texture2D))
                    texFieldCache[newTypes[i]] = fi;
            }

            LunaProgressWindow.Show("Appending Fields", newTypes.Count);

            var ctx = new AppendContext
            {
                controller = controller,
                types = newTypes,
                allFields = new List<FieldBase>(controller.fields ?? new FieldBase[0]),
                texFieldCache = texFieldCache,
                gameObjectLookup = gameObjectLookup,
                effectiveParent = effectiveParent,
                index = 0,
                componentsAdded = 0,
                goCreated = 0,
            };

            EditorApplication.delayCall += () => AppendFieldStep(ctx);
        }

        private class AppendContext
        {
            public LunaFieldController controller;
            public List<Type> types;
            public List<FieldBase> allFields;
            public Dictionary<Type, FieldInfo> texFieldCache;
            public Dictionary<string, Transform> gameObjectLookup;
            public Transform effectiveParent;
            public int index;
            public int componentsAdded;
            public int goCreated;
        }

        private void AppendFieldStep(AppendContext ctx)
        {
            if (ctx.index >= ctx.types.Count)
            {
                ctx.controller.fields = ctx.allFields.ToArray();
                EditorUtility.SetDirty(ctx.controller);
                LunaProgressWindow.Update("Done!", 1f, ctx.types.Count);
                LunaProgressWindow.Dismiss();
                EditorUtility.DisplayDialog("Append Complete",
                    $"Fields appended: {ctx.types.Count}\n" +
                    $"GameObjects created: {ctx.goCreated}\n" +
                    $"Components added: {ctx.componentsAdded}\n" +
                    $"Total fields: {ctx.allFields.Count}", "OK");
                return;
            }

            Type type = ctx.types[ctx.index];
            float progress = (float)ctx.index / ctx.types.Count;
            LunaProgressWindow.Update($"{type.Name}", progress, ctx.index + 1);

            try
            {
                GameObject targetGO = FindOrCreateGameObjectForType(
                    ctx.controller, type, ctx.gameObjectLookup, out bool wasCreated, ctx.effectiveParent);

                if (targetGO != null)
                {
                    if (wasCreated) ctx.goCreated++;

                    FieldBase field = targetGO.GetComponent(type) as FieldBase;
                    if (field == null)
                    {
                        field = Undo.AddComponent(targetGO, type) as FieldBase;
                        ctx.componentsAdded++;
                    }

                    if (field != null)
                    {
                        if (ctx.texFieldCache.TryGetValue(type, out FieldInfo fi))
                            fi.SetValue(field, dummyManager.GetOrCreateForClass(type.Name));

                        EditorUtility.SetDirty(field);
                        EditorUtility.SetDirty(targetGO);
                        ctx.allFields.Add(field);
                    }
                }
            }
            catch (Exception e)
            {
                SDebug.LogError($"[LunaFieldEditor] Failed to append {type.Name}: {e.Message}");
            }

            ctx.index++;
            EditorApplication.delayCall += () => AppendFieldStep(ctx);
        }

        #endregion

        #region Preview

        private void GenerateAllFieldsWithPreview(LunaFieldController controller)
        {
            List<Type> typesToGenerate = GetFilteredFieldTypes();
            if (typesToGenerate.Count == 0)
            {
                EditorUtility.DisplayDialog("No Field Types", "No field types found.", "OK");
                return;
            }

            EditorUtility.DisplayProgressBar("Scanning", "Scanning scene...", 0.5f);

            Transform searchRoot = parentTransformForGenerate;
            GameObject[] allObjects;
            if (searchRoot != null)
            {
                Transform[] children = searchRoot.GetComponentsInChildren<Transform>(true);
                allObjects = new GameObject[children.Length];
                for (int i = 0; i < children.Length; i++)
                    allObjects[i] = children[i].gameObject;
            }
            else
            {
                allObjects = GameObject.FindObjectsOfType<GameObject>(true);
            }

            int existingObjects = 0;
            int existingWithComponents = 0;

            for (int t = 0; t < typesToGenerate.Count; t++)
            {
                Type fieldType = typesToGenerate[t];
                string typeName = fieldType.Name;
                bool found = false;

                for (int g = 0; g < allObjects.Length; g++)
                {
                    if (allObjects[g].name.Equals(typeName, StringComparison.OrdinalIgnoreCase))
                    {
                        existingObjects++;
                        if (allObjects[g].GetComponent(fieldType) != null)
                            existingWithComponents++;
                        found = true;
                        break;
                    }
                }
            }

            int toCreate = typesToGenerate.Count - existingObjects;
            EditorUtility.ClearProgressBar();

            if (!EditorUtility.DisplayDialog("Preview",
                $"Total types: {typesToGenerate.Count}\n" +
                $"Existing GameObjects: {existingObjects}\n" +
                $"  With component: {existingWithComponents}\n" +
                $"  Without component: {existingObjects - existingWithComponents}\n" +
                $"Will create: {toCreate} new GameObject(s)\n\nContinue?",
                "Yes, Generate", "Cancel"))
                return;

            GenerateAllFields(controller);
        }

        #endregion

        #region Validate & Merge

        private void ValidateDummies(LunaFieldController controller)
        {
            if (controller.fields == null || controller.fields.Length == 0)
            {
                EditorUtility.DisplayDialog("Validate Dummies", "No fields to validate.", "OK");
                return;
            }

            dummyManager.EnsureFolderExists();

            int totalFields = controller.fields.Length;
            int alreadyCorrect = 0;
            int fixed_ = 0;
            int skippedNoTex = 0;
            var report = new System.Text.StringBuilder();

            for (int i = 0; i < totalFields; i++)
            {
                FieldBase field = controller.fields[i];
                if (field == null) continue;

                Type fieldType = field.GetType();
                string className = fieldType.Name;

                FieldInfo texField = fieldType.GetField("tex", BindingFlags.Public | BindingFlags.Instance);
                if (texField == null || texField.FieldType != typeof(Texture2D))
                {
                    skippedNoTex++;
                    continue;
                }

                Texture2D currentTex = texField.GetValue(field) as Texture2D;
                bool isCorrect = currentTex != null
                    && Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(currentTex)) == className;

                if (isCorrect)
                {
                    alreadyCorrect++;
                    continue;
                }

                Texture2D correctTex = dummyManager.GetOrCreateForClass(className);
                if (correctTex == null)
                {
                    report.AppendLine($"{className}: Failed to create dummy texture");
                    continue;
                }

                Undo.RecordObject(field, "Validate Dummy Texture");
                texField.SetValue(field, correctTex);

                RawImage rawImage = field.gameObject.GetComponent<RawImage>();
                if (rawImage != null)
                {
                    Undo.RecordObject(rawImage, "Validate Dummy RawImage");
                    rawImage.texture = correctTex;
                }

                SpriteRenderer sr = field.gameObject.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Sprite sprite = dummyManager.CreateSpriteFromTexture(correctTex);
                    if (sprite != null)
                    {
                        Undo.RecordObject(sr, "Validate Dummy SpriteRenderer");
                        sr.sprite = sprite;
                    }
                }

                EditorUtility.SetDirty(field);
                EditorUtility.SetDirty(field.gameObject);
                fixed_++;

                string oldName = currentTex != null ? currentTex.name : "(none)";
                report.AppendLine($"{className}: {oldName} → {correctTex.name}");
            }

            string summary = $"Total fields: {totalFields}\n" +
                              $"Already correct: {alreadyCorrect}\n" +
                              $"Fixed: {fixed_}\n" +
                              $"Skipped (no tex): {skippedNoTex}\n";

            if (report.Length > 0)
                summary += $"\nDetails:\n{report}";

            EditorUtility.DisplayDialog("Validate Dummies — Complete", summary, "OK");
        }

        /// <summary>
        /// Validate None Only: only assign dummy texture to fields where tex is currently null.
        /// </summary>
        private void ValidateDummiesNoneOnly(LunaFieldController controller)
        {
            if (controller.fields == null || controller.fields.Length == 0)
            {
                EditorUtility.DisplayDialog("Validate Dummies", "No fields to validate.", "OK");
                return;
            }

            dummyManager.EnsureFolderExists();

            int totalFields = controller.fields.Length;
            int filled = 0;
            int skipped = 0;

            for (int i = 0; i < totalFields; i++)
            {
                FieldBase field = controller.fields[i];
                if (field == null) continue;

                Type fieldType = field.GetType();
                string className = fieldType.Name;

                FieldInfo texField = fieldType.GetField("tex", BindingFlags.Public | BindingFlags.Instance);
                if (texField == null || texField.FieldType != typeof(Texture2D)) { skipped++; continue; }

                Texture2D currentTex = texField.GetValue(field) as Texture2D;
                if (currentTex != null) { skipped++; continue; }

                Texture2D correctTex = dummyManager.GetOrCreateForClass(className);
                if (correctTex == null) continue;

                Undo.RecordObject(field, "Fill Empty Dummy");
                texField.SetValue(field, correctTex);
                AssignTextureToVisual(field, correctTex);
                EditorUtility.SetDirty(field);
                filled++;
            }

            EditorUtility.DisplayDialog("Validate None — Complete",
                $"Total: {totalFields}\nFilled empty: {filled}\nSkipped (already set or no tex field): {skipped}", "OK");
        }

        /// <summary>
        /// Force Validate: overwrite all tex fields with dummy named after the script class.
        /// </summary>
        private void ValidateDummiesForce(LunaFieldController controller)
        {
            if (controller.fields == null || controller.fields.Length == 0)
            {
                EditorUtility.DisplayDialog("Validate Dummies", "No fields to validate.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Force Validate",
                "This will OVERWRITE all texture fields with dummy textures named after each script class.\n\nContinue?",
                "Yes, Force", "Cancel"))
                return;

            dummyManager.EnsureFolderExists();

            int totalFields = controller.fields.Length;
            int forced = 0;
            int skipped = 0;

            for (int i = 0; i < totalFields; i++)
            {
                FieldBase field = controller.fields[i];
                if (field == null) continue;

                Type fieldType = field.GetType();
                string className = fieldType.Name;

                FieldInfo texField = fieldType.GetField("tex", BindingFlags.Public | BindingFlags.Instance);
                if (texField == null || texField.FieldType != typeof(Texture2D)) { skipped++; continue; }

                Texture2D correctTex = dummyManager.GetOrCreateForClass(className);
                if (correctTex == null) { skipped++; continue; }

                Undo.RecordObject(field, "Force Validate Dummy");
                texField.SetValue(field, correctTex);
                AssignTextureToVisual(field, correctTex);
                EditorUtility.SetDirty(field);
                EditorUtility.SetDirty(field.gameObject);
                forced++;
            }

            EditorUtility.DisplayDialog("Force Validate — Complete",
                $"Total: {totalFields}\nForce updated: {forced}\nSkipped (no tex field): {skipped}", "OK");
        }

        /// <summary>
        /// Validate a single field's dummy texture (None only mode).
        /// Returns true if texture was assigned.
        /// </summary>
        internal bool ValidateSingleFieldNone(FieldBase field)
        {
            if (field == null) return false;

            Type fieldType = field.GetType();
            FieldInfo texField = fieldType.GetField("tex", BindingFlags.Public | BindingFlags.Instance);
            if (texField == null || texField.FieldType != typeof(Texture2D)) return false;

            Texture2D currentTex = texField.GetValue(field) as Texture2D;
            if (currentTex != null) return false;

            dummyManager.EnsureFolderExists();
            Texture2D correctTex = dummyManager.GetOrCreateForClass(fieldType.Name);
            if (correctTex == null) return false;

            Undo.RecordObject(field, "Validate Dummy");
            texField.SetValue(field, correctTex);
            AssignTextureToVisual(field, correctTex);
            EditorUtility.SetDirty(field);
            return true;
        }

        /// <summary>
        /// Force validate a single field's dummy texture.
        /// Returns true if texture was assigned.
        /// </summary>
        internal bool ValidateSingleFieldForce(FieldBase field)
        {
            if (field == null) return false;

            Type fieldType = field.GetType();
            FieldInfo texField = fieldType.GetField("tex", BindingFlags.Public | BindingFlags.Instance);
            if (texField == null || texField.FieldType != typeof(Texture2D)) return false;

            dummyManager.EnsureFolderExists();
            Texture2D correctTex = dummyManager.GetOrCreateForClass(fieldType.Name);
            if (correctTex == null) return false;

            Undo.RecordObject(field, "Force Validate Dummy");
            texField.SetValue(field, correctTex);
            AssignTextureToVisual(field, correctTex);
            EditorUtility.SetDirty(field);
            EditorUtility.SetDirty(field.gameObject);
            return true;
        }

        /// <summary>Assign texture to RawImage or SpriteRenderer on the field's GameObject.</summary>
        private void AssignTextureToVisual(FieldBase field, Texture2D tex)
        {
            RawImage rawImage = field.gameObject.GetComponent<RawImage>();
            if (rawImage != null)
            {
                Undo.RecordObject(rawImage, "Assign Dummy Visual");
                rawImage.texture = tex;
            }

            SpriteRenderer sr = field.gameObject.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Sprite sprite = dummyManager.CreateSpriteFromTexture(tex);
                if (sprite != null)
                {
                    Undo.RecordObject(sr, "Assign Dummy Visual");
                    sr.sprite = sprite;
                }
            }
        }

        private void MergeDuplicateParentGroups(LunaFieldController controller)
        {
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

            var groups = new Dictionary<string, List<GameObject>>();
            foreach (GameObject go in allObjects)
            {
                if (!go.scene.IsValid()) continue;
                if (go.hideFlags != HideFlags.None) continue;
                int parentID = go.transform.parent != null ? go.transform.parent.gameObject.GetInstanceID() : -1;
                string key = $"{parentID}::{go.name}";
                if (!groups.TryGetValue(key, out var list))
                {
                    list = new List<GameObject>(4);
                    groups[key] = list;
                }
                list.Add(go);
            }

            var dupGroups = new List<List<GameObject>>(8);
            foreach (var kvp in groups)
            {
                if (kvp.Value.Count > 1)
                    dupGroups.Add(kvp.Value);
            }

            if (dupGroups.Count == 0)
            {
                EditorUtility.DisplayDialog("No Duplicates", "No duplicate parent groups found.", "OK");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Found {dupGroups.Count} duplicate group(s):\n");
            int totalToDestroy = 0;
            foreach (var dups in dupGroups)
            {
                string parentName = dups[0].transform.parent != null ? dups[0].transform.parent.name : "<root>";
                sb.AppendLine($"• \"{dups[0].name}\" under [{parentName}] — {dups.Count} copies");
                totalToDestroy += dups.Count - 1;
            }
            sb.AppendLine($"\nTotal removed: {totalToDestroy} object(s).");

            if (!EditorUtility.DisplayDialog("Merge Duplicate Groups", sb.ToString(), "Merge", "Cancel"))
                return;

            foreach (var dups in dupGroups)
            {
                GameObject keeper = dups[0];
                Undo.RegisterFullObjectHierarchyUndo(keeper, "Merge Duplicate Groups");

                for (int i = 1; i < dups.Count; i++)
                {
                    GameObject dup = dups[i];
                    if (dup == null) continue;

                    while (dup.transform.childCount > 0)
                    {
                        Transform child = dup.transform.GetChild(0);
                        Undo.SetTransformParent(child, keeper.transform, $"Merge child {child.name}");
                    }

                    Undo.DestroyObjectImmediate(dup);
                }
            }

            EditorUtility.SetDirty(controller);
            EditorUtility.DisplayDialog("Merge Complete", $"Merged {dupGroups.Count} group(s).", "OK");
        }

        #endregion

        #region GameObject Helpers

        private GameObject FindOrCreateGameObjectForType(
            LunaFieldController controller, Type type,
            Dictionary<string, Transform> gameObjectLookup,
            out bool wasCreated, Transform parentOverride = null)
        {
            wasCreated = false;
            string typeName = type.Name;

            if (gameObjectLookup.TryGetValue(typeName, out Transform found))
                return found.gameObject;

            string[] variations = GenerateNameVariations(typeName);
            for (int i = 0; i < variations.Length; i++)
            {
                if (gameObjectLookup.TryGetValue(variations[i], out Transform variantTransform))
                    return variantTransform.gameObject;
            }

            FieldBase existingComponent = FindObjectOfType(type) as FieldBase;
            if (existingComponent != null)
            {
                gameObjectLookup[existingComponent.gameObject.name] = existingComponent.transform;
                return existingComponent.gameObject;
            }

            Transform parentToUse = parentOverride != null ? parentOverride : controller.transform;
            if (createFieldType == FieldObjectType.RawImage)
                parentToUse = SubfolderResolver.EnsureParentInsideCanvas(parentToUse);

            GameObject newGO = new GameObject(typeName);
            newGO.transform.SetParent(parentToUse, false);
            newGO.transform.localPosition = Vector3.zero;
            newGO.transform.localRotation = Quaternion.identity;
            newGO.transform.localScale = Vector3.one;

            Texture2D dummyTex = dummyManager.GetOrCreateForClass(typeName);
            if (createFieldType == FieldObjectType.RawImage)
                SetupRawImageOnNewObject(newGO, dummyTex);
            else if (createFieldType == FieldObjectType.SpriteRenderer)
                CreateSpriteRendererComponent(newGO, dummyTex);

            Undo.RegisterCreatedObjectUndo(newGO, "Generate Field GameObject");
            wasCreated = true;
            gameObjectLookup[typeName] = newGO.transform;

            return newGO;
        }

        private Dictionary<string, Transform> BuildGameObjectLookup(LunaFieldController controller, Transform parentTransform)
        {
            var lookup = new Dictionary<string, Transform>(StringComparer.OrdinalIgnoreCase);

            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>(true);
            for (int i = 0; i < allObjects.Length; i++)
            {
                string name = allObjects[i].name;
                if (!lookup.ContainsKey(name))
                    lookup[name] = allObjects[i].transform;
            }

            if (parentTransform != null)
            {
                Transform[] children = parentTransform.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < children.Length; i++)
                {
                    if (children[i] == parentTransform) continue;
                    lookup[children[i].gameObject.name] = children[i];
                }
            }

            return lookup;
        }

        private string[] GenerateNameVariations(string baseName)
        {
            return new[]
            {
                baseName,
                baseName.Replace("Field", ""),
                baseName.Replace("_LunaField", ""),
                baseName.Replace("LunaField", ""),
                baseName.ToLower(),
                baseName.Replace("_", ""),
                baseName.Replace("-", "")
            }.Distinct().ToArray();
        }

        internal void SetupRawImageOnNewObject(GameObject go, Texture2D texture)
        {
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(200, 200);
            rt.anchoredPosition = Vector2.zero;

            if (go.GetComponent<RawImage>() == null)
            {
                RawImage rawImage = go.AddComponent<RawImage>();
                rawImage.texture = texture;
                rawImage.raycastTarget = false;
            }
        }

        internal void CreateSpriteRendererComponent(GameObject go, Texture2D texture)
        {
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            if (texture != null)
                sr.sprite = dummyManager.CreateSpriteFromTexture(texture);
            sr.sortingOrder = 0;
        }

        #endregion
    }
}
