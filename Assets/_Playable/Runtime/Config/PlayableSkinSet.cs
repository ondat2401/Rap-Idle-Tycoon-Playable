using System;
using UnityEngine;

namespace _Playable.Runtime.Config
{
    /// <summary>
    /// Mot bo skin ghep cua nhan vat nam. Skeleton <c>character1</c> tach nhan vat thanh 4 part skin
    /// doc lap (<c>mic_*</c>, <c>outfit_*</c>, <c>head_*</c>, <c>jewel_*</c>) nen phai gop lai thanh
    /// mot Skin duy nhat truoc khi SetSkin.
    ///
    /// Ban goc dung 3 node nhan vat rieng cho 3 lua chon quan ao; ban nay chi doi skin tren cung mot
    /// SkeletonGraphic - nhe hon va it prefab hon.
    /// </summary>
    [Serializable]
    public struct PlayableSkinSet
    {
        [Tooltip("mic_1 .. mic_3")]
        public string Mic;

        [Tooltip("outfit_1 .. outfit_15")]
        public string Outfit;

        [Tooltip("head_1 .. head_13")]
        public string Head;

        [Tooltip("jewel_1 .. jewel_15")]
        public string Jewel;

        public PlayableSkinSet(string mic, string outfit, string head, string jewel)
        {
            this.Mic = mic;
            this.Outfit = outfit;
            this.Head = head;
            this.Jewel = jewel;
        }
    }

    /// <summary>Hai bo skin khoi dau cua nhan vat nam, chon 1 trong 2 luc init.</summary>
    public enum PlayableManSkinOption
    {
        SkinA = 0,
        SkinB = 1
    }

    /// <summary>Ten skin co san trong skeleton <c>character1</c>, dung lam gia tri mac dinh.</summary>
    public static class PlayableSkinNames
    {
        public const string EmotionNormal = "emo_normal";
        public const string EmotionHappy = "emo_happy";
        public const string EmotionSad = "emo_sad";
        public const string EmotionSurprise = "emo_surprise";
    }
}
