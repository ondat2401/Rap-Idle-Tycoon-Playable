using UnityEngine;
using UnityEngine.Events;

namespace _Playable.Runtime.Flow
{
    /// <summary>
    /// Diem thoat cua playable - tuong duong <c>GameManager.GameOver()</c> cua ban goc
    /// (goi <c>redirect()</c> roi <c>end()</c>).
    ///
    /// Package nay khong biet gi ve MRAID / store nen chi ban su kien ra ngoai. Host tu cam handler
    /// cua minh vao <see cref="OnExitRequested"/> hoac implement <see cref="IPlayableExitHandler"/>
    /// tren cung GameObject.
    /// </summary>
    public sealed class PlayableExit : MonoBehaviour
    {
        [Tooltip("Ban khi nguoi choi bam logo / Download / lua chon cuoi cung.")]
        [SerializeField] private UnityEvent _onExitRequested;

        [Tooltip("Chi ban su kien mot lan duy nhat.")]
        [SerializeField] private bool _once = true;

        private bool _fired;

        public UnityEvent OnExitRequested => this._onExitRequested;

        public void RequestExit()
        {
            if (this._once && this._fired)
            {
                return;
            }

            this._fired = true;
            this._onExitRequested?.Invoke();

            var handlers = this.GetComponents<IPlayableExitHandler>();
            foreach (IPlayableExitHandler handler in handlers)
            {
                handler?.OnPlayableExit();
            }

            Debug.Log("[Playable] Exit requested - host nen mo store o day.");
        }
    }

    /// <summary>Cam vao GameObject chua <see cref="PlayableExit"/> de nhan callback ket thuc.</summary>
    public interface IPlayableExitHandler
    {
        void OnPlayableExit();
    }
}
