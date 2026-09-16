using UnityEngine;
#if LUNA_PLAYABLE
using Luna.Unity;
#endif

namespace _Playable.Runtime
{
    public class CTA : MonoBehaviour
    {
        public void OnClick()
        {
#if LUNA_PLAYABLE
            Playable.InstallFullGame();
#else
            Debug.Log("CTA clicked (Luna SDK not present in editor).");
#endif
        }
    }
}
