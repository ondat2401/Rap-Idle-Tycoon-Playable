using UnityEngine;

namespace Amanotes.LunaFieldGenerator
{
    /// <summary>
    /// Base class for all Luna Field components.
    /// Subclasses define orientation-specific transform data and texture assignments.
    /// </summary>
    public class FieldBase : MonoBehaviour
    {
        #region Core

        public Transform targetTransform => this.transform;

        public virtual void ApplyOrientation() { }
        public virtual void ApplyTexture() { }

        #endregion

        #region Metadata (override in subclass or detected by Editor via reflection)

        /// <summary>Whether this field has position data (phone/tablet × portrait/landscape).</summary>
        public virtual bool HasPosition => false;

        /// <summary>Whether this field has scale data.</summary>
        public virtual bool HasScale => false;

        /// <summary>Whether this field has rotation data.</summary>
        public virtual bool HasRotation => false;

        /// <summary>Whether this field has a texture assignment.</summary>
        public virtual bool HasTexture => false;

        #endregion

        #region Capture API (Editor-friendly)

        /// <summary>
        /// Capture current transform values into the specified orientation slot.
        /// Override in subclass to store values into the correct fields.
        /// </summary>
        public virtual void CaptureSlot(bool isPhone, bool isPortrait)
        {
            CaptureSlotFrom(targetTransform, isPhone, isPortrait);
        }

        /// <summary>
        /// Capture values from a specific transform into the specified orientation slot.
        /// </summary>
        public virtual void CaptureSlotFrom(Transform source, bool isPhone, bool isPortrait) { }

        /// <summary>
        /// Capture current transform values into all 4 orientation slots.
        /// </summary>
        public virtual void CaptureAllSlots()
        {
            CaptureSlot(true, true);   // Phone Portrait
            CaptureSlot(true, false);  // Phone Landscape
            CaptureSlot(false, true);  // Tablet Portrait
            CaptureSlot(false, false); // Tablet Landscape
        }

        /// <summary>
        /// Get stored position for the specified slot. Returns Vector2.zero if not available.
        /// </summary>
        public virtual Vector2 GetStoredPosition(bool isPhone, bool isPortrait) => Vector2.zero;

        /// <summary>
        /// Get stored scale for the specified slot. Returns Vector2.one if not available.
        /// </summary>
        public virtual Vector2 GetStoredScale(bool isPhone, bool isPortrait) => Vector2.one;

        /// <summary>
        /// Get stored rotation for the specified slot. Returns 0 if not available.
        /// </summary>
        public virtual float GetStoredRotation(bool isPhone, bool isPortrait) => 0f;

        #endregion
    }
}
