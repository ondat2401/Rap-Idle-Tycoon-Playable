namespace Amanotes.Core
{
    public enum ScreenState
    {
        Hidden,
        Showing,
        Visible,
        Hiding
    }

    public enum ScreenLayer
    {
        Background = 0,
        Main = 100,
        Popup = 200,
        Overlay = 300,
        System = 400
    }
}
