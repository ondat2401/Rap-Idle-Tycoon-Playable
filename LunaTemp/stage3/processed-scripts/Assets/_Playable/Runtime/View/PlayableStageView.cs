using System.Collections;
using _Playable.Runtime.Config;
using UnityEngine;

namespace _Playable.Runtime.View
{
    /// <summary>
    /// Lop UI cua canh: tap target va hieu ung nhac. Nen, vat the va hai nhan vat da nam tren
    /// <see cref="PlayableMainMap"/> (world space); o day chi giu tham chieu va chuyen tiep lenh.
    /// </summary>
    public sealed class PlayableStageView : MonoBehaviour
    {
        [Header("Map (world space)")]
        [SerializeField] private PlayableMainMap _map;

        [Header("Tuong tac")]
        [SerializeField] private PlayableTapTargetView _tapTarget;
        [SerializeField] private RectTransform _musicFx;
        [SerializeField] private float _musicFxSpinSpeed = 120f;

        private bool _musicFxSpinning;

        public PlayableMainMap Map => this._map;

        public PlayableManView Man => this._map != null ? this._map.Man : null;

        /// <summary>Ban gai da duoc chon o buoc 1. Null truoc khi chon.</summary>
        public PlayableWomanView Woman => this._map != null ? this._map.Woman : null;

        public PlayableTapTargetView TapTarget => this._tapTarget;

        private void Update()
        {
            if (this._musicFxSpinning && this._musicFx != null)
            {
                this._musicFx.Rotate(Vector3.forward, -this._musicFxSpinSpeed * Time.unscaledDeltaTime);
            }
        }

        public PlayableWomanView SelectWoman(int index)
        {
            return this._map != null ? this._map.SelectWoman(index) : null;
        }

        /// <summary>Tra canh ve trang thai mo dau: map + nhan vat ve ban dau, tap target an.</summary>
        public void ResetToInitial()
        {
            this._map?.ResetToBase();
            this._tapTarget?.HideImmediate();
            this.SetMusicFxVisible(false);
        }

        public void SetMusicFxVisible(bool visible)
        {
            this._musicFxSpinning = visible;

            if (this._musicFx != null)
            {
                this._musicFx.gameObject.SetActive(visible);
            }
        }

        /// <summary>Nang xe len ban thu <paramref name="index"/> (0-based trong ba lua chon).</summary>
        public IEnumerator SwapCar(int index, [Bridge.Ref] PlayableTiming timing)
        {
            yield return this._map.SetLevel(PlayableMapSlot.Car, index + 1, timing);
        }

        /// <summary>
        /// Nang nha len ban thu <paramref name="index"/>: MOI slot cua map (tru xe) cung doi sang level
        /// tuong ung; slot nao khong co sprite o level do thi an. Xe co nhom mua rieng nen giu nguyen.
        /// </summary>
        public IEnumerator SwapRoom(int index, [Bridge.Ref] PlayableTiming timing)
        {
            yield return this._map.SetLevelAll(index + 1, timing, PlayableMapSlot.Car);
        }
    }
}
