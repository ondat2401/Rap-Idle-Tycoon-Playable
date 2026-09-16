using UnityEngine;

namespace Amanotes.Core
{
    /// <summary>
    /// Abstract base class for UI screens (Open for extension, Closed for modification)
    /// Coding standard alias: GUIBase
    /// </summary>
    public abstract class GUIBase : MonoBehaviour, IGUIBase, IScreenLifecycle
    {
        [SerializeField] private string screenId;

        protected CanvasGroup canvasGroup;

        public string ScreenId => screenId;
        public ScreenState State { get; protected set; }

        protected virtual void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            OnScreenInitialize();
        }

        #region IGUIBase Implementation

        public virtual void Show()
        {
            if (State == ScreenState.Visible || State == ScreenState.Showing)
                return;

            State = ScreenState.Showing;
            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            State = ScreenState.Visible;

            OnScreenShow();
        }

        public virtual void Hide()
        {
            if (State == ScreenState.Hidden || State == ScreenState.Hiding)
                return;

            State = ScreenState.Hiding;
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
            State = ScreenState.Hidden;

            OnScreenHide();
        }

        #endregion

        #region IScreenLifecycle Implementation

        public virtual void OnScreenInitialize() { }
        public virtual void OnScreenShow() { }
        public virtual void OnScreenHide() { }
        public virtual void OnScreenDestroy() { }

        #endregion

        protected virtual void OnDestroy()
        {
            OnScreenDestroy();
        }
    }
}
