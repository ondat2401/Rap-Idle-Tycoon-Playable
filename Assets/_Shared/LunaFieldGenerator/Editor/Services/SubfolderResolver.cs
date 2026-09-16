using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Amanotes.LunaFieldGenerator.Editor
{
    /// <summary>
    /// Resolves parent transforms, subfolder groups, and Canvas hierarchy for field creation.
    /// </summary>
    public static class SubfolderResolver
    {
        /// <summary>
        /// Find or create a parent group GameObject for the given subfolder name.
        /// Searches scene-wide first to avoid duplicates.
        /// </summary>
        public static Transform ResolveSubFolderParent(string subFolderName, Transform parentTransform)
        {
            if (string.IsNullOrEmpty(subFolderName)) return parentTransform;

            // If the parent itself is already the target group, use it directly
            if (parentTransform != null &&
                parentTransform.name.Equals(subFolderName, StringComparison.OrdinalIgnoreCase))
                return parentTransform;

            // Search scene-wide first to avoid creating duplicate parent groups
            Transform found = FindTransformInSceneByName(subFolderName);
            if (found != null) return found;

            if (parentTransform == null)
                return CreateGroupGameObject(subFolderName, null);

            // Check direct children
            for (int i = 0; i < parentTransform.childCount; i++)
            {
                Transform child = parentTransform.GetChild(i);
                if (child.name.Equals(subFolderName, StringComparison.OrdinalIgnoreCase))
                    return child;
            }

            return CreateGroupGameObject(subFolderName, parentTransform);
        }

        /// <summary>
        /// Ensure the parent transform is inside a Canvas hierarchy.
        /// If not, find or create a Canvas and reparent accordingly.
        /// </summary>
        public static Transform EnsureParentInsideCanvas(Transform parent)
        {
            if (parent != null && parent.GetComponentInParent<Canvas>() != null)
                return parent;

            Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");
            }

            // Reparent group into Canvas if it exists but is outside Canvas
            if (parent != null && parent.GetComponentInParent<Canvas>() == null)
            {
                parent.SetParent(canvas.transform, false);
                if (parent.GetComponent<RectTransform>() == null)
                    parent.gameObject.AddComponent<RectTransform>();
            }

            return parent != null ? parent : canvas.transform;
        }

        /// <summary>
        /// Find a transform by name anywhere in the scene (case-insensitive).
        /// </summary>
        public static Transform FindTransformInSceneByName(string name)
        {
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.scene.IsValid() && go.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return go.transform;
            }
            return null;
        }

        /// <summary>
        /// Find a transform by name under a specific parent (case-insensitive).
        /// Falls back to scene-wide search.
        /// </summary>
        public static Transform FindTransformByName(string name, Transform parentTransform)
        {
            if (parentTransform != null)
            {
                Transform[] children = parentTransform.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < children.Length; i++)
                {
                    if (children[i].gameObject.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        return children[i];
                }
            }

            GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>(true);
            for (int i = 0; i < allObjects.Length; i++)
            {
                if (allObjects[i].name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return allObjects[i].transform;
            }

            return null;
        }

        /// <summary>Get the full hierarchy path of a GameObject.</summary>
        public static string GetGameObjectPath(GameObject obj)
        {
            if (obj == null) return "";
            string path = obj.name;
            Transform current = obj.transform.parent;
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        #region Private

        private static Transform CreateGroupGameObject(string groupName, Transform parent)
        {
            GameObject groupGO = new GameObject(groupName);
            if (parent != null) groupGO.transform.SetParent(parent, false);
            groupGO.transform.localPosition = Vector3.zero;
            groupGO.transform.localRotation = Quaternion.identity;
            groupGO.transform.localScale = Vector3.one;

            // Add RectTransform if inside Canvas hierarchy
            if (parent != null && parent.GetComponentInParent<Canvas>() != null)
            {
                if (groupGO.GetComponent<RectTransform>() == null)
                    groupGO.AddComponent<RectTransform>();
            }

            Undo.RegisterCreatedObjectUndo(groupGO, $"Create Group {groupName}");
            return groupGO.transform;
        }

        #endregion
    }
}
