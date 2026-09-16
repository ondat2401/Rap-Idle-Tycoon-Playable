using UnityEngine;

namespace _Playable.Runtime.Config
{
    /// <summary>
    /// Toan bo so lieu tinh chinh cua playable. ScriptableObject thuan - khong dung Odin, khong CSV,
    /// de package export ra .unitypackage khong keo theo dependency nao.
    ///
    /// Gia tri mac dinh trich dung tu ban playable goc (Docs/Playable-Clone-Plan.md muc 1.2).
    /// </summary>
    [CreateAssetMenu(fileName = "PlayableConfig", menuName = "Playable/Playable Config", order = 0)]
    public sealed class PlayableConfig : ScriptableObject
    {
        [Header("Kinh te")]
        [SerializeField] private long _startMoney = 5_500L;
        [SerializeField] private long _moneyGoal = 999_999L;
        [SerializeField] private long _tapReward = 62_500L;

        [Header("Gia")]
        [SerializeField] private long _priceGirlA = 5_000L;
        [SerializeField] private long _priceGirlB = 150L;
        [SerializeField] private long _priceCar = 235_322L;
        [SerializeField] private long _priceHouse = 546_654L;
        [SerializeField] private long _priceClothes = 123_456L;

        [Header("Thoi gian")]
        [SerializeField] private PlayableTiming _timing = PlayableTiming.Default;

        [Header("Guide hand")]
        [Tooltip("De yen bao lau khong cham thi hien ban tay.")]
        [SerializeField] private float _guideIdleTimeout = 3f;

        [Tooltip("Doi target moi bao lau khi co nhieu hon mot muc tieu.")]
        [SerializeField] private float _guideSwitchInterval = 2f;

        [Tooltip("Bien do pulse scale cua target (ban goc: 0.05).")]
        [SerializeField] private float _guidePulse = 0.05f;

        [Tooltip("Bien do pulse rieng cho tap target (ban goc: 0.02).")]
        [SerializeField] private float _guidePulseSmall = 0.02f;

        [Header("Emoji khi tap")]
        [SerializeField] private float _emojiRiseDistance = 300f;
        [SerializeField] private float _emojiSpreadX = 250f;
        [SerializeField] private float _emojiDuration = 0.5f;

        [Header("Skin nhan vat nam")]
        [Tooltip("Bo skin luc mo dau - nhan vat con ngheo. Ten phai co trong skeleton (mic_/outfit_/head_/jewel_).")]
        [SerializeField]
        private PlayableSkinSet _startSkin = new PlayableSkinSet("mic_1", "outfit_3", "head_1", "jewel_3");

        [Tooltip("Cac bo skin ung voi cac lua chon quan ao. Ten phai co trong skeleton hien tai.")]
        [SerializeField]
        private PlayableSkinSet[] _clothesSkins =
        {
            new PlayableSkinSet("mic_4", "outfit_9", "head_7", "jewel_9"),
            new PlayableSkinSet("mic_5", "outfit_9", "head_7", "jewel_9"),
            new PlayableSkinSet("mic_6", "outfit_9", "head_7", "jewel_9")
        };

        public long StartMoney => this._startMoney;

        public long MoneyGoal => this._moneyGoal;

        public long TapReward => this._tapReward;

        public long PriceGirlA => this._priceGirlA;

        public long PriceGirlB => this._priceGirlB;

        public long PriceCar => this._priceCar;

        public long PriceHouse => this._priceHouse;

        public long PriceClothes => this._priceClothes;

        public PlayableTiming Timing => this._timing;

        public float GuideIdleTimeout => this._guideIdleTimeout;

        public float GuideSwitchInterval => this._guideSwitchInterval;

        public float GuidePulse => this._guidePulse;

        public float GuidePulseSmall => this._guidePulseSmall;

        public float EmojiRiseDistance => this._emojiRiseDistance;

        public float EmojiSpreadX => this._emojiSpreadX;

        public float EmojiDuration => this._emojiDuration;

        public PlayableSkinSet StartSkin => this._startSkin;

        /// <summary>Gia cua hai lua chon ban gai, theo dung thu tu nut trong nhom.</summary>
        public long GetGirlPrice(int index)
        {
            return index == 0 ? this._priceGirlA : this._priceGirlB;
        }

        public PlayableSkinSet GetClothesSkin(int index)
        {
            if (this._clothesSkins == null || this._clothesSkins.Length == 0)
            {
                return this._startSkin;
            }

            int clamped = Mathf.Clamp(index, 0, this._clothesSkins.Length - 1);
            return this._clothesSkins[clamped];
        }
    }
}
