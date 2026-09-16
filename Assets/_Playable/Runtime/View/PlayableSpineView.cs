using System.Collections;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace _Playable.Runtime.View
{
    /// <summary>
    /// Nen tang chung cho hai nhan vat Spine cua playable. Nhan vat nam trong world space tren
    /// <see cref="PlayableMainMap"/>, duoc Main Camera nhin - giong prefab nhan vat cua game (c_drake,
    /// lover_prefab).
    ///
    /// Theo dung quy uoc spine-unity 4.3 cua du an: <see cref="SkeletonRenderer"/> lo render (MeshRenderer)
    /// va giu skeleton/skin, con <see cref="SkeletonAnimation"/> nam cung GameObject giu
    /// <see cref="Spine.AnimationState"/>. Ca hai deu phai co.
    ///
    /// Moi lan goi animation deu kiem tra ton tai truoc, nen mot ten animation chua co
    /// (xem <c>PlayableAnimationNames</c>) chi log canh bao chu khong nem loi.
    /// </summary>
    public abstract class PlayableSpineView : MonoBehaviour
    {
        protected const int TrackIndex = 0;

        [SerializeField] protected SkeletonRenderer SpineRenderer;
        [SerializeField] protected SkeletonAnimation SpineAnimation;

        protected SkeletonData SpineData => this.SpineSkeleton != null ? this.SpineSkeleton.Data : null;

        protected Skeleton SpineSkeleton => this.SpineRenderer != null ? this.SpineRenderer.Skeleton : null;

        protected Spine.AnimationState State => this.SpineAnimation != null ? this.SpineAnimation.AnimationState : null;

        protected virtual void Awake()
        {
            if (this.SpineRenderer == null)
            {
                this.SpineRenderer = this.GetComponentInChildren<SkeletonRenderer>(true);
            }

            if (this.SpineAnimation == null && this.SpineRenderer != null)
            {
                this.SpineAnimation = this.SpineRenderer.GetComponent<SkeletonAnimation>();
            }

            if (this.SpineRenderer == null || this.SpineAnimation == null)
            {
                Debug.LogWarning(
                    $"[Playable] {this.name}: thieu SkeletonRenderer hoac SkeletonAnimation tren cung GameObject.",
                    this);
            }
        }

        /// <summary>
        /// Doi SkeletonDataAsset luc runtime (vd chon skeleton A/B tu config) roi khoi tao lai.
        /// Bo qua neu asset null hoac trung voi asset hien tai.
        /// </summary>
        public void SetSkeletonData(SkeletonDataAsset dataAsset)
        {
            if (dataAsset == null || this.SpineRenderer == null)
            {
                return;
            }

            if (this.SpineRenderer.skeletonDataAsset == dataAsset && this.SpineRenderer.valid)
            {
                return;
            }

            this.SpineRenderer.skeletonDataAsset = dataAsset;
            this.SpineRenderer.Initialize(true);

            if (this.SpineAnimation != null)
            {
                this.SpineAnimation.Initialize(true);
            }
        }

        /// <summary>Phat animation lap tren track 0.</summary>
        public void PlayLoop(string animationName)
        {
            this.SetAnimation(animationName, true);
        }

        /// <summary>
        /// Phat mot lan roi cho het thoi luong; neu co <paramref name="fallback"/> thi tra ve animation do.
        /// Cho bang unscaled time thay vi lang nghe TrackEntry.Complete de tranh delegate song sot khi
        /// object bi tat giua chung.
        /// </summary>
        public IEnumerator PlayOnce(string animationName, string fallback = null)
        {
            TrackEntry entry = this.SetAnimation(animationName, false);
            if (entry == null)
            {
                yield break;
            }

            float duration = entry.Animation?.Duration ?? 0f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!string.IsNullOrEmpty(fallback))
            {
                this.SetAnimation(fallback, true);
            }
        }

        public void SetVisible(bool visible)
        {
            this.gameObject.SetActive(visible);
        }

        /// <summary>
        /// Bat Spine khoi tao neu chua. Can thiet vi nhan vat nu bi tat san trong prefab: khi
        /// SetActive(true) roi goi animation ngay trong cung frame, thu tu Awake giua node cha va node
        /// Skeleton khong duoc bao dam. <c>Initialize(false)</c> tu thoat som neu da hop le.
        /// </summary>
        protected void EnsureSpineReady()
        {
            if (this.SpineRenderer == null)
            {
                return;
            }

            if (!this.SpineRenderer.valid)
            {
                this.SpineRenderer.Initialize(false);
            }

            if (this.SpineAnimation != null && this.SpineAnimation.AnimationState == null)
            {
                this.SpineAnimation.Initialize(false);
            }
        }

        private TrackEntry SetAnimation(string animationName, bool loop)
        {
            if (string.IsNullOrEmpty(animationName))
            {
                return null;
            }

            this.EnsureSpineReady();

            Spine.AnimationState state = this.State;
            SkeletonData data = this.SpineData;
            if (state == null || data == null)
            {
                Debug.LogWarning($"[Playable] {this.name}: Spine chua san sang, bo qua '{animationName}'.", this);
                return null;
            }

            if (data.FindAnimation(animationName) == null)
            {
                Debug.LogWarning(
                    $"[Playable] {this.name}: khong co animation '{animationName}' trong skeleton '{data.Name}'. " +
                    "Kiem tra PlayableAnimationNames.", this);
                return null;
            }

            return state.SetAnimation(TrackIndex, animationName, loop);
        }
    }
}
