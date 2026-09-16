using UnityEngine;

namespace _Playable.Runtime.Config
{
    /// <summary>
    /// Cau hinh text hien thi cua playable: tieu de cac nut lua chon va noi dung bong bong.
    /// Tach rieng khoi PlayableConfig de dan text de dang qua Inspector, khong dung cung so lieu kinh te.
    ///
    /// Chi gom cac muc can chinh text theo yeu cau:
    ///  - Choices_Dialog: 2 nut tra loi.
    ///  - Choices_Car: 3 nut xe.
    ///  - Choices_Pardon: 2 nut tha thu.
    ///  - Bubble_RapHint / Bubble_PraiseA / Bubble_PraiseB.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayableTextConfig", menuName = "Playable/Playable Text Config", order = 1)]
    public sealed class PlayableTextConfig : ScriptableObject
    {
        [Header("Choices_Dialog (nut tra loi)")]
        [Tooltip("Tieu de tung nut nhom thoai. Thu tu khop thu tu nut.")]
        [SerializeField]
        private string[] _dialogTitles = { "I will make it big", "Give me some time" };

        [Header("Choices_Car (nut mua xe)")]
        [Tooltip("Tieu de tung nut xe. De trong thi khong hien chu.")]
        [SerializeField]
        private string[] _carTitles = { string.Empty, string.Empty, string.Empty };

        [Header("Choices_Pardon (nut tha thu)")]
        [Tooltip("Tieu de tung nut tha thu. Thu tu khop thu tu nut.")]
        [SerializeField]
        private string[] _pardonTitles = { "Forgive her", "Walk away" };

        [Header("Bong bong")]
        [SerializeField] private string _bubbleRapHint = "Tap to drop a beat!";
        [SerializeField] private string _bubblePraiseA = "He is on fire!";
        [SerializeField] private string _bubblePraiseB = "That flow is insane!";

        public string[] DialogTitles => this._dialogTitles;

        public string[] CarTitles => this._carTitles;

        public string[] PardonTitles => this._pardonTitles;

        public string BubbleRapHint => this._bubbleRapHint;

        public string BubblePraiseA => this._bubblePraiseA;

        public string BubblePraiseB => this._bubblePraiseB;
    }
}
