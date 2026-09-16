# Luna Field Generator

Tool tạo và quản lý responsive layout cho Luna playable ads trong Unity Editor.

## Installation (UPM Git URL)

> ⚠️ **Phải install dependencies theo đúng thứ tự!** UPM git packages không tự resolve dependencies.

Mở **Window → Package Manager → + → Add package from git URL**, install lần lượt:

**1. Playable Core (bắt buộc):**
```
https://gitlab.amanotes.net/dat.ot/tool-builder.git#com.amanotes.playable-core
```

**2. Orientation Tracker (bắt buộc):**
```
https://gitlab.amanotes.net/dat.ot/tool-builder.git#com.amanotes.playable-orientation-tracker
```

**3. Luna Field Generator:**
```
https://gitlab.amanotes.net/dat.ot/tool-builder.git#com.amanotes.playable-luna-field-generator
```

> Nếu thiếu bất kỳ package nào ở trên, Unity sẽ báo lỗi `The type or namespace name 'OrientationTracking' could not be found`.

---

## ⚠️ Điều kiện bắt buộc: Luna SDK Package

Project phải import **Luna Playground SDK** trước khi sử dụng tool này.
Các field script dùng attribute `[LunaPlaygroundField]` và `[LunaPlaygroundAsset]` từ Luna SDK — nếu thiếu package, toàn bộ script sẽ không compile được.

---

## Yêu cầu: Gắn OrientationTracker

`LunaFieldController` cần `OrientationTracker` đã được khởi tạo để detect phone/tablet và portrait/landscape.

1. Tạo một GameObject trong scene, đặt tên `OrientationTracker` (hoặc bất kỳ tên nào)
2. Gắn component `OrientationTrackerInitializer` vào GameObject đó
3. Giữ nguyên mặc định `Use Unity Screen API = true` — tracker sẽ tự detect từ `Screen.width/height`
4. Đảm bảo GameObject này tồn tại **trước** `LunaFieldController` trong thứ tự Awake (đặt trên hierarchy hoặc dùng Script Execution Order)

> Nếu chưa có tracker khi Play, Editor tool vẫn hoạt động nhưng capture sẽ fallback về Phone và dùng Game View size để detect portrait/landscape.

---

## Bắt đầu

1. Gắn component `LunaFieldController` vào một GameObject trong scene
2. Mở Inspector của GameObject đó → tool hiện ra với 4 tab

---

## Tạo field mới

**Tab "Create Field"**

1. Nhập tên vào ô "Base Name" (ví dụ: `Logo` → sẽ tạo `Logo_LunaField.cs`)
2. _(Tùy chọn)_ Kéo thả folder từ Project vào "Script Folder" để chỉ định subfolder
3. _(Tùy chọn)_ Chọn Parent transform trong scene
4. Chọn loại component: `RawImage` hoặc `SpriteRenderer`
5. Nhấn **✨ Create Complete Field (Automated)**

Tool sẽ tự tạo script, chờ compile, rồi tạo GameObject và add component.

---

## Generate hàng loạt

**Tab "Main" → Quick Actions**

- Kéo thả folder vào "Script Folder" để chỉ generate types trong folder đó (bỏ trống = tất cả)
- Kéo thả Transform vào "Parent Transform" để giới hạn phạm vi tìm kiếm trong scene
- Nhấn **🔄 Generate All Fields** để tạo toàn bộ
- Nhấn **🔍 Preview & Generate** để xem trước trước khi tạo
- Nhấn **➕ Append Fields** để thêm mà không xóa list hiện tại

---

## Capture layout

Sau khi đã setup xong vị trí/scale của các element trong Game View:

**Cách 1 — Capture từng field:**
- Mở Inspector của bất kỳ `*_LunaField` component nào
- Chọn slot orientation cần capture
- Nhấn **Capture**

**Cách 2 — Capture tất cả cùng lúc:**
- Vào tab **Capture** trên `LunaFieldController`
- Chọn orientation slot
- Nhấn **Capture All Fields**

> Tool tự detect orientation hiện tại từ Game View size và OrientationTracker.

---

## Cấu trúc script

Các field script đặt trong:
```
Assets/_LunaFieldGenerator/Scripts/Samples/
├── GUI_Intro/      ← subfolder group
├── GUI_Outro/
└── ...
```

Subfolder trong `Samples/` sẽ tự động tạo hierarchy group tương ứng trong scene.

---

## Dummy textures

Đặt texture placeholder vào `Assets/_LunaFieldGenerator/Dummys/`
hoặc set "Default Dummy Texture" trong tab **Settings**.

---

## Lưu ý

- Sau khi generate, nhớ assign đúng texture thật vào từng field trước khi build
- Dùng **🧹 Merge Duplicate Parent Groups** (tab Main) nếu scene có group hierarchy bị trùng tên
- Mọi setting (path, texture mặc định...) có thể reset về default trong tab **Settings**
