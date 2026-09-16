# Rap Star Playable

Clone Unity cua playable ad "Rap Star: Idle Clicker" (ban goc chay Cocos Creator 3.8.3).
Spec goc va bang doi chieu asset: `Docs/Playable-Clone-Plan.md`.

> **Vua import `.unitypackage`?** Doc `SETUP.md` truoc - cai dependency (Spine 4.3, TMP Essentials,
> Input System) roi moi chay duoc scene demo.

## Chay thu

Mo `Assets/_Playable/Scenes/PlayableDemo.unity` roi bam Play. Kich ban tu chay het 14 buoc.

Muon nhung vao scene khac: keo `Assets/_Playable/Prefabs/PB_PlayableRoot.prefab` vao scene.
Scene do phai co san mot **EventSystem** (prefab khong kem theo, vi input module tuy thuoc project
dung Input System moi hay cu).

## Cau truc prefab

| Prefab | Vai tro |
|---|---|
| `PB_PlayableRoot` | Prefab tong: flow, audio, exit, Canvas va toan bo UI |
| `PlayableMainMap` | Nen + vat the cua map (nha, xe, san, loa, tuong, media) + hai nhan vat Spine (man, woman_1, woman_2). World space, con cua root, nam ngoai Canvas |
| `PB_Bubble` | Bong thoai / bong goi y - 5 ban trong root |
| `PB_ChoiceButton` | Nut lua chon - 16 ban chia thanh 6 nhom |
| `PB_Emoji_*` | 7 loai emoji bay len khi tap |

### PlayableMainMap

Tach rieng theo dung cach `Assets/_Project/Addressables/Prefabs/StoryScene/MainMap.prefab` lam:
mot prefab gom day du vat the cua map, mot component (`PlayableMainMap`) giu bang slot -> renderer,
ben ngoai chi bao "slot nay len level may".

Giong `MainMap` goc: nam trong **world space**, moi slot la mot `SpriteRenderer` duoc Main Camera
nhin. Vi tri va sorting order chep dung tu `MainMap.prefab` (background -1, floor -2, con lai 0).
Camera trong scene demo lay dung thong so camera map cua game: orthographic, size 10.8, z = -10.
UI (Canvas overlay) ve de len tren map.

Nhan vat (`PlayableManView`, `PlayableWomanView`) cung nam tren map, dung `SkeletonRenderer` +
`SkeletonAnimation` (MeshRenderer) nhu prefab nhan vat cua game. Nam dat o
`StorySceneConstant.MainCharacterPosition` (-2.13, -7.85); nu dat ben phai (1.4, -7.85).
Sorting order 10 tren layer Default - **khong** dung sorting layer `Character` cua game vi sorting layer
nam trong ProjectSettings, khong di theo .unitypackage.
Moi khoang lech tween cua nhan vat (`GirlEnterOffsetX`, `AngerExitOffsetX`) tinh bang don vi world.

Khac `MainMap` goc chi o phan du lieu, do rang buoc package doc lap: sprite gan san theo mang trong
Inspector thay vi load qua Addressables, va bang slot la mang `PlayableMapSlotBinding` thay vi
`Dictionary` cua Odin.

Moi slot co mang `Levels`: index 0 la trang thai khoi dau, 1..3 la ba ban nang cap mua duoc.
Muon doi bo art cua map thi chi can keo sprite khac vao mang nay, khong dong vao code.

**Hieu ung doi vat the:** truot ngang theo X cua chinh slot do. Clone SpriteRenderer mang sprite moi dat
tai `(x - offset, y)` roi truot ve `(x, y)` cua slot goc; cung luc vat the cu truot tu `(x, y)` ra
`(x + offset, y)`. Vi du car goc `(1.3, 1.14)`: clone tu `(-3.7, 1.14)` ve `(1.3, 1.14)`, car cu ra
`(6.3, 1.14)`. Xong thi renderer goc nhan sprite moi, ve cho cu va clone bi huy.
`offset` = `_slideOffsetX` tren `PlayableMainMap` (mac dinh 5); thoi gian = `PropSlideDuration` trong
`PlayableConfig`.

**Luat khi mua nha (chon nha thu N):** moi slot tren map, **tru `car`**, cung doi sang `Levels[N]`.
Slot nao khong co phan tu `N` (mang ngan hon, hoac phan tu do de trong/null) thi **bi an**.
`car` chi doi khi mua xe. Vi du voi cau hinh hien tai: `background`, `house`, `floor` co du 4 level nen
doi hinh; `backyard`, `media`, `speaker`, `statue` chi co level 0 nen se an sau khi mua nha.
Muon mot slot giu hien thi thi them sprite vao dung vi tri trong mang `Levels` cua no.

> Luu y: prefab dang duoc chinh tay, builder (`Tools > Playable > Rebuild Scene And Prefabs`) KHONG con
> khop voi prefab. Chay lai lenh do se ghi de mat cac chinh sua tay.
Icon tren nut mua xe/nha cung doc tu chinh mang nay nen khong bao gio lech voi vat the hien ra.

## Cam ket noi voi host

`PlayableExit` tren `PB_PlayableRoot` ban su kien khi nguoi choi bam logo, bam Download, hoac chon
xong o buoc cuoi. Cam mot trong hai cach:

- Keo ham cua ban vao `UnityEvent OnExitRequested` trong Inspector, hoac
- Them mot component implement `IPlayableExitHandler` vao chinh GameObject `PB_PlayableRoot`.

Ngoai ra `PlayableFlow.Bus` cho phep nghe `Touch`, `MoneyFull`, `GameOver`.

## Tinh chinh so lieu

Tat ca nam trong `Assets/_Playable/Config/PlayableConfig.asset` - tien khoi diem, muc tieu, tien moi
lan tap, gia tung mon, va toan bo moc thoi gian cua kich ban.

## Asset con thieu - can bo sung

Nhung cho nay dang chay bang do thay the. Khi co file that thi sua dung mot cho, khong can dong vao
logic:

| Thieu | Dang tam dung | Sua o dau |
|---|---|---|
| Thoai nam khoc | `sfx_talk.mp3` | Inspector `PlayableAudio` tren `PB_PlayableRoot`, dong `ManCrying` |
| Thoai nam vui | `sfx_talk.mp3` | dong `ManHappy` |
| Thoai nu gian | `sfx_talk.mp3` | dong `WomanAngry` |
| Thoai nu hai long | `sfx_talk.mp3` | dong `WomanSatisfied` |
| Sprite dien thoai + anim Phone_* | icon dia nhac `disc_101` + tween | prefab con `TapTarget` trong `PB_PlayableRoot` |
| Chu NICE / AWESOME dang anh | TMP + tween scale | node `Fx/Praise` trong `PB_PlayableRoot` |

## Sinh lai prefab va scene

`Tools > Playable > Rebuild Scene And Prefabs` dung lai toan bo tu code.

**Ghi de sach** - moi tinh chinh tay tren prefab se mat. Sau khi da chinh tay thi dung chay lai nua.

Hai lenh phu:

- `Tools > Playable > Validate Wiring` - canh bao moi tham chieu bi bo trong tren `PB_PlayableRoot`.
- `Tools > Playable > Optimize Spine Textures` - thu nho file PNG nguon cua atlas Spine xuong 2048.
  Chi chay mot lan sau khi copy art (da chay roi: character1.png 14.6 MB -> 5.1 MB).

## Export .unitypackage doc lap

1. **Xoa `Assets/_Playable/Editor/PlayableAgentBridge.cs`** - file nay chi la ha tang de agent tu chay
   builder, khong lien quan gi den playable.
2. Chuot phai `Assets/_Playable` > **Export Package...**
3. **Bo tick "Include dependencies"** - moi thu can thiet da nam san trong thu muc nay; bat len se keo
   theo ca `Assets/Spine/Runtime` va atlas chung cua game.

Ben nhan package can: **Unity 6**, **spine-unity 4.3** (unitypackage chinh thuc) + **spine-csharp 4.3**
(UPM), **TMP Essential Resources**, **UGUI**, va **Input System** neu dung scene demo.
Huong dan day du cho ben nhan: `SETUP.md`.

## Cam ket ve do doc lap

`_Playable.Runtime.asmdef` de `autoReferenced: false` va chi tham chieu `spine-unity`, `spine-csharp`,
`Unity.TextMeshPro`, `UnityEngine.UI`.

`_Framework` va `_Project` deu nam trong `Assembly-CSharp` (khong co asmdef), nen trinh bien dich
**khong cho phep** code trong `_Playable` nhin thay chung. Khong phai quy uoc - la rang buoc bien dich.

Tuong tu: khong DOTween, khong Odin, khong UniTask, khong Addressables, khong MessageBus.
Tween tu viet o `Runtime/Core/PlayableTween.cs`.
