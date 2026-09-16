using UnityEngine;

namespace Amanotes.Core
{
    public class SingletonMonoDontDestroy<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T singleton;
        public static bool IsInstanceValid() => singleton != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            singleton = null;
        }

        protected virtual void Awake()
        {
            if (singleton == null)
            {
                singleton = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (singleton != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (singleton == this)
                singleton = null;
        }

        public static T Instance
        {
            get
            {
                if (!Application.isPlaying) return null;
                if (singleton == null)
                    singleton = FindObjectOfType<T>();
                return singleton;
            }
        }
    }

    /// <summary>
    /// Singleton bình thường (destroy khi scene thay đổi)
    /// </summary>
    public class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T singleton;
        public static bool IsInstanceValid() => singleton != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            singleton = null;
        }

        protected virtual void Awake()
        {
            if (singleton == null)
            {
                singleton = this as T;
            }
            else if (singleton != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (singleton == this)
                singleton = null;
        }

        public static T Instance
        {
            get
            {
                if (!Application.isPlaying) return null;
                if (singleton == null)
                    singleton = FindObjectOfType<T>();
                return singleton;
            }
        }
    }
}
