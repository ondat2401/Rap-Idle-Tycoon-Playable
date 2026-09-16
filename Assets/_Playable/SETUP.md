# Rap Star Playable - Huong dan setup sau khi import

Tai lieu nay danh cho nguoi nhan file `.unitypackage` cua thu muc `Assets/_Playable`.
Lam dung thu tu ben duoi thi mo `Scenes/PlayableDemo.unity` va bam Play la chay duoc.

---

## 1. Yeu cau

| Thanh phan | Phien ban | Bat buoc | Dung de lam gi |
|---|---|---|---|
| Unity | **6000.3.x** (da test tren 6000.3.20f1) | Co | |
| spine-unity runtime | **4.3** (ban `spine-unity-4.3-2026-07-29`) | Co | Nhan vat Spine (`SkeletonRenderer`, `SkeletonAnimation`, shader `Spine/Skeleton`) |
| spine-csharp | **4.3** (UPM package) | Co | Thu vien loi Spine ma spine-unity va code playable dung |
| uGUI + TextMeshPro | `com.unity.ugui` 2.x (co san trong Unity 6) | Co | Canvas, Button, Image, TextMeshProUGUI |
| TMP Essential Resources | di kem Unity 6 | Co | Shader cua font `Lalezar-Regular SDF` |
| Input System | `com.unity.inputsystem` | Chi cho scene demo | EventSystem cua scene demo dung `InputSystemUIInputModule` |
| Universal RP | `com.unity.render-pipelines.universal` | Khong | Camera scene demo co component `UniversalAdditionalCameraData` |

Package **khong** can: DOTween, Odin, UniTask, Addressables, Firebase, hay bat ky code nao cua game goc.
Code playable nam trong asmdef rieng `_Playable.Runtime`, chi tham chieu `spine-unity`, `spine-csharp`,
`Unity.TextMeshPro`, `UnityEngine.UI`.

---

## 2. Cai dat - lam TRUOC khi import package

Neu import `_Playable` truoc khi co Spine, Unity se bao loi compile va cac component Spine tren prefab se
thanh "Missing Script". Vi vay cai dependency truoc.

### 2.1 Spine 4.3

Can **ca hai** phan, dung phien ban 4.3:

1. **spine-csharp** (UPM). Them vao `Packages/manifest.json`:

   ```json
   "com.esotericsoftware.spine.spine-csharp": "https://github.com/EsotericSoftware/spine-runtimes.git?path=spine-csharp/src#4.3"
   ```

2. **spine-unity** (runtime + editor). Tai ban **spine-unity 4.3** `.unitypackage` tu trang download chinh thuc
   cua Esoteric Software roi import vao project (se tao thu muc `Assets/Spine`).

   Package playable tham chieu script va shader Spine **bang GUID**. GUID chi khop khi dung ban chinh thuc cua
   Esoteric Software. Neu tu build hay sua lai spine-unity thi prefab se bi "Missing Script".

Kiem tra: trong project phai co hai assembly ten dung la `spine-unity` va `spine-csharp`
(asmdef cua `_Playable` tham chieu theo ten nay).

Tuy chon: `com.esotericsoftware.spine.urp-shaders` (4.3). Package playable **khong** dung, vi material Spine
dung shader `Spine/Skeleton` co san trong spine-unity.

### 2.2 TextMeshPro Essential Resources

`Window > TextMeshPro > Import TMP Essential Resources`.

Font `Art/Font/Lalezar-Regular SDF.asset` va material cua no dung shader `TMP_SDF-Mobile SSD` trong
`Assets/TextMesh Pro/Shaders`. Thieu buoc nay thi chu se bi hong mau hoac khong hien.

### 2.3 Input System (chi can neu dung scene demo)

1. `Window > Package Manager` > cai **Input System**.
2. `Edit > Project Settings > Player > Other Settings > Active Input Handling` = **Input System Package (New)**
   hoac **Both**.

Neu project cua ban dung input cu va khong muon cai Input System: mo scene demo, chon GameObject
`EventSystem`, xoa component `InputSystemUIInputModule` (se bao Missing Script) va them
`Standalone Input Module`.

### 2.4 Render pipeline

Package chay duoc tren ca **URP** va **Built-in**:

- Sprite map dung material mac dinh `Sprites-Default`, Spine dung `Spine/Skeleton`, UI dung Canvas overlay.
- Camera scene demo co component `UniversalAdditionalCameraData`. Tren Built-in no hien "Missing Script" -
  chi can xoa component do tren `Main Camera`, khong anh huong gi.

Khong can tao sorting layer hay tag rieng: moi renderer dung layer `Default`, camera dung tag co san `MainCamera`.

---

## 3. Import package

1. `Assets > Import Package > Custom Package...` > chon file `.unitypackage`.
2. Import toan bo (thu muc `Assets/_Playable`).
3. Doi Unity compile xong, Console **khong** duoc co loi do. Neu co, xem muc 6.

---

## 4. Chay scene demo

1. Mo `Assets/_Playable/Scenes/PlayableDemo.unity`.
2. Trong Game view chon do phan giai **doc 1080x1920** (hoac ti le 9:16).
   UI dung `CanvasScaler` tham chieu 1080x1920; camera map orthographic size 10.8.
3. Bam **Play**. Kich ban tu chay: chon ban gai > thoai > chia tay > tap de rap kiem tien > mua xe > mua nha >
   mua do > ban gai quay lai > tha thu hay khong > end card.

Thao tac: bam cac nut lua chon va bam vao dia nhac de kiem tien. De yen khoang 3 giay se hien ban tay huong dan.

---

## 5. Dung trong scene cua ban

1. Keo `Assets/_Playable/Prefabs/PB_PlayableRoot.prefab` vao scene.
2. Scene phai co **EventSystem** (prefab khong kem theo, vi input module tuy project). Tao bang
   `GameObject > UI > Event System`.
3. Scene phai co camera tag `MainCamera`, **orthographic**, size **10.8**, dat o `(0, 0, -10)` - map va nhan vat
   nam trong world space tai goc toa do.
4. Noi ket thuc playable voi host (mo store, MRAID...): tren `PB_PlayableRoot`, component `PlayableExit`
   - keo ham vao `On Exit Requested`, hoac
   - them component implement `IPlayableExitHandler` vao cung GameObject.

Tinh chinh so lieu (tien, gia, thoi gian) trong `Assets/_Playable/Config/PlayableConfig.asset`.
Chi tiet cau truc prefab va logic xem `README.md` cung thu muc.

---

## 6. Loi thuong gap

| Hien tuong | Nguyen nhan | Cach sua |
|---|---|---|
| Loi compile `The type or namespace name 'Spine' could not be found` / assembly `spine-unity` khong ton tai | Chua cai Spine, hoac asmdef Spine khong dung ten | Lam muc 2.1 roi reimport `_Playable` |
| "Missing Script" tren `skeleton` cua `man` / `woman_*` trong `PlayableMainMap` | spine-unity khong phai ban chinh thuc 4.3 (lech GUID) | Cai lai spine-unity 4.3 chinh thuc |
| Nhan vat mau hong | Thieu shader `Spine/Skeleton` | Cai lai spine-unity 4.3 day du |
| Nhan vat khong hien, khong loi | Camera khong phai orthographic size 10.8 tai `(0,0,-10)`, hoac Game view sai ti le | Xem muc 4 buoc 2 va muc 5 buoc 3 |
| Chu mau hong / khong hien | Chua import TMP Essential Resources | Lam muc 2.2 |
| Bam nut khong an | Scene khong co EventSystem, hoac input module khong hop voi Active Input Handling | Muc 2.3 / muc 5 buoc 2 |
| Loi `You are trying to read Input using the UnityEngine.Input class...` | Dung `Standalone Input Module` khi project chi bat Input System moi | Doi Active Input Handling sang Both, hoac dung `InputSystemUIInputModule` |
| "Missing Script" tren `Main Camera` | Project dung Built-in RP | Xoa component thieu (muc 2.4) |
| Khong co tieng | Mot so clip thoai dang de tam `sfx_talk` | Binh thuong - xem bang "Asset con thieu" trong `README.md` |

---

## 7. Luu y cho nguoi phat trien tiep

- **Khong** chay `Tools > Playable > Rebuild Scene And Prefabs`. Prefab da duoc chinh tay sau khi sinh; lenh nay
  sinh lai tu code va **ghi de mat** moi chinh sua.
- `Tools > Playable > Validate Wiring` an toan - chi canh bao tham chieu bi bo trong.
- `PlayableManView` co field debug `Trace Lip Sync` (log `[Playable][Man]`). Tat di khi khong can.
