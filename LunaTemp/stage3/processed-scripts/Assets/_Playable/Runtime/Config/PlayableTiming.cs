using System;
using UnityEngine;

namespace _Playable.Runtime.Config
{
    /// <summary>
    /// Cac moc thoi gian cua kich ban. Gia tri mac dinh lay dung tu ban playable goc
    /// (xem Docs/Playable-Clone-Plan.md muc 1.3).
    /// </summary>
    [Serializable]
    public struct PlayableTiming
    {
        [Header("Buoc 1 - chon ban gai")]
        [Tooltip("Thoi gian nu truot tu ngoai man hinh vao cho.")]
        public float GirlEnterDuration;

        [Tooltip("Khoang lech X (don vi world) ma nu bat dau truot vao. Ban goc 1000px ~ 11.25 unit.")]
        public float GirlEnterOffsetX;

        [Header("Buoc 2 - thoai")]
        [Tooltip("Cho bao lau sau khi nu vao cho thi hien 2 nut thoai.")]
        public float DialogButtonDelay;

        [Header("Buoc 3 - chia tay")]
        [Tooltip("Cho bao lau sau khi chon thoai thi hien bong gian du.")]
        public float AngerBubbleDelay;

        [Tooltip("Cho bao lau thi nu bat dau gian.")]
        public float AngerStartDelay;

        [Tooltip("Cho bao lau thi nam bat dau khoc.")]
        public float CryStartDelay;

        [Tooltip("Nam khoc bao lau roi ve idle.")]
        public float CryDuration;

        [Tooltip("Nu truot ra khoi man hinh trong bao lau (thay cho animation Anger chua co).")]
        public float AngerExitDuration;

        [Tooltip("Khoang lech X (don vi world) ma nu truot ra. Ban goc 200px ~ 2.25 unit.")]
        public float AngerExitOffsetX;

        [Header("Buoc 8 - tien day")]
        [Tooltip("Cho bao lau sau khi tien day thi cac nut mua moi nhan tap duoc.")]
        public float ChoiceArmDelay;

        [Header("Buoc 9-11 - mua do")]
        [Tooltip("Thoi gian vat the moi truot vao tu ben trai va vat the cu truot ra ben phai (chay cung luc).")]
        public float PropSlideDuration;

        [Tooltip("Cho bao lau sau khi do moi hien thi keu tieng leng keng.")]
        public float ShinyDelay;

        [Tooltip("Cho bao lau sau khi mua thi nam an mung.")]
        public float CelebrateDelay;

        [Tooltip("Cho bao lau sau khi mua thi hien chu NICE.")]
        public float PraiseDelay;

        [Header("Buoc 11-12 - quay lai")]
        [Tooltip("Cho bao lau sau khi nam thay do xong thi nu quay lai.")]
        public float ComeBackDelay;

        [Header("Chung")]
        [Tooltip("Thoi gian tween so tien.")]
        public float MoneyTweenDuration;

        [Tooltip("Nua chu ky cua cac tween loop (bong bong, guide hand).")]
        public float LoopHalfDuration;

        public static PlayableTiming Default => new PlayableTiming
        {
            GirlEnterDuration = 1f,
            GirlEnterOffsetX = 11.25f,
            DialogButtonDelay = 0.5f,
            AngerBubbleDelay = 0.5f,
            AngerStartDelay = 1f,
            CryStartDelay = 0.15f,
            CryDuration = 3.3f,
            AngerExitDuration = 0.4f,
            AngerExitOffsetX = 2.25f,
            ChoiceArmDelay = 1f,
            PropSlideDuration = 0.45f,
            ShinyDelay = 0.2f,
            CelebrateDelay = 0.3f,
            PraiseDelay = 0.2f,
            ComeBackDelay = 0.1f,
            MoneyTweenDuration = 0.5f,
            LoopHalfDuration = 0.5f
        };
    }
}
