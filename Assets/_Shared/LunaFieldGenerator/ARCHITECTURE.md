# Luna Field Generator — Package Architecture

## Overview

Tool tạo và quản lý responsive layout cho Luna playable ads trong Unity Editor.  
Mỗi UI element được đại diện bởi một `FieldBase` subclass chứa dữ liệu position/scale/rotation cho 4 slot orientation (Phone/Tablet × Portrait/Landscape).

---

## Folder Structure

```
com.amanotes.playable-luna-field-generator/
├── package.json
├── README.md
├── CHANGELOG.md
├── ARCHITECTURE.md              ← file này
│
├── Runtime/
│   ├── Amanotes.LunaFieldGenerator.Runtime.asmdef
│   ├── Cores/
│   │   ├── FieldBase.cs                 — Base class cho tất cả Luna fields
│   │   └── LunaFieldController.cs      — Manager quản lý array FieldBase
│   └── Samples/
│       └── GUI_Outro/
│           └── SampleOutro_LunaField.cs — Ví dụ concrete field
│
├── Editor/
│   ├── Amanotes.LunaFieldGenerator.Editor.asmdef
│   │
│   ├── LunaFieldControllerEditor.cs           — Main coordinator (lifecycle, tabs, shared helpers)
│   ├── LunaFieldControllerEditor.MainTab.cs   — Tab Main: Status, Actions, Field List
│   ├── LunaFieldControllerEditor.Generation.cs — Generate/Append/Preview/Validate/Merge
│   ├── LunaFieldControllerEditor.Automation.cs — Post-compilation automation pipeline
│   ├── LunaFieldControllerEditor.CreateTab.cs  — Tab Create: tạo field mới
│   ├── LunaFieldControllerEditor.SettingsTab.cs — Tab Settings: paths, options
│   ├── LunaFieldCaptureEditor.cs              — Tab Capture: capture transform → slots
│   ├── LunaFieldRenameEditor.cs               — Tab Rename: batch rename Luna names
│   ├── FieldBaseEditor.cs                     — Per-field inspector (capture + property groups)
│   ├── LunaFieldFinderWindow.cs               — EditorWindow: tìm field chưa register
│   ├── LunaProgressWindow.cs                  — EditorWindow: progress bar
│   │
│   └── Services/
│       ├── OrientationHelper.cs       — Shared orientation detection & field name mapping
│       ├── FieldTypeScanner.cs        — Scan assemblies/files cho FieldBase types
│       ├── DummyTextureManager.cs     — CRUD dummy placeholder textures
│       ├── ScriptCodeGenerator.cs     — Generate .cs source code cho new fields
│       ├── SubfolderResolver.cs       — Canvas/subfolder/parent transform resolution
│       └── EditorSettingsStore.cs     — Centralized EditorPrefs persistence
│
└── Dummys/                            — (generated at runtime) placeholder textures
```

---

## Runtime Layer

### FieldBase

```
MonoBehaviour
├── targetTransform          → this.transform
├── ApplyOrientation()       → virtual, set position/scale/rotation từ stored data
├── ApplyTexture()           → virtual, assign texture to visual component
│
├── Metadata (virtual properties)
│   ├── HasPosition
│   ├── HasScale
│   ├── HasRotation
│   └── HasTexture
│
└── Capture API
    ├── CaptureSlot(isPhone, isPortrait)
    ├── CaptureSlotFrom(source, isPhone, isPortrait)
    ├── CaptureAllSlots()
    ├── GetStoredPosition(isPhone, isPortrait)
    ├── GetStoredScale(isPhone, isPortrait)
    └── GetStoredRotation(isPhone, isPortrait)
```

**Subclass pattern** (ví dụ `Logo_LunaField`):
- Khai báo fields: `phone_Position_PT`, `phone_Scale_LS`, `tablet_Position_PT`...
- Override `ApplyOrientation()` để đọc đúng slot theo device/orientation
- Override metadata: `HasPosition => true`, `HasScale => true`
- Override `CaptureSlotFrom()` để ghi transform values vào đúng field

### LunaFieldController

```
MonoBehaviour
├── fields: FieldBase[]      → serialized array
├── ApplyAllFields()         → gọi ApplyOrientation + ApplyTexture cho tất cả
│
├── Collection API
│   ├── AddField(field)
│   ├── RemoveField(field)
│   ├── RemoveFieldAt(index)
│   ├── RemoveNullFields()
│   ├── ContainsField(field)
│   ├── ContainsFieldOfType(type)
│   ├── GetFieldByType(type)
│   ├── GetField<T>()
│   └── FieldCount
│
└── Events
    └── OnOrientationChangedEvent → ApplyAllFields()
```

---

## Editor Layer

### Partial Class Structure

`LunaFieldControllerEditor` được chia thành 7 partial files:

| File | Responsibility |
|------|---------------|
| `.cs` (main) | OnEnable/OnDisable, tab routing, settings load/save, shared UI helpers |
| `.MainTab.cs` | Status overview, Quick Actions buttons, Field List with drag-drop |
| `.Generation.cs` | GenerateAll, Append, Preview, ValidateDummies, MergeDuplicates, GO helpers |
| `.Automation.cs` | Pending state machine: create script → wait compile → add component |
| `.CreateTab.cs` | Create Field UI, CreateFieldBaseClass, CreateCompleteFieldAutomated |
| `.SettingsTab.cs` | Paths, default texture, generation options, reset |
| `CaptureEditor.cs` | Capture tab: orientation status, capture all/per-field |
| `RenameEditor.cs` | Rename tab: scan Luna attributes, batch rename |

### Services

| Service | Mô tả |
|---------|--------|
| `OrientationHelper` | Static. Detect phone/tablet, portrait/landscape. Map field names (`phone_Position_PT`...). Shared giữa FieldBaseEditor và CaptureEditor |
| `FieldTypeScanner` | Static. Scan assemblies + file system cho FieldBase subclasses. Filter by folder |
| `DummyTextureManager` | Instance. Create/find/validate dummy textures per class name. Manage default texture |
| `ScriptCodeGenerator` | Static. Generate .cs source cho standard field và background field classes |
| `SubfolderResolver` | Static. Find/create parent groups, ensure Canvas hierarchy, resolve subfolder names |
| `EditorSettingsStore` | Instance. Wrap tất cả EditorPrefs keys vào typed properties |

### FieldBaseEditor (standalone)

Custom inspector cho mọi FieldBase subclass:
- Capture tool (single field)
- Property group management (add/remove Position/Scale/Rotation/Texture bằng cách modify .cs source)

### Utility Windows

- `LunaFieldFinderWindow` — Scan scene cho FieldBase chưa register, batch add
- `LunaProgressWindow` — Styled progress bar cho generation operations

---

## Data Flow

```
[Create Field]
    User nhập tên → ScriptCodeGenerator tạo .cs
    → Unity compile → Automation detect type mới
    → Tạo GameObject + Component → AddField vào Controller

[Generate All]
    FieldTypeScanner scan types → Filter by folder
    → Per-frame loop (delayCall) → FindOrCreate GO → Add component
    → DummyTextureManager assign texture → Update controller.fields

[Capture]
    OrientationHelper detect slot → Reflection set field values
    (hoặc FieldBase.CaptureSlot nếu override)

[Apply (Runtime)]
    LunaFieldController.Start() hoặc OnOrientationChanged
    → Mỗi field.ApplyOrientation() + field.ApplyTexture()
```

---

## Dependencies

```
com.amanotes.playable-luna-field-generator
├── com.amanotes.playable-core              (EventBus, SDebug)
├── com.amanotes.playable-orientation-tracker (OrientationTracker, events)
└── Luna Playground SDK                      (LunaPlaygroundField, LunaPlaygroundAsset attributes)
```

---

## Conventions

- Field class naming: `{BaseName}_LunaField` (ví dụ: `Logo_LunaField`)
- Field data naming: `{device}_{Property}_{Orientation}` (ví dụ: `phone_Position_PT`)
- Devices: `phone`, `tablet`
- Orientations: `PT` (Portrait), `LS` (Landscape)
- Dummy textures: `Assets/_LunaFieldGenerator/Dummys/{ClassName}.png`
- Field scripts: `Assets/_LunaFieldGenerator/Scripts/Samples/{SubFolder}/{ClassName}.cs`
