namespace _Playable.Runtime.Config
{
    /// <summary>
    /// Ten animation Spine dung trong playable.
    ///
    /// Day la NOI DUY NHAT can sua khi bo sung file Spine moi - cac view chi doc hang so o day.
    /// Nhung hang so co ghi chu TODO(asset) dang tro tam sang mot animation khac vi skeleton hien tai
    /// chua co animation dung nghia.
    /// </summary>
    public static class PlayableAnimationNames
    {
        /// <summary>
        /// Nhan vat nam - skeleton <c>character1</c> (_Project/Spine/c_main). Du toan bo 5 state ban goc can.
        /// </summary>
        public static class Man
        {
            /// <summary>Ban goc: "Idle".</summary>
            public const string Idle = "no_rap_idle";

            /// <summary>Ban goc: "Idle-Sad". Dung luc bi bo, khoc.</summary>
            public const string Sad = "no_rap_sad";

            /// <summary>Ban goc: "Rap". Loop trong suot giai doan kiem tien.</summary>
            public const string RapIdle = "rap_idle";

            /// <summary>Ban goc: "Upgrade_1". An mung khi mua xe / nha.</summary>
            public const string Upgrade1 = "no_rap_boost";

            /// <summary>Ban goc: "Upgrade_2". An mung khi thay do.</summary>
            public const string Upgrade2 = "no_rap_idle";
        }

        /// <summary>
        /// Nhan vat nu - skeleton <c>npc_1..npc_5</c> (_Project/Addressables/Spine/NPC_*).
        /// </summary>
        public static class Woman
        {
            /// <summary>Ban goc: "Idle".</summary>
            public const string Idle = "idle";

            /// <summary>
            /// Ban goc: "Anger".
            /// TODO(asset): skeleton npc_* hien KHONG co animation gian du. Tam tro ve idle - phan "gian"
            /// duoc the hien bang bong bong gian + tween truot ra khoi man hinh trong PlayableWomanView.
            /// Khi co file Spine moi thi doi chuoi nay thanh ten animation that (vd "anim_lover_angry").
            /// </summary>
            public const string Anger = "idle";

            /// <summary>Ban goc: "ComeBack". Dung anim chao mung khi quay lai.</summary>
            public const string ComeBack = "kiss";

            /// <summary>Ban goc: "ComeBack-Idle".</summary>
            public const string ComeBackIdle = "kiss";
        }
    }
}
