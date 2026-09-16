namespace _Playable.Runtime.Core
{
    /// <summary>
    /// Danh sach am thanh cua playable, doi chieu 1:1 voi SOUND_EVENT cua ban Cocos goc.
    ///
    /// TODO(asset): du an hien chua co 4 clip thoai nhan vat (<see cref="ManCrying"/>, <see cref="ManHappy"/>,
    /// <see cref="WomanAngry"/>, <see cref="WomanSatisfied"/>). De trong slot tuong ung trong
    /// <c>PlayableAudio</c> - he thong tu bo qua. Khi co file thi keo vao Inspector, khong can sua code.
    /// </summary>
    public enum PlayableSfx
    {
        None = 0,

        /// <summary>Cham nut / cham man hinh. Nguon: Audio/Sfx/sfx_button_2.ogg</summary>
        Click = 1,

        /// <summary>Nhan tien. Nguon: Audio/Sfx/sfx_cash_2.ogg</summary>
        Money = 2,

        /// <summary>Tieng "leng keng" khi mon do moi hien ra. Nguon: Audio/Sfx/sfx_ting.mp3</summary>
        Shiny = 3,

        /// <summary>Chuyen canh. Nguon: Audio/Sfx/sfx_beat_switch_2.mp3</summary>
        Whoosh = 4,

        /// <summary>Dat mon do xuong. Nguon: tam dung sfx_close_modal_2.ogg</summary>
        PutDown = 5,

        /// <summary>TODO(asset): chua co clip. Nam khoc.</summary>
        ManCrying = 6,

        /// <summary>TODO(asset): chua co clip. Nam vui.</summary>
        ManHappy = 7,

        /// <summary>TODO(asset): chua co clip. Nu gian.</summary>
        WomanAngry = 8,

        /// <summary>TODO(asset): chua co clip. Nu hai long.</summary>
        WomanSatisfied = 9,

        /// <summary>Beat nen lap. Nguon: Audio/Music/beat_101.ogg</summary>
        BeatLoop = 10,

        /// <summary>Giong rap lap. Nguon: Audio/Music/vocal_101.ogg</summary>
        Vocal = 11
    }
}
