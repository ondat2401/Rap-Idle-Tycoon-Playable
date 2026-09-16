using UnityEngine;

namespace Amanotes.Core
{
    public class DontDestroyMono : MonoBehaviour
    {
        void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
