using UnityEngine;

namespace Amanotes.Core
{
    public class AnimationActiveTrigger : MonoBehaviour
    {
        public void ActiveSelf()
        {
            gameObject.SetActive(false);
        }
    }
}
