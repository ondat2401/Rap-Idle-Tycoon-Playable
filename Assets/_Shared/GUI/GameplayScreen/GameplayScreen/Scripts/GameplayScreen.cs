using Amanotes.Core;
using UnityEngine;
    using UnityEngine.UI;

    public class GameplayScreen : AnimatedGUIBase
    {
        //[Header("UI References")]
        // Add your UI component references here

        public override void OnScreenInitialize()
        {
            base.OnScreenInitialize();
            // Initialize UI components
            GUIManager.Instance.RegisterScreen(this);
        }

        public override void OnScreenShow()
        {
            base.OnScreenShow();
            // Called when screen is shown
        }

        public override void OnScreenHide()
        {
            base.OnScreenHide();
            // Called when screen is hidden
        }
    }