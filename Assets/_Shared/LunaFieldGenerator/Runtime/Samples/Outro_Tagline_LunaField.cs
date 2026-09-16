using UnityEngine;
using OrientationTracking.Core;
using UnityEngine.UI;
using Amanotes.LunaFieldGenerator;

namespace LunaField
{
    public class Outro_Tagline_LunaField : FieldBase
    {
        [Header("Texture")]
        [LunaPlaygroundAsset("Texture", 0, "Outro_Tagline")]
        public Texture2D tex = null;

        [Header("Phone Position")]
        [LunaPlaygroundField("Phone_Position_PT", 1, "Outro_Tagline")]
        public Vector2 phone_Position_PT = Vector2.zero;
        [LunaPlaygroundField("Phone_Position_LS", 2, "Outro_Tagline")]
        public Vector2 phone_Position_LS = Vector2.zero;

        [Header("Phone Scale")]
        [LunaPlaygroundField("Phone_Scale_PT", 3, "Outro_Tagline")]
        public Vector2 phone_Scale_PT = Vector2.one;
        [LunaPlaygroundField("Phone_Scale_LS", 4, "Outro_Tagline")]
        public Vector2 phone_Scale_LS = Vector2.one;

        [Header("Tablet Position")]
        [LunaPlaygroundField("Tablet_Position_PT", 11, "Outro_Tagline")]
        public Vector2 tablet_Position_PT = Vector2.zero;
        [LunaPlaygroundField("Tablet_Position_LS", 12, "Outro_Tagline")]
        public Vector2 tablet_Position_LS = Vector2.zero;

        [Header("Tablet Scale")]
        [LunaPlaygroundField("Tablet_Scale_PT", 13, "Outro_Tagline")]
        public Vector2 tablet_Scale_PT = Vector2.one;
        [LunaPlaygroundField("Tablet_Scale_LS", 14, "Outro_Tagline")]
        public Vector2 tablet_Scale_LS = Vector2.one;

        public override void ApplyOrientation()
        {
            bool isPhone = OrientationTracker.Instance.IsPhone;
            bool isPortrait = OrientationTracker.Instance.IsPortrait;

            Vector2 pos, scale;
            if (isPhone)
            {
                pos = isPortrait ? phone_Position_PT : phone_Position_LS;
                scale = isPortrait ? phone_Scale_PT : phone_Scale_LS;
            }
            else
            {
                pos = isPortrait ? tablet_Position_PT : tablet_Position_LS;
                scale = isPortrait ? tablet_Scale_PT : tablet_Scale_LS;
            }

            targetTransform.localPosition = new Vector3(pos.x, pos.y, 0);
            targetTransform.localScale = new Vector3(scale.x, scale.y, 1);
        }

        public override void ApplyTexture()
        {
            if (tex == null) return;
            RawImage img = GetComponent<RawImage>();
            if (img != null)
            {
                img.texture = tex;
                ((RectTransform)img.transform).sizeDelta = new Vector2(tex.width, tex.height);
            }
        }
    }
}
