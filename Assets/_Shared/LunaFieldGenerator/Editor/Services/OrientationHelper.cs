using UnityEditor;
using UnityEngine;
using OrientationTracking.Core;

namespace Amanotes.LunaFieldGenerator.Editor
{
    /// <summary>
    /// Shared orientation detection utilities for Editor tools.
    /// Centralizes device/orientation detection logic used by multiple editors.
    /// </summary>
    public static class OrientationHelper
    {
        private const string PrefKeyDeviceOverride = "LunaFieldEditor_DeviceOverride";

        /// <summary>Get current Game View size.</summary>
        public static Vector2 GetGameViewSize()
        {
            return Handles.GetMainGameViewSize();
        }

        /// <summary>Detect if current Game View is portrait orientation.</summary>
        public static bool IsPortrait()
        {
            Vector2 size = GetGameViewSize();
            return size.y > size.x;
        }

        /// <summary>Detect if current device is phone (from tracker or editor override).</summary>
        public static bool IsPhone()
        {
            if (OrientationTracker.IsInitialized && OrientationTracker.Instance != null)
                return OrientationTracker.Instance.IsPhone;
            return EditorPrefs.GetBool(PrefKeyDeviceOverride, true);
        }

        /// <summary>Whether OrientationTracker is ready.</summary>
        public static bool IsTrackerReady =>
            OrientationTracker.IsInitialized && OrientationTracker.Instance != null;

        /// <summary>Get/set editor device override (phone vs tablet).</summary>
        public static bool EditorDeviceIsPhone
        {
            get => EditorPrefs.GetBool(PrefKeyDeviceOverride, true);
            set => EditorPrefs.SetBool(PrefKeyDeviceOverride, value);
        }

        /// <summary>Refresh tracker state if available.</summary>
        public static void RefreshTracker()
        {
            if (IsTrackerReady)
                OrientationTracker.Instance.RefreshState();
        }

        /// <summary>Get position field name for the given device/orientation slot.</summary>
        public static string GetPositionFieldName(bool isPhone, bool isPortrait)
        {
            if (isPhone) return isPortrait ? "phone_Position_PT" : "phone_Position_LS";
            return isPortrait ? "tablet_Position_PT" : "tablet_Position_LS";
        }

        /// <summary>Get scale field name for the given device/orientation slot.</summary>
        public static string GetScaleFieldName(bool isPhone, bool isPortrait)
        {
            if (isPhone) return isPortrait ? "phone_Scale_PT" : "phone_Scale_LS";
            return isPortrait ? "tablet_Scale_PT" : "tablet_Scale_LS";
        }

        /// <summary>Get rotation field name for the given device/orientation slot.</summary>
        public static string GetRotationFieldName(bool isPhone, bool isPortrait)
        {
            if (isPhone) return isPortrait ? "phone_Rotation_PT" : "phone_Rotation_LS";
            return isPortrait ? "tablet_Rotation_PT" : "tablet_Rotation_LS";
        }

        /// <summary>Get human-readable slot label.</summary>
        public static string GetSlotLabel(bool isPhone, bool isPortrait)
        {
            string device = isPhone ? "Phone" : "Tablet";
            string orient = isPortrait ? "Portrait" : "Landscape";
            return $"{device} {orient}";
        }
    }
}
