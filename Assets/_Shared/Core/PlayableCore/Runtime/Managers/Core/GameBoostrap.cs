using UnityEngine;

namespace Amanotes.Core
{
    public class GameBoostrap : MonoBehaviour
    {
        void Awake()
        {
            CoreManager.Instance.Register(new CoreGameFlow());
        }
    }
}
