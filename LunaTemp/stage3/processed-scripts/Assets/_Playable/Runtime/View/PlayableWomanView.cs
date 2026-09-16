using System.Collections;
using _Playable.Runtime.Config;
using _Playable.Runtime.Core;
using UnityEngine;

namespace _Playable.Runtime.View
{
    /// <summary>
    /// Nhan vat nu, nam trong world space tren <see cref="PlayableMainMap"/>. Skeleton <c>npc_*</c> hien
    /// khong co animation gian du, nen buoc "gian roi bo di" duoc dien ta bang tween truot ra khoi man hinh
    /// - xem ghi chu TODO(asset) trong <see cref="PlayableAnimationNames.Woman.Anger"/>.
    ///
    /// Moi khoang lech (offset) tinh bang don vi world, khong phai pixel.
    /// </summary>
    public sealed class PlayableWomanView : PlayableSpineView
    {
        private Vector3 _homePosition;
        private bool _hasHome;

        public void Initialize()
        {
            this.CacheHome();
            this.transform.localPosition = this._homePosition;
            this.SetVisible(false);
        }

        /// <summary>Truot tu ngoai man hinh vao cho (ban goc: tu x + 1000px ve cho trong 1 giay).</summary>
        public IEnumerator PlayEnter(float offsetX, float duration)
        {
            this.CacheHome();
            this.transform.localPosition = this._homePosition + new Vector3(offsetX, 0f, 0f);
            this.SetVisible(true);
            this.PlayLoop(PlayableAnimationNames.Woman.Idle);
            yield return PlayableTween.MoveLocal(this.transform, this._homePosition, duration, PlayableEase.QuadOut);
        }

        /// <summary>
        /// Gian roi bo di. Khi co animation gian that thi <see cref="PlayableAnimationNames.Woman.Anger"/>
        /// se tro dung ten va doan nay tu chay dung - phan tween truot ra van giu.
        /// </summary>
        public IEnumerator PlayAngerAndLeave(float offsetX, float duration)
        {
            this.PlayLoop(PlayableAnimationNames.Woman.Anger);
            yield return PlayableTween.MoveLocal(this.transform,
                this.transform.localPosition + new Vector3(offsetX, 0f, 0f), duration, PlayableEase.QuadIn);
            this.SetVisible(false);
        }

        /// <summary>Quay lai tu ben phai, chao mung mot lan roi chuyen sang idle vui ve.</summary>
        public IEnumerator PlayComeBack(float offsetX, float duration)
        {
            this.CacheHome();
            this.transform.localPosition = this._homePosition + new Vector3(offsetX, 0f, 0f);
            this.SetVisible(true);
            float startedAt = Time.realtimeSinceStartup;
            Debug.Log($"[Playable][Woman] ComeBack begin | moveDuration={duration:0.###}s, offsetX={offsetX:0.###}",
                this);

            Coroutine move = this.StartCoroutine(PlayableTween.MoveLocal(this.transform, this._homePosition, duration,
                PlayableEase.QuadOut));
            yield return this.PlayOnce(PlayableAnimationNames.Woman.ComeBack);
            Debug.Log($"[Playable][Woman] ComeBack animation complete | actual={Time.realtimeSinceStartup - startedAt:0.###}s",
                this);
            yield return move;
            this.PlayLoop(PlayableAnimationNames.Woman.ComeBackIdle);
            Debug.Log($"[Playable][Woman] ComeBack move complete | actual={Time.realtimeSinceStartup - startedAt:0.###}s",
                this);
        }

        private void CacheHome()
        {
            if (this._hasHome)
            {
                return;
            }

            this._homePosition = this.transform.localPosition;
            this._hasHome = true;
        }
    }
}
