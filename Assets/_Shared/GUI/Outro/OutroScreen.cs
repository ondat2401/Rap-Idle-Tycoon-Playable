using Amanotes.Core;
using UnityEngine;
    using UnityEngine.UI;

    public class OutroScreen : AnimatedGUIBase
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
            AudioManager.Instance.PlaySFX("outro_sfx");
        }

        public override void OnScreenHide()
        {
            base.OnScreenHide();
            // Called when screen is hidden
        }
    }