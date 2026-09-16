using UnityEngine;
using OrientationTracking.Core;
using UnityEngine.UI;
using Amanotes.Core;
using Amanotes.LunaFieldGenerator;
using DG.Tweening;

namespace LunaField
{
    public class Intro_Hand_LunaField : FieldBase
    {
        [Header("Texture")]
        [LunaPlaygroundAsset("Texture", 0, "Intro_Hand")]
        public Texture2D tex = null;

        [Header("Phone Position")]
        [LunaPlaygroundField("Phone_Position_PT", 1, "Intro_Hand")]
        public Vector2 phone_Position_PT = Vector2.zero;
        [LunaPlaygroundField("Phone_Position_LS", 2, "Intro_Hand")]
        public Vector2 phone_Position_LS = Vector2.zero;

        [Header("Tablet Position")]
        [LunaPlaygroundField("Tablet_Position_PT", 11, "Intro_Hand")]
        public Vector2 tablet_Position_PT = Vector2.zero;
        [LunaPlaygroundField("Tablet_Position_LS", 12, "Intro_Hand")]
        public Vector2 tablet_Position_LS = Vector2.zero;

        [Header("Phone Scale")]
        [LunaPlaygroundField("Phone_Scale_PT", 21, "Intro_Hand")]
        public Vector2 phone_Scale_PT = Vector2.one;
        [LunaPlaygroundField("Phone_Scale_LS", 22, "Intro_Hand")]
        public Vector2 phone_Scale_LS = Vector2.one;

        [Header("Tablet Scale")]
        [LunaPlaygroundField("Tablet_Scale_PT", 31, "Intro_Hand")]
        public Vector2 tablet_Scale_PT = Vector2.one;
        [LunaPlaygroundField("Tablet_Scale_LS", 32, "Intro_Hand")]
        public Vector2 tablet_Scale_LS = Vector2.one;

        [Header("Phone Rotation")]
        [LunaPlaygroundField("Phone_Rotation_PT", 41, "Intro_Hand")]
        public float phone_Rotation_PT = 0f;
        [LunaPlaygroundField("Phone_Rotation_LS", 42, "Intro_Hand")]
        public float phone_Rotation_LS = 0f;

        [Header("Tablet Rotation")]
        [LunaPlaygroundField("Tablet_Rotation_PT", 51, "Intro_Hand")]
        public float tablet_Rotation_PT = 0f;
        [LunaPlaygroundField("Tablet_Rotation_LS", 52, "Intro_Hand")]
        public float tablet_Rotation_LS = 0f;


        #region Metadata

        public override bool HasTexture => true;
        public override bool HasPosition => true;
        public override bool HasScale => true;
        public override bool HasRotation => true;

        #endregion

        [Header("Pointing Animation")]
        [SerializeField] private Transform targetPosition;
        [SerializeField] private float pointDuration = 0.6f;
        [SerializeField] private Ease pointEase = Ease.InOutSine;
        [LunaPlaygroundField("Target Offset", 61, "Intro_Hand")]
        [SerializeField] private Vector2 targetOffset;
        private Tween handTween;
        private Vector3 basePosition;

        #region Apply

        public override void ApplyOrientation()
        {
            bool isPhone = OrientationTracker.Instance.IsPhone;
            bool isPortrait = OrientationTracker.Instance.IsPortrait;

            Vector2 pos;
            if (isPhone) pos = isPortrait ? phone_Position_PT : phone_Position_LS;
            else pos = isPortrait ? tablet_Position_PT : tablet_Position_LS;
            basePosition = new Vector3(pos.x, pos.y, 0);
            targetTransform.localPosition = basePosition;

            Vector2 scale;
            if (isPhone) scale = isPortrait ? phone_Scale_PT : phone_Scale_LS;
            else scale = isPortrait ? tablet_Scale_PT : tablet_Scale_LS;
            targetTransform.localScale = new Vector3(scale.x, scale.y, 1);

            float rot;
            if (isPhone) rot = isPortrait ? phone_Rotation_PT : phone_Rotation_LS;
            else rot = isPortrait ? tablet_Rotation_PT : tablet_Rotation_LS;
            targetTransform.localRotation = Quaternion.Euler(0, 0, rot);

            StartPointingLoop();
        }

        private void StartPointingLoop()
        {
            handTween?.Kill();
            targetTransform.localPosition = basePosition;

            if (targetPosition == null) return;

            Vector3 targetLocal = ToLocalPosition(targetPosition.position);
            var destination = new Vector3(targetLocal.x + targetOffset.x, targetLocal.y + targetOffset.y, basePosition.z);
            handTween = targetTransform
                .DOLocalMove(destination, pointDuration)
                .SetEase(pointEase)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);
        }

        private Vector3 ToLocalPosition(Vector3 worldPosition)
        {
            Transform parent = targetTransform.parent;
            if (parent == null) return new Vector3(worldPosition.x, worldPosition.y, basePosition.z);

            Vector3 local = parent.InverseTransformPoint(worldPosition);
            return new Vector3(local.x, local.y, basePosition.z);
        }

        private void OnDisable()
        {
            handTween?.Kill();
            handTween = null;
        }

        public override void ApplyTexture()
        {
            if (tex == null) return;
            RawImage img = GetComponent<RawImage>();
            if (img == null) return;
            img.texture = tex;
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

            float rot = source.localEulerAngles.z;
            if (isPhone) { if (isPortrait) phone_Rotation_PT = rot; else phone_Rotation_LS = rot; }
            else { if (isPortrait) tablet_Rotation_PT = rot; else tablet_Rotation_LS = rot; }

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

        public override float GetStoredRotation(bool isPhone, bool isPortrait)
        {
            if (isPhone) return isPortrait ? phone_Rotation_PT : phone_Rotation_LS;
            return isPortrait ? tablet_Rotation_PT : tablet_Rotation_LS;
        }

        #endregion
    }
}
