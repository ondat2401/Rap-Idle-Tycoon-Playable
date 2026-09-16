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
        /// Nhan vat nu - skeleton lover_3 / lover_4 (Art/Spine/Lover*).
        /// Animation co san: idle, angry, happy, sad, dance, kiss.
        /// </summary>
        public static class Woman
        {
            /// <summary>Ban goc: "Idle".</summary>
            public const string Idle = "idle";

            /// <summary>
            /// Ban goc: "Anger". Skeleton co san animation "angry" - phat luc nu gian roi bo di,
            /// ket hop bong bong gian + tween truot ra khoi man hinh trong PlayableWomanView.
            /// </summary>
            public const string Anger = "angry";

            /// <summary>Ban goc: "ComeBack". Phat "kiss" mot lan luc quay lai lam hoa.</summary>
            public const string ComeBack = "kiss";

            /// <summary>Ban goc: "ComeBack-Idle". Loop "happy" - idle vui ve sau khi quay lai.</summary>
            public const string ComeBackIdle = "happy";
        }
    }
}
