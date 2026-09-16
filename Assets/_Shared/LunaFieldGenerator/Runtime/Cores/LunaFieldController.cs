using System;
using System.Collections.Generic;
using Amanotes.Core;
using OrientationTracking.Core;
using UnityEngine;

namespace Amanotes.LunaFieldGenerator
{
    /// <summary>
    /// Manages a collection of FieldBase components.
    /// Applies orientation and texture to all registered fields on start and orientation change.
    /// </summary>
    public class LunaFieldController : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField]
        public FieldBase[] fields;

        #endregion

        #region Unity Lifecycle

        void Start()
        {
            ApplyAllFields();
        }

        void OnEnable()
        {
            EventBus.Subscribe<OnOrientationChangedEvent>(OnOrientationChanged);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<OnOrientationChangedEvent>(OnOrientationChanged);
        }

        #endregion

        #region Public API

        /// <summary>Apply orientation and texture to all registered fields.</summary>
        public void ApplyAllFields()
        {
            if (fields == null || fields.Length == 0)
            {
                SDebug.LogWarning("[LunaFieldController] No fields registered.");
                return;
            }

            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i] == null) continue;
                fields[i].ApplyOrientation();
                fields[i].ApplyTexture();
            }
        }

        /// <summary>Add a field to the list. Returns false if already exists.</summary>
        public bool AddField(FieldBase field)
        {
            if (field == null) return false;
            if (ContainsField(field)) return false;

            var list = fields != null ? new List<FieldBase>(fields) : new List<FieldBase>(8);
            list.Add(field);
            fields = list.ToArray();
            return true;
        }

        /// <summary>Remove a field by reference. Returns false if not found.</summary>
        public bool RemoveField(FieldBase field)
        {
            if (field == null || fields == null) return false;

            var list = new List<FieldBase>(fields);
            bool removed = list.Remove(field);
            if (removed) fields = list.ToArray();
            return removed;
        }

        /// <summary>Remove field at index.</summary>
        public void RemoveFieldAt(int index)
        {
            if (fields == null || index < 0 || index >= fields.Length) return;

            var list = new List<FieldBase>(fields);
            list.RemoveAt(index);
            fields = list.ToArray();
        }

        /// <summary>Remove all null/destroyed references from the array.</summary>
        public int RemoveNullFields()
        {
            if (fields == null) return 0;

            int originalCount = fields.Length;
            fields = Array.FindAll(fields, f => f != null);
            return originalCount - fields.Length;
        }

        /// <summary>Check if a field is already registered.</summary>
        public bool ContainsField(FieldBase field)
        {
            if (fields == null) return false;
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i] == field) return true;
            }
            return false;
        }

        /// <summary>Check if a field of the given type is already registered.</summary>
        public bool ContainsFieldOfType(Type fieldType)
        {
            if (fields == null) return false;
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i] != null && fields[i].GetType() == fieldType) return true;
            }
            return false;
        }

        /// <summary>Get field by type. Returns null if not found.</summary>
        public FieldBase GetFieldByType(Type fieldType)
        {
            if (fields == null) return null;
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i] != null && fields[i].GetType() == fieldType)
                    return fields[i];
            }
            return null;
        }

        /// <summary>Get field by type (generic).</summary>
        public T GetField<T>() where T : FieldBase
        {
            if (fields == null) return null;
            for (int i = 0; i < fields.Length; i++)
            {
                var typed = fields[i] as T;
                if (typed != null) return typed;
            }
            return null;
        }

        /// <summary>Total registered field count (including nulls).</summary>
        public int FieldCount => fields?.Length ?? 0;

        #endregion

        #region Private

        private void OnOrientationChanged(OnOrientationChangedEvent e)
        {
            ApplyAllFields();
        }

        #endregion
    }
}
