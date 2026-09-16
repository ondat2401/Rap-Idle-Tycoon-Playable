using UnityEngine;
using OrientationTracking.Core;
using UnityEngine.UI;
using Amanotes.Core;
using Amanotes.LunaFieldGenerator;

namespace LunaField
{
    public class Revive_Tagline_LunaField : FieldBase
    {
        [Header("Texture")]
        [LunaPlaygroundAsset("Texture", 0, "Revive_Tagline")]
        public Texture2D tex = null;

        [Header("Phone Position")]
        [LunaPlaygroundField("Phone_Position_PT", 1, "Revive_Tagline")]
        public Vector2 phone_Position_PT = Vector2.zero;
        [LunaPlaygroundField("Phone_Position_LS", 2, "Revive_Tagline")]
        public Vector2 phone_Position_LS = Vector2.zero;

        [Header("Tablet Position")]
        [LunaPlaygroundField("Tablet_Position_PT", 11, "Revive_Tagline")]
        public Vector2 tablet_Position_PT = Vector2.zero;
        [LunaPlaygroundField("Tablet_Position_LS", 12, "Revive_Tagline")]
        public Vector2 tablet_Position_LS = Vector2.zero;

        [Header("Phone Scale")]
        [LunaPlaygroundField("Phone_Scale_PT", 21, "Revive_Tagline")]
        public Vector2 phone_Scale_PT = Vector2.one;
        [LunaPlaygroundField("Phone_Scale_LS", 22, "Revive_Tagline")]
        public Vector2 phone_Scale_LS = Vector2.one;

        [Header("Tablet Scale")]
        [LunaPlaygroundField("Tablet_Scale_PT", 31, "Revive_Tagline")]
        public Vector2 tablet_Scale_PT = Vector2.one;
        [LunaPlaygroundField("Tablet_Scale_LS", 32, "Revive_Tagline")]
        public Vector2 tablet_Scale_LS = Vector2.one;


        #region Metadata

        public override bool HasTexture => true;
        public override bool HasPosition => true;
        public override bool HasScale => true;

        #endregion

        #region Apply

        public override void ApplyOrientation()
        {
            bool isPhone = OrientationTracker.Instance.IsPhone;
            bool isPortrait = OrientationTracker.Instance.IsPortrait;

            Vector2 pos;
            if (isPhone) pos = isPortrait ? phone_Position_PT : phone_Position_LS;
            else pos = isPortrait ? tablet_Position_PT : tablet_Position_LS;
            targetTransform.localPosition = new Vector3(pos.x, pos.y, 0);

            Vector2 scale;
            if (isPhone) scale = isPortrait ? phone_Scale_PT : phone_Scale_LS;
            else scale = isPortrait ? tablet_Scale_PT : tablet_Scale_LS;
            targetTransform.localScale = new Vector3(scale.x, scale.y, 1);

        }

        public override void ApplyTexture()
        {
            if (tex == null) return;
            RawImage img = GetComponent<RawImage>();
            if (img != null) img.texture = tex;
            img.rectTransform.sizeDelta = new Vector2(tex.width, tex.height);
        }

        #endregion

        #region Capture

        public override void CaptureSlotFrom(Transform source, bool isPhone, bool isPortrait)
        {
            if (source == null) return;

            Vector2 pos = new Vector2(source.localPosition.x, source.localPosition.y);
            if (isPhone) { if (isPortrait) phone_Position_PT = pos; else phone_Position_LS = pos; }
            else { if (isPortrait) tablet_Position_PT = pos; else tablet_Position_LS = pos; }

            Vector2 scl = new Vector2(source.localScale.x, source.localScale.y);
            if (isPhone) { if (isPortrait) phone_Scale_PT = scl; else phone_Scale_LS = scl; }
            else { if (isPortrait) tablet_Scale_PT = scl; else tablet_Scale_LS = scl; }

        }

        public override Vector2 GetStoredPosition(bool isPhone, bool isPortrait)
        {
            if (isPhone) return isPortrait ? phone_Position_PT : phone_Position_LS;
            return isPortrait ? tablet_Position_PT : tablet_Position_LS;
        }

        public override Vector2 GetStoredScale(bool isPhone, bool isPortrait)
        {
            if (isPhone) return isPortrait ? phone_Scale_PT : phone_Scale_LS;
            return isPortrait ? tablet_Scale_PT : tablet_Scale_LS;
        }

        #endregion
    }
}
