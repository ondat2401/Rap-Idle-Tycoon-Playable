using UnityEditor;
using UnityEngine;

namespace Amanotes.LunaFieldGenerator.Editor
{
    /// <summary>
    /// Centralized EditorPrefs persistence for LunaField editor settings.
    /// Eliminates scattered EditorPrefs calls across multiple files.
    /// </summary>
    public class EditorSettingsStore
    {
        #region Keys

        private const string KeyFieldPath = "LunaFieldEditor_FieldPath";
        private const string KeyDummyPath = "LunaFieldEditor_DummyPath";
        private const string KeyAutoRemove = "LunaFieldEditor_AutoRemove";
        private const string KeyShowStatus = "LunaFieldEditor_ShowStatus";
        private const string KeyShowFieldList = "LunaFieldEditor_ShowFieldList";
        private const string KeyShowActions = "LunaFieldEditor_ShowActions";
        private const string KeyCurrentTab = "LunaFieldEditor_CurrentTab";
        private const string KeyFieldBaseName = "LunaFieldEditor_FieldBaseName";
        private const string KeyCreateFieldType = "LunaFieldEditor_CreateFieldType";
        private const string KeyCreateWithTexture = "LunaFieldEditor_CreateWithTexture";
        private const string KeyCreateWithPosition = "LunaFieldEditor_CreateWithPosition";
        private const string KeyCreateWithScale = "LunaFieldEditor_CreateWithScale";
        private const string KeyCreateWithRotation = "LunaFieldEditor_CreateWithRotation";
        private const string KeyCreateAsBackground = "LunaFieldEditor_CreateAsBackground";
        private const string KeyDefaultTexture = "LunaFieldEditor_DefaultTexture";
        private const string KeyFieldSubFolder = "LunaFieldEditor_FieldSubFolder";
        private const string KeyGenerateSubFolder = "LunaFieldEditor_GenerateSubFolder";
        private const string KeyCreateParentPath = "LunaFieldEditor_CreateParentPath";
        private const string KeyGenerateParentPath = "LunaFieldEditor_GenerateParentPath";

        #endregion

        #region Defaults

        public const string DefaultFieldPath = "Assets/_LunaFieldGenerator/Scripts/Samples";
        public const string DefaultDummyPath = "Assets/_LunaFieldGenerator/Dummys";

        #endregion

        #region Properties

        public string FieldPath
        {
            get => EditorPrefs.GetString(KeyFieldPath, DefaultFieldPath);
            set => EditorPrefs.SetString(KeyFieldPath, value);
        }

        public string DummyPath
        {
            get => EditorPrefs.GetString(KeyDummyPath, DefaultDummyPath);
            set => EditorPrefs.SetString(KeyDummyPath, value);
        }

        public bool AutoRemoveDuplicates
        {
            get => EditorPrefs.GetBool(KeyAutoRemove, true);
            set => EditorPrefs.SetBool(KeyAutoRemove, value);
        }

        public bool ShowStatus
        {
            get => EditorPrefs.GetBool(KeyShowStatus, true);
            set => EditorPrefs.SetBool(KeyShowStatus, value);
        }

        public bool ShowFieldList
        {
            get => EditorPrefs.GetBool(KeyShowFieldList, true);
            set => EditorPrefs.SetBool(KeyShowFieldList, value);
        }

        public bool ShowActions
        {
            get => EditorPrefs.GetBool(KeyShowActions, true);
            set => EditorPrefs.SetBool(KeyShowActions, value);
        }

        public int CurrentTab
        {
            get => EditorPrefs.GetInt(KeyCurrentTab, 0);
            set => EditorPrefs.SetInt(KeyCurrentTab, value);
        }

        public string FieldBaseName
        {
            get => EditorPrefs.GetString(KeyFieldBaseName, "NewField");
            set => EditorPrefs.SetString(KeyFieldBaseName, value);
        }

        public int CreateFieldType
        {
            get => EditorPrefs.GetInt(KeyCreateFieldType, 0);
            set => EditorPrefs.SetInt(KeyCreateFieldType, value);
        }

        public bool CreateWithTexture
        {
            get => EditorPrefs.GetBool(KeyCreateWithTexture, true);
            set => EditorPrefs.SetBool(KeyCreateWithTexture, value);
        }

        public bool CreateWithPosition
        {
            get => EditorPrefs.GetBool(KeyCreateWithPosition, true);
            set => EditorPrefs.SetBool(KeyCreateWithPosition, value);
        }

        public bool CreateWithScale
        {
            get => EditorPrefs.GetBool(KeyCreateWithScale, true);
            set => EditorPrefs.SetBool(KeyCreateWithScale, value);
        }

        public bool CreateWithRotation
        {
            get => EditorPrefs.GetBool(KeyCreateWithRotation, false);
            set => EditorPrefs.SetBool(KeyCreateWithRotation, value);
        }

        public bool CreateAsBackground
        {
            get => EditorPrefs.GetBool(KeyCreateAsBackground, false);
            set => EditorPrefs.SetBool(KeyCreateAsBackground, value);
        }

        public string DefaultTexturePath
        {
            get => EditorPrefs.GetString(KeyDefaultTexture, "");
            set => EditorPrefs.SetString(KeyDefaultTexture, value);
        }

        public string FieldSubFolderPath
        {
            get => EditorPrefs.GetString(KeyFieldSubFolder, "");
            set => EditorPrefs.SetString(KeyFieldSubFolder, value);
        }

        public string GenerateSubFolderPath
        {
            get => EditorPrefs.GetString(KeyGenerateSubFolder, "");
            set => EditorPrefs.SetString(KeyGenerateSubFolder, value);
        }

        public string CreateParentPath
        {
            get => EditorPrefs.GetString(KeyCreateParentPath, "");
            set => EditorPrefs.SetString(KeyCreateParentPath, value);
        }

        public string GenerateParentPath
        {
            get => EditorPrefs.GetString(KeyGenerateParentPath, "");
            set => EditorPrefs.SetString(KeyGenerateParentPath, value);
        }

        #endregion

        #region Methods

        /// <summary>Reset paths to default values.</summary>
        public void ResetPaths()
        {
            FieldPath = DefaultFieldPath;
            DummyPath = DefaultDummyPath;
        }

        /// <summary>Clear default texture reference.</summary>
        public void ClearDefaultTexture()
        {
            DefaultTexturePath = "";
        }

        #endregion
    }
}
