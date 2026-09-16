namespace Amanotes.Core
{
    public interface ICore
    {
        void OnAwake();
        void OnStart();
        void OnUpdate();
        void OnFixedUpdate();
    }
}
