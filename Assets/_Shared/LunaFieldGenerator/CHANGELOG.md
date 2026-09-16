# Luna Field Generator — Changelog
Nếu tôi ghi update change log to new version thì mới update version mới, còn không thì vẫn giữ nguyên version cũ
---
## [v0.4] — 2026-03-26

### Changed
- **Tab Capture → Tab Rename**: thay thế tab 📸 Capture bằng tab ✏️ Rename
- `EditorTab.Capture` → `EditorTab.Rename`
- `LunaFieldCaptureEditor.cs` vẫn giữ (dead code), không còn được gọi từ tab bar
- **Attribute format mới: `[LunaPlaygroundSection]`**
  - Luna name giờ đặt 1 lần trên class: `[LunaPlaygroundSection("Name")]` thay vì lặp param thứ 3 ở mỗi field
  - `[LunaPlaygroundAsset("Texture", 0)]` và `[LunaPlaygroundField("...", N)]` chỉ còn 2 params
  - `GenerateClassContent` cập nhật template theo format mới
  - Backward compatible: scan & rename vẫn hỗ trợ legacy 3-param format

### Added
- **Tab Rename — Batch rename Luna name** (`LunaFieldRenameEditor.cs`)
  - Scan tất cả `.cs` files trong `customFieldPath`, parse source bằng regex
  - Ưu tiên `[LunaPlaygroundSection("Name")]`, fallback sang legacy 3-param format
  - Hiển thị danh sách scripts với Luna display name, badge format (Section/Legacy)
  - Link optional tới scene FieldBase instance (nút 📍 ping object)
  - **Rename Selected**: đổi Luna name trong 1 script
  - **Rename All Same Name**: batch rename tất cả scripts cùng Luna name
  - Hỗ trợ cả 2 format khi rename (Section + legacy 3rd param)
  - Không phụ thuộc reflection — parse source file trực tiếp, hoạt động với attribute class từ DLL
- **Main tab: hiển thị Luna name kế bên tên class** trong field list
  - `GetLunaNameForType()` + `ParseLunaNameFromSource()` parse từ source file
  - Cache `lunaNameCache` tránh đọc file lặp lại, clear khi rescan types hoặc rename
- **Create Field tab: toggles `hasPosition`, `hasScale`, `hasRotation`**
  - 3 toggle mới trong "Transform Properties" section, persist qua EditorPrefs
  - `GenerateClassContentFull()` tạo code động dựa trên tổ hợp toggle
  - Rotation fields dùng `float` (Z-axis euler angle) thay vì `Vector2`
  - `ApplyOrientation()` tự động build dựa trên properties được chọn
- **FieldBaseEditor: Property Groups management** (per-field inspector)
  - Auto-detect property groups qua reflection (hasPosition, hasScale, hasRotation, hasTexture)
  - Status badges hiển thị ✓/✗ cho từng group
  - Nút `+ Position/Scale/Rotation/Texture` thêm property group vào source file
  - Nút `- Position/Scale/Rotation/Texture` xóa property group khỏi source file (có confirm dialog)
  - Auto-rebuild `ApplyOrientation()` khi add/remove transform properties
  - `GetNextAvailableIndex()` tự tính index tiếp theo cho LunaPlayground attributes
- **FieldBaseEditor: Capture hỗ trợ Rotation**
  - Capture `localEulerAngles.z` vào rotation fields
  - Hiển thị stored rotation values trong slot preview
  - `SetSlotValues()` unified method thay thế `SetFieldValues`/`SetFieldValuesRaw`

### Fixed
- Đổi Event Unity → EventBus
## [v0.3] — 2026-03-14

### Added
- **Create Field tab: toggle "Has Texture"**
  - `createWithTexture` toggle — mặc định `true`
  - Nếu `false`: ẩn "Component Type" dropdown, ẩn nút "Create GameObject Only"
  - `GenerateClassContent(baseName, withTexture)` — khi `false` bỏ `tex` field, `[LunaPlaygroundAsset]`, `using UnityEngine.UI`, và `ApplyTexture()` override
  - `CreateFieldGameObjectNoTexture()` — tạo empty GO không có RawImage/SpriteRenderer
  - Persist `createWithTexture` qua `EditorPrefs`

- **Create Auto: auto-parent khi không set parent**
  - `ResolveCreateParentWithAutoCreate()` — tìm GO tên `baseName` trên scene, nếu không có thì tạo mới trong Canvas với `RectTransform`
  - Nếu chưa có Canvas trên scene thì tự tạo Canvas (ScreenSpaceOverlay + CanvasScaler + GraphicRaycaster)
  - Thay thế `ResolveCreateParent()` trong flow `CreateCompleteFieldAutomated`

- **Automation: persist `pendingWithTexture` qua domain reload**
  - `pendingWithTexture` static field + `LunaField_PendingWithTexture` EditorPrefs key
  - `CompleteAutomatedFieldCreation` nhận param `withTexture` — tạo GO và assign texture tương ứng



## [v0.2] — 2026-03-14

### Fixed
- **Settings tab: auto-save đường dẫn khi thay đổi**
  - `customFieldPath` và `dummyFolderPath` chỉ lưu khi `OnDisable()` → đã thêm `BeginChangeCheck` / `SaveSettings()` ngay khi edit

- **Settings tab: auto-load dummy texture từ folder nếu chưa set**
  - Khi `defaultDummyTexture` null, tự động load texture đầu tiên tìm thấy trong `dummyFolderPath`

- **Dummy texture đặt tên theo class**
  - Thêm `GetOrCreateDummyTextureForClass(className)`: tìm file `{ClassName}.png` trong dummy folder trước, nếu không có mới tạo mới
  - `GenerateAllFields` và `AppendFields` dùng logic này thay vì `dummy_1`, `dummy_2`

- **Drop zone field list luôn hiển thị**
  - Chuyển drop zone ra ngoài scroll view, luôn visible dù list có bao nhiêu field

- **Generate All: không tạo duplicate object**
  - `BuildGameObjectLookup` scan toàn scene trước, override bằng scoped match nếu có parent
  - `FindOrCreateGameObjectForType` thêm fallback `FindObjectOfType(type)` tìm theo component trước khi tạo mới

- **Generate All: không tạo duplicate parent group**
  - `ResolveSubFolderParent` search toàn scene trước bất kể `parentTransform` null hay không

- **Field list: tự động xóa entry null khi object bị xóa khỏi scene**
  - Mỗi lần render, filter null references ra khỏi `controller.fields` và dirty controller

---

## [v0.1] — Initial Release

### Features
- **4 Orientation slots** per `FieldBase`: Phone/Tablet × Portrait/Landscape
- **LunaFieldController** — giữ danh sách `FieldBase[]`, apply all khi orientation thay đổi
- **Editor Tool — 4 tab**:
  - Main: Generate All, Preview & Generate, Append Fields, drag-drop folder filter, field list với foldout/remove
  - Create Field: tạo `.cs` + GameObject + component + dummy texture tự động qua `CompilationPipeline`
  - Capture: capture transform hiện tại vào đúng slot orientation
  - Settings: config path scripts, path dummy textures, default texture, auto remove duplicates
- **FieldBaseEditor** — inline capture tool trong Inspector của từng `FieldBase`
- **LunaProgressWindow** — progress bar cho batch operations
- **Subfolder & Hierarchy Group** mapping 1:1 giữa `Samples/` và scene hierarchy
- **MergeDuplicateParentGroups** — gộp group trùng tên, reparent children
- **Persist settings** qua `EditorPrefs` với prefix `LunaFieldEditor_`
