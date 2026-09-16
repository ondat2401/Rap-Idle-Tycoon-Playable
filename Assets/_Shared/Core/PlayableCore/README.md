# Amanotes Core Framework

Core framework package chứa các utilities và managers dùng chung cho tất cả project.

## Installation (UPM Git URL)

Mở **Window → Package Manager → + → Add package from git URL** và nhập:

```
https://gitlab.amanotes.net/dat.ot/tool-builder.git#com.amanotes.playable-core
```
Hoặc thêm vào manifest.json
    "com.amanotes.playable-core": "https://gitlab.amanotes.net/dat.ot/tool-builder.git#repo/com.amanotes.playable-core",

> Package này không có dependency ngoài — install độc lập được.

## Modules

### Utils
- `SingletonMono<T>` / `SingletonMonoDontDestroy<T>` — Singleton pattern
- `EventBus` — Pub/Sub event system
- `SDebug` — Conditional debug logging (Editor only)
- `MegaExtension` — DOTween extension methods
- `Timer` — Coroutine-based timer
- `ButtonScale` — UI button press animation
- `CameraShake` — Camera shake effect
- `ImageHelper` — Aspect fit/fill helpers

### Managers
- `CoreManager` — ICore lifecycle management
- `AudioManager` — BGM/SFX with object pooling
- `GameConfigManager` — ScriptableObject config system
- `ObjectPoolManager` — Generic object pooling
- `GUIManager` — UI screen management with history

## Dependencies
- DOTween (DLL in Plugins/)
- TextMesh Pro
