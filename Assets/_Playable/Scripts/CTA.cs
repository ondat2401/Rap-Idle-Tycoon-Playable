using UnityEngine;
using Luna.Unity;

namespace _Playable.Runtime
{
    public class CTA : MonoBehaviour
    {
        public void OnClick()
        {
            Playable.InstallFullGame();

        }
    }
}
