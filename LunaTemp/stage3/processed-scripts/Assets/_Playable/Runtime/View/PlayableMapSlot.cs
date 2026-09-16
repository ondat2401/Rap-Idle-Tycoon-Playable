namespace _Playable.Runtime.View
{
    /// <summary>
    /// Cac slot vat the tren map, dat theo dung bo slot cua
    /// <c>Assets/_Project/Addressables/Prefabs/StoryScene/MainMap.prefab</c>.
    ///
    /// Khac ban goc o cho: MainMap dinh danh slot bang <c>upgradeId</c> (int, khop CSV cua game),
    /// con playable khong co CSV nen dung enum cho ro nghia va khoi phai tra bang.
    /// Khong bat buoc prefab phai gan du moi slot - slot khong gan thi bi bo qua.
    /// </summary>
    public enum PlayableMapSlot
    {
        Background = 0,
        Backyard = 1,
        House = 2,
        Floor = 3,
        Car = 4,
        Speaker = 5,
        Statue = 6,
        Media = 7,
        Mic = 8
    }
}
