using UnityEngine;
using UnityEngine.UI;

namespace Amanotes.Core
{
    public class MainMenuScreen : AnimatedGUIBase
    {
        [Header("UI References")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        public override void OnScreenInitialize()
        {
            base.OnScreenInitialize();

            playButton?.onClick.AddListener(OnPlayClicked);
            settingsButton?.onClick.AddListener(OnSettingsClicked);
            quitButton?.onClick.AddListener(OnQuitClicked);

            GUIManager.Instance.RegisterScreen(this);
        }

        public override void OnScreenShow()
        {
            base.OnScreenShow();
            SDebug.Log("[MainMenuScreen] Shown");
        }

        private void OnPlayClicked()
        {
            GUIManager.Instance.ShowScreen("GameScreen");
        }

        private void OnSettingsClicked()
        {
            GUIManager.Instance.ShowScreen("SettingsScreen");
        }

        private void OnQuitClicked()
        {
            Application.Quit();
        }
    }
}
