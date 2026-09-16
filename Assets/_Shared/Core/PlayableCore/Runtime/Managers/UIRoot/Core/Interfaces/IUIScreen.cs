namespace Amanotes.Core
{
    /// <summary>
    /// Base interface for all UI screens
    /// </summary>
    public interface IGUIBase
    {
        string ScreenId { get; }
        ScreenState State { get; }
        void Show();
        void Hide();
    }

    /// <summary>
    /// Interface for screens with animation support
    /// </summary>
    public interface IAnimatedScreen : IGUIBase
    {
        void ShowAnimated(float duration);
        void HideAnimated(float duration);
    }

    /// <summary>
    /// Interface for screens that can receive data
    /// </summary>
    public interface IScreenWithData<T> : IGUIBase
    {
        void SetData(T data);
    }

    /// <summary>
    /// Interface for lifecycle events
    /// </summary>
    public interface IScreenLifecycle
    {
        void OnScreenInitialize();
        void OnScreenShow();
        void OnScreenHide();
        void OnScreenDestroy();
    }
}
