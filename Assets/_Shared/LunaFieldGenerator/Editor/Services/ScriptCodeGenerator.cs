using System.Text;

namespace Amanotes.LunaFieldGenerator.Editor
{
    /// <summary>
    /// Generates .cs source code for new FieldBase subclasses.
    /// </summary>
    public static class ScriptCodeGenerator
    {
        /// <summary>
        /// Generate a standard field class with configurable property groups.
        /// </summary>
        public static string GenerateFieldClass(
            string baseName,
            bool withTexture = true,
            bool withPosition = true,
            bool withScale = true,
            bool withRotation = false,
            string sectionName = null)
        {
            string uiUsing = withTexture ? "\nusing UnityEngine.UI;" : "";
            string effectiveSectionName = sectionName ?? baseName;

            var fields = new StringBuilder();
            var metadataOverrides = new StringBuilder();
            int idx = 0;

            // Texture field
            if (withTexture)
            {
                fields.AppendLine($@"
        [Header(""Texture"")]
        [LunaPlaygroundAsset(""Texture"", 0, ""{effectiveSectionName}"")]
        public Texture2D tex = null;");
                metadataOverrides.AppendLine("        public override bool HasTexture => true;");
                idx = 1;
            }

            // Position fields
            if (withPosition)
            {
                fields.AppendLine($@"
        [Header(""Phone Position"")]
        [LunaPlaygroundField(""Phone_Position_PT"", {idx}, ""{effectiveSectionName}"")]
        public Vector2 phone_Position_PT = Vector2.zero;
        [LunaPlaygroundField(""Phone_Position_LS"", {idx + 1}, ""{effectiveSectionName}"")]
        public Vector2 phone_Position_LS = Vector2.zero;

        [Header(""Tablet Position"")]
        [LunaPlaygroundField(""Tablet_Position_PT"", {idx + 10}, ""{effectiveSectionName}"")]
        public Vector2 tablet_Position_PT = Vector2.zero;
        [LunaPlaygroundField(""Tablet_Position_LS"", {idx + 11}, ""{effectiveSectionName}"")]
        public Vector2 tablet_Position_LS = Vector2.zero;");
                metadataOverrides.AppendLine("        public override bool HasPosition => true;");
                idx += 20;
            }

            // Scale fields
            if (withScale)
            {
                fields.AppendLine($@"
        [Header(""Phone Scale"")]
        [LunaPlaygroundField(""Phone_Scale_PT"", {idx}, ""{effectiveSectionName}"")]
        public Vector2 phone_Scale_PT = Vector2.one;
        [LunaPlaygroundField(""Phone_Scale_LS"", {idx + 1}, ""{effectiveSectionName}"")]
        public Vector2 phone_Scale_LS = Vector2.one;

        [Header(""Tablet Scale"")]
        [LunaPlaygroundField(""Tablet_Scale_PT"", {idx + 10}, ""{effectiveSectionName}"")]
        public Vector2 tablet_Scale_PT = Vector2.one;
        [LunaPlaygroundField(""Tablet_Scale_LS"", {idx + 11}, ""{effectiveSectionName}"")]
        public Vector2 tablet_Scale_LS = Vector2.one;");
                metadataOverrides.AppendLine("        public override bool HasScale => true;");
                idx += 20;
            }

            // Rotation fields
            if (withRotation)
            {
                fields.AppendLine($@"
        [Header(""Phone Rotation"")]
        [LunaPlaygroundField(""Phone_Rotation_PT"", {idx}, ""{effectiveSectionName}"")]
        public float phone_Rotation_PT = 0f;
        [LunaPlaygroundField(""Phone_Rotation_LS"", {idx + 1}, ""{effectiveSectionName}"")]
        public float phone_Rotation_LS = 0f;

        [Header(""Tablet Rotation"")]
        [LunaPlaygroundField(""Tablet_Rotation_PT"", {idx + 10}, ""{effectiveSectionName}"")]
        public float tablet_Rotation_PT = 0f;
        [LunaPlaygroundField(""Tablet_Rotation_LS"", {idx + 11}, ""{effectiveSectionName}"")]
        public float tablet_Rotation_LS = 0f;");
                metadataOverrides.AppendLine("        public override bool HasRotation => true;");
            }

            // Build ApplyOrientation
            var applyOrientation = new StringBuilder();
            if (withPosition || withScale || withRotation)
            {
                applyOrientation.AppendLine(@"
        public override void ApplyOrientation()
        {
            bool isPhone = OrientationTracker.Instance.IsPhone;
            bool isPortrait = OrientationTracker.Instance.IsPortrait;
");
                if (withPosition)
                {
                    applyOrientation.AppendLine(@"            Vector2 pos;
            if (isPhone) pos = isPortrait ? phone_Position_PT : phone_Position_LS;
            else pos = isPortrait ? tablet_Position_PT : tablet_Position_LS;
            targetTransform.localPosition = new Vector3(pos.x, pos.y, 0);
");
                }
                if (withScale)
                {
                    applyOrientation.AppendLine(@"            Vector2 scale;
            if (isPhone) scale = isPortrait ? phone_Scale_PT : phone_Scale_LS;
            else scale = isPortrait ? tablet_Scale_PT : tablet_Scale_LS;
            targetTransform.localScale = new Vector3(scale.x, scale.y, 1);
");
                }
                if (withRotation)
                {
                    applyOrientation.AppendLine(@"            float rot;
            if (isPhone) rot = isPortrait ? phone_Rotation_PT : phone_Rotation_LS;
            else rot = isPortrait ? tablet_Rotation_PT : tablet_Rotation_LS;
            targetTransform.localRotation = Quaternion.Euler(0, 0, rot);
");
                }
                applyOrientation.AppendLine("        }");
            }

            // Build ApplyTexture
            string applyTexture = withTexture ? @"
        public override void ApplyTexture()
        {
            if (tex == null) return;
            RawImage img = GetComponent<RawImage>();
            if (img != null) img.texture = tex;
            img.rectTransform.sizeDelta = new Vector2(tex.width, tex.height);
        }
" : "";

            // Build CaptureSlotFrom override
            var captureMethod = BuildCaptureSlotMethod(withPosition, withScale, withRotation);

            // Build GetStored overrides
            var getStoredMethods = BuildGetStoredMethods(withPosition, withScale, withRotation);

            return $@"using UnityEngine;
using OrientationTracking.Core;{uiUsing}
using Amanotes.Core;
using Amanotes.LunaFieldGenerator;

namespace LunaField
{{
    public class {baseName}_LunaField : FieldBase
    {{{fields}

        #region Metadata

{metadataOverrides}
        #endregion

        #region Apply
{applyOrientation}{applyTexture}
        #endregion

        #region Capture
{captureMethod}{getStoredMethods}
        #endregion
    }}
}}
";
        }

        /// <summary>
        /// Generate a background field class with 2 textures (portrait + landscape) and aspect-fill.
        /// </summary>
        public static string GenerateBackgroundClass(string baseName, string sectionName = null)
        {
            string effectiveSectionName = sectionName ?? baseName;
            return $@"using UnityEngine;
using OrientationTracking.Core;
using Amanotes.Core;
using UnityEngine.UI;
using Amanotes.LunaFieldGenerator;

namespace LunaField
{{
    public class {baseName}_LunaField : FieldBase
    {{
        [Header(""Background Textures"")]
        [LunaPlaygroundAsset(""Texture_Portrait"", 0, ""{effectiveSectionName}"")]
        public Texture2D texPortrait = null;
        [LunaPlaygroundAsset(""Texture_Landscape"", 1, ""{effectiveSectionName}"")]
        public Texture2D texLandscape = null;

        public override bool HasTexture => true;

        public override void ApplyTexture()
        {{
            bool isPortrait = OrientationTracker.Instance.IsPortrait;
            Texture2D tex = isPortrait ? texPortrait : texLandscape;
            if (tex == null) return;

            RawImage img = GetComponent<RawImage>();
            if (img == null) return;

            img.texture = tex;
            ImageRectHelper.ApplyAspectFillToPanel(img);
        }}
    }}
}}
";
        }

        #region Private Helpers

        private static string BuildCaptureSlotMethod(bool withPosition, bool withScale, bool withRotation)
        {
            if (!withPosition && !withScale && !withRotation) return "";

            var sb = new StringBuilder();
            sb.AppendLine(@"
        public override void CaptureSlotFrom(Transform source, bool isPhone, bool isPortrait)
        {
            if (source == null) return;
");
            if (withPosition)
            {
                sb.AppendLine(@"            Vector2 pos = new Vector2(source.localPosition.x, source.localPosition.y);
            if (isPhone) { if (isPortrait) phone_Position_PT = pos; else phone_Position_LS = pos; }
            else { if (isPortrait) tablet_Position_PT = pos; else tablet_Position_LS = pos; }
");
            }
            if (withScale)
            {
                sb.AppendLine(@"            Vector2 scl = new Vector2(source.localScale.x, source.localScale.y);
            if (isPhone) { if (isPortrait) phone_Scale_PT = scl; else phone_Scale_LS = scl; }
            else { if (isPortrait) tablet_Scale_PT = scl; else tablet_Scale_LS = scl; }
");
            }
            if (withRotation)
            {
                sb.AppendLine(@"            float rot = source.localEulerAngles.z;
            if (isPhone) { if (isPortrait) phone_Rotation_PT = rot; else phone_Rotation_LS = rot; }
            else { if (isPortrait) tablet_Rotation_PT = rot; else tablet_Rotation_LS = rot; }
");
            }
            sb.AppendLine("        }");
            return sb.ToString();
        }

        private static string BuildGetStoredMethods(bool withPosition, bool withScale, bool withRotation)
        {
            var sb = new StringBuilder();

            if (withPosition)
            {
                sb.AppendLine(@"
        public override Vector2 GetStoredPosition(bool isPhone, bool isPortrait)
        {
            if (isPhone) return isPortrait ? phone_Position_PT : phone_Position_LS;
            return isPortrait ? tablet_Position_PT : tablet_Position_LS;
        }");
            }

            if (withScale)
            {
                sb.AppendLine(@"
        public override Vector2 GetStoredScale(bool isPhone, bool isPortrait)
        {
            if (isPhone) return isPortrait ? phone_Scale_PT : phone_Scale_LS;
            return isPortrait ? tablet_Scale_PT : tablet_Scale_LS;
        }");
            }

            if (withRotation)
            {
                sb.AppendLine(@"
        public override float GetStoredRotation(bool isPhone, bool isPortrait)
        {
            if (isPhone) return isPortrait ? phone_Rotation_PT : phone_Rotation_LS;
            return isPortrait ? tablet_Rotation_PT : tablet_Rotation_LS;
        }");
            }

            return sb.ToString();
        }

        #endregion
    }
}
