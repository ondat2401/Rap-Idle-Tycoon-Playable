namespace _Playable.Runtime.Core
{
    /// <summary>
    /// Tin hieu cat ngang giua cac he thong cua playable. Thay cho EventTags cua ban Cocos goc.
    /// Chi giu lai nhung tin hieu that su co nhieu hon mot nguoi nghe - phan con lai cua kich ban
    /// do <c>PlayableFlow</c> dieu phoi truc tiep bang coroutine cho de doc.
    /// </summary>
    public enum PlayableSignal
    {
        None = 0,

        /// <summary>Nguoi choi vua cham man hinh. Guide hand va sfx click nghe tin hieu nay.</summary>
        Touch = 1,

        /// <summary>So tien hien thi vua cham moc muc tieu.</summary>
        MoneyFull = 2,

        /// <summary>Playable ket thuc - host nen mo store.</summary>
        GameOver = 3
    }
}
