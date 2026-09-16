using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using _Playable.Runtime.Config;
using _Playable.Runtime.Core;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace _Playable.Runtime.View
{
    /// <summary>
    /// Nhan vat nam. Skeleton <c>character1</c> tach thanh 4 part skin (mic / outfit / head / jewel)
    /// cong mot skin bieu cam (emo_*), nen moi lan doi trang phuc phai gop lai thanh mot Skin duy nhat.
    ///
    /// Doi skin lam y het <c>CharacterMainController.RefreshVisual</c> cua game:
    /// <c>SetSkin(combined)</c> -> <c>SetupPoseSlots()</c> -> <c>AnimationState.Apply</c>.
    ///
    /// Rieng bieu cam: rig ve mat goc (long may <c>Nude/r_brow</c>, <c>Nude/l_brow</c> va mieng lip-sync
    /// <c>Nude/mouth1</c> do animation key moi frame) o slot RIENG, luon hien. Skin emo_* chi ve them mot lop
    /// long may / mat / mieng len tren, nen neu de nguyen thi mat goc chong len bieu cam. Game khong dung emo_*
    /// nen khong gap. Vi vay khi bieu cam khac <see cref="_emotionNormal"/>, cac slot mat goc bi an sau moi lan
    /// animation apply, truoc khi renderer dung mesh.
    ///
    /// Ngoai le luc dang rap: mieng lip-sync la thu cho thay nhan vat dang rap, nen giu mieng lip-sync va an
    /// mieng emo thay vi nguoc lai (long may / mat van theo bieu cam).
    ///
    /// Ban goc dung 3 GameObject nhan vat rieng cho 3 lua chon quan ao; o day chi doi skin tren cung
    /// mot skeleton.
    /// </summary>
    public sealed class PlayableManView : PlayableSpineView
    {
        private const string CombinedSkinName = "playable_man_combined";

        [Header("Bieu cam")]
        [SerializeField] private string _emotionNormal = PlayableSkinNames.EmotionNormal;
        [SerializeField] private string _emotionHappy = PlayableSkinNames.EmotionHappy;
        [SerializeField] private string _emotionSad = PlayableSkinNames.EmotionSad;

        [Tooltip("Slot mat goc bi an khi dang o bieu cam khac normal, de khong chong len lop emo_*.")]
        [SerializeField]
        private string[] _baseFaceSlots = { "Nude/r_brow", "Nude/l_brow", "Nude/mouth1" };

        [Tooltip("Slot mieng lip-sync do animation key. Luc rap slot nay duoc giu lai.")]
        [SerializeField] private string _lipSyncMouthSlot = "Nude/mouth1";

        [Tooltip("Slot mieng cua lop emo_*. Luc rap slot nay bi an de khong chong len mieng lip-sync.")]
        [SerializeField] private string _emotionMouthSlot = "Nude/mouth_sad";

        // TODO(debug): trace tam de tim loi mieng khong nhep o lan rap_idle thu 2. Tat/xoa sau khi tim ra nguyen nhan.
        [Header("Debug tam")]
        [SerializeField] private bool _traceLipSync = true;

        private readonly List<Slot> _resolvedBaseFaceSlots = new List<Slot>();

        private PlayableSkinSet _currentSet;
        private string _currentEmotion;
        private int _animationVersion;
        private bool _keepLipSync;
        private Skeleton _resolvedFor;
        private Slot _resolvedLipSyncSlot;
        private Slot _resolvedEmotionMouthSlot;
        private SkeletonAnimation _subscribedAnimation;

        private TrackEntry _traceEntry;
        private int _traceLoop = -1;
        private int _traceLipChanges;
        private int _traceNullFrames;
        private string _traceLastLip;
        private int _traceRapPlayCount;

        protected override void Awake()
        {
            base.Awake();
            this.SubscribeUpdateComplete();
        }

        private void OnDestroy()
        {
            if (this._subscribedAnimation != null)
            {
                this._subscribedAnimation.UpdateComplete -= this.HandleUpdateComplete;
                this._subscribedAnimation = null;
            }
        }

        /// <summary>Ap bo skin khoi dau va bieu cam binh thuong.</summary>
        public void Initialize([Bridge.Ref] PlayableSkinSet startSkin)
        {
            this.BeginAnimation();
            this._currentSet = startSkin;
            this._currentEmotion = this._emotionNormal;
            this.RebuildSkin();
            this.PlayLoop(PlayableAnimationNames.Man.Idle);
        }

        public void SetIdle()
        {
            this.BeginAnimation();
            this.SetIdleInternal();
        }

        /// <summary>Khoc: doi bieu cam buon, phat Idle-Sad trong <paramref name="duration"/> roi ve idle.</summary>
        public IEnumerator PlaySad(float duration)
        {
            int version = this.BeginAnimation();
            this.SetEmotion(this._emotionSad);
            this.PlayLoop(PlayableAnimationNames.Man.Sad);
            yield return PlayableTween.Delay(duration);
            if (this.IsCurrentAnimation(version))
            {
                this.SetIdleInternal();
            }
        }

        /// <summary>Rap: bieu cam vui nhung giu mieng lip-sync cua animation rap.</summary>
        public void StartRap()
        {
            this.BeginAnimation();
            this._keepLipSync = true;
            this.SetEmotion(this._emotionHappy);
            this.PlayLoop(PlayableAnimationNames.Man.RapIdle);

            this._traceRapPlayCount++;
            this.Trace($"StartRap #{this._traceRapPlayCount} | emotion={this._currentEmotion} keepLipSync={this._keepLipSync} " +
                       $"track='{this.State?.GetCurrent(0)?.Animation?.Name}'");
        }

        /// <summary>An mung khi mua xe / nha (ban goc: Upgrade_1).</summary>
        public IEnumerator PlayCelebrate()
        {
            int version = this.BeginAnimation();
            this.SetEmotion(this._emotionHappy);
            yield return this.PlayOnce(PlayableAnimationNames.Man.Upgrade1);
            if (this.IsCurrentAnimation(version))
            {
                this.SetIdleInternal();
            }
        }

        /// <summary>An mung khi doi trang phuc (ban goc: Upgrade_2).</summary>
        public IEnumerator PlayDressUp([Bridge.Ref] PlayableSkinSet skinSet)
        {
            int version = this.BeginAnimation();
            this._currentSet = skinSet;
            this.SetEmotion(this._emotionHappy);
            this.RebuildSkin();
            yield return this.PlayOnce(PlayableAnimationNames.Man.Upgrade2);
            if (this.IsCurrentAnimation(version))
            {
                this.SetIdleInternal();
            }
        }

        private void SetIdleInternal()
        {
            this.SetEmotion(this._emotionNormal);
            this.PlayLoop(PlayableAnimationNames.Man.Idle);
        }

        /// <summary>
        /// Bat dau mot trang thai moi. Neu vua roi khoi trang thai rap thi tra cac slot ve setup pose - mieng emo
        /// da bi an moi frame trong luc rap, va neu bieu cam ke tiep trung bieu cam cu thi SetEmotion se khong
        /// dung lai skin nen khong tu hien lai.
        /// </summary>
        private int BeginAnimation([CallerMemberName] string caller = null)
        {
            this.Trace($"BeginAnimation from {caller} | prevKeepLipSync={this._keepLipSync} emotion={this._currentEmotion} " +
                       $"track='{this.State?.GetCurrent(0)?.Animation?.Name}'");

            if (this._keepLipSync)
            {
                this._keepLipSync = false;

                Skeleton skeleton = this.SpineSkeleton;
                if (skeleton != null)
                {
                    skeleton.SetSlotsToSetupPose();
                    this.State?.Apply(skeleton);
                    this.HideBaseFaceIfNeeded(skeleton);
                }
            }

            this._animationVersion++;
            return this._animationVersion;
        }

        private bool IsCurrentAnimation(int version)
        {
            return version == this._animationVersion;
        }

        private void SetEmotion(string emotion)
        {
            if (this._currentEmotion == emotion)
            {
                return;
            }

            this._currentEmotion = emotion;
            this.RebuildSkin();
        }

        /// <summary>
        /// Gop 4 part skin + skin bieu cam thanh mot Skin roi ap len skeleton - cung thu tu goi voi
        /// CharacterMainController.RefreshVisual.
        /// </summary>
        private void RebuildSkin()
        {
            this.EnsureSpineReady();
            this.SubscribeUpdateComplete();

            SkeletonData data = this.SpineData;
            Skeleton skeleton = this.SpineSkeleton;
            if (data == null || skeleton == null)
            {
                return;
            }

            var combined = new Skin(CombinedSkinName);
            this.AddSkinPart(combined, data, this._currentSet.Mic);
            this.AddSkinPart(combined, data, this._currentSet.Outfit);
            this.AddSkinPart(combined, data, this._currentSet.Head);
            this.AddSkinPart(combined, data, this._currentSet.Jewel);
            this.AddSkinPart(combined, data, this._currentEmotion);

            skeleton.SetSkin(combined);

            // Spine 4.2 dung SetSlotsToSetupPose. Buoc nay cung tra long may goc ve
            // khi quay lai bieu cam normal (luc truoc da bi an).
            skeleton.SetSlotsToSetupPose();
            this.State?.Apply(skeleton);
            this.HideBaseFaceIfNeeded(skeleton);
        }

        private void SubscribeUpdateComplete()
        {
            if (this.SpineAnimation == null || this._subscribedAnimation == this.SpineAnimation)
            {
                return;
            }

            if (this._subscribedAnimation != null)
            {
                this._subscribedAnimation.UpdateComplete -= this.HandleUpdateComplete;
            }

            this._subscribedAnimation = this.SpineAnimation;
            this._subscribedAnimation.UpdateComplete += this.HandleUpdateComplete;
        }

        /// <summary>
        /// Chay moi frame sau khi animation da apply va cap nhat world transform, truoc khi renderer dung mesh.
        /// Animation key lai mieng lip-sync moi frame nen phai xu ly lai o day chu khong chi luc doi skin.
        /// </summary>
        private void HandleUpdateComplete(ISkeletonAnimation animated)
        {
            Skeleton skeleton = this.SpineSkeleton;
            this.HideBaseFaceIfNeeded(skeleton);
            this.TraceLipSync(skeleton);
        }

        private void HideBaseFaceIfNeeded(Skeleton skeleton)
        {
            if (skeleton == null || this._currentEmotion == this._emotionNormal)
            {
                return;
            }

            if (this._resolvedFor != skeleton)
            {
                this.ResolveFaceSlots(skeleton);
            }

            foreach (Slot slot in this._resolvedBaseFaceSlots)
            {
                if (this._keepLipSync && slot == this._resolvedLipSyncSlot)
                {
                    continue;
                }

                HideSlot(slot);
            }

            if (this._keepLipSync && this._resolvedEmotionMouthSlot != null)
            {
                HideSlot(this._resolvedEmotionMouthSlot);
            }
        }

        /// <summary>
        /// Renderer doc AppliedPose (duoc chep tu Pose trong UpdateWorldTransform) - an ca hai de khong phu
        /// thuoc thoi diem goi.
        /// </summary>
        private static void HideSlot(Slot slot)
        {
            slot.Attachment = null;
        }

        private void ResolveFaceSlots(Skeleton skeleton)
        {
            this._resolvedFor = skeleton;
            this._resolvedBaseFaceSlots.Clear();

            foreach (string slotName in this._baseFaceSlots ?? new string[0])
            {
                Slot slot = this.FindSlot(skeleton, slotName);
                if (slot != null)
                {
                    this._resolvedBaseFaceSlots.Add(slot);
                }
            }

            this._resolvedLipSyncSlot = this.FindSlot(skeleton, this._lipSyncMouthSlot);
            this._resolvedEmotionMouthSlot = this.FindSlot(skeleton, this._emotionMouthSlot);
        }

        private Slot FindSlot(Skeleton skeleton, string slotName)
        {
            if (string.IsNullOrEmpty(slotName))
            {
                return null;
            }

            Slot slot = skeleton.FindSlot(slotName);
            if (slot == null)
            {
                Debug.LogWarning($"[Playable] {this.name}: khong tim thay slot '{slotName}'.", this);
            }

            return slot;
        }

        private void AddSkinPart(Skin target, SkeletonData data, string skinName)
        {
            if (string.IsNullOrEmpty(skinName))
            {
                return;
            }

            Skin skin = data.FindSkin(skinName);
            if (skin == null)
            {
                Debug.LogWarning($"[Playable] {this.name}: khong tim thay skin '{skinName}', bo qua.", this);
                return;
            }

            target.AddSkin(skin);
        }

        // ------------------------------------------------------------------ TODO(debug): trace tam

        /// <summary>
        /// Moi frame khi track 0 dang la rap_idle: dem so lan attachment mieng lip-sync doi trong tung vong lap,
        /// dem so frame mieng bi null trong khi dang can giu lip-sync, va log tong ket moi khi sang vong moi
        /// hoac doi TrackEntry (tuc rap_idle duoc set lai).
        /// </summary>
        private void TraceLipSync(Skeleton skeleton)
        {
            if (!this._traceLipSync || skeleton == null)
            {
                return;
            }

            TrackEntry entry = this.State?.GetCurrent(0);
            bool isRap = entry?.Animation != null && entry.Animation.Name == PlayableAnimationNames.Man.RapIdle;

            if (!isRap)
            {
                if (this._traceEntry != null)
                {
                    this.FlushLoopTrace("rap_idle ended");
                    this._traceEntry = null;
                }

                return;
            }

            if (this._resolvedFor != skeleton)
            {
                this.ResolveFaceSlots(skeleton);
            }

            float duration = entry.Animation.Duration;
            int loop = duration > 0f ? Mathf.FloorToInt(entry.TrackTime / duration) : 0;

            if (entry != this._traceEntry)
            {
                if (this._traceEntry != null)
                {
                    this.FlushLoopTrace("new TrackEntry");
                }

                this._traceEntry = entry;
                this._traceLoop = loop;
                this.ResetLoopCounters();
                this.Trace($"rap_idle TrackEntry start | trackTime={entry.TrackTime:0.00} dur={duration:0.00} " +
                           $"loop={entry.Loop} timeScale={entry.TimeScale} mixDuration={entry.MixDuration:0.00}");
            }
            else if (loop != this._traceLoop)
            {
                this.FlushLoopTrace($"loop {this._traceLoop} -> {loop}");
                this._traceLoop = loop;
                this.ResetLoopCounters();
            }

            Attachment lipAttachment = this._resolvedLipSyncSlot?.Attachment;
            string lip = lipAttachment?.Name ?? "null";
            if (lip != this._traceLastLip)
            {
                this._traceLipChanges++;
                this._traceLastLip = lip;
            }

            if (lipAttachment == null && this._keepLipSync)
            {
                this._traceNullFrames++;
            }
        }

        private void FlushLoopTrace(string reason)
        {
            this.Trace($"rap_idle loop {this._traceLoop} summary ({reason}) | lipChanges={this._traceLipChanges} " +
                       $"nullLipFrames={this._traceNullFrames} lastLip='{this._traceLastLip}' " +
                       $"keepLipSync={this._keepLipSync} emotion={this._currentEmotion} " +
                       $"skin='{this.SpineSkeleton?.Skin?.Name}' " +
                       $"animEnabled={(this.SpineAnimation != null && this.SpineAnimation.enabled)} " +
                       $"rendererEnabled={(this.SpineRenderer != null && this.SpineRenderer.enabled)}");

            if (this._traceLipChanges <= 1)
            {
                Debug.LogWarning($"[Playable][Man] rap_idle loop {this._traceLoop}: mieng lip-sync KHONG doi ({reason}).",
                    this);
            }
        }

        private void ResetLoopCounters()
        {
            this._traceLipChanges = 0;
            this._traceNullFrames = 0;
            this._traceLastLip = null;
        }

        private void Trace(string message)
        {
            if (this._traceLipSync)
            {
                Debug.Log($"[Playable][Man][t={Time.realtimeSinceStartup:0.00}] {message}", this);
            }
        }
    }
}
