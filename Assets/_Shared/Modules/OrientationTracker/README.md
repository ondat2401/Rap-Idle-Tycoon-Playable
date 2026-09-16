# OrientationTracker

Pure C# singleton for detecting device type (Phone/Tablet) and screen orientation (Portrait/Landscape) with dynamic change detection and zero per-frame overhead.

## Installation (UPM Git URL)

> ⚠️ **Phải install dependency trước!** UPM git packages không tự resolve dependencies.

**Bước 1 — Install dependency:**

Mở **Window → Package Manager → + → Add package from git URL**, nhập:

```
https://gitlab.amanotes.net/dat.ot/tool-builder.git#com.amanotes.playable-core
```

**Bước 2 — Install package này:**

```
https://gitlab.amanotes.net/dat.ot/tool-builder.git#com.amanotes.playable-orientation-tracker
```

---

## Overview

OrientationTracker is a pure C# sealed class (not a MonoBehaviour) that provides device type and orientation detection without Unity's built-in orientation APIs. It uses a manual singleton pattern with dependency injection support, enabling custom detection logic and testability. The system fires events when orientation or device type changes, allowing responsive UI and gameplay adjustments.

Key characteristics:
- Pure C# class with no MonoBehaviour dependency — can be initialized from any context
- Manual singleton with thread-safe initialization via `lock`
- Three initialization methods: standard, custom dependency injection, and reset for testing
- Event-driven architecture with `OnOrientationChanged` and `OnDeviceTypeChanged` callbacks
- O(1) property queries for all device and orientation checks
- Companion `OrientationTrackerInitializer` MonoBehaviour for automatic Unity integration
- Editor tools: debug window with device simulation and Project Settings integration

---

## Quick Start

**1. Add OrientationTrackerInitializer to scene**

Create a GameObject with the `OrientationTrackerInitializer` component. It will initialize the tracker on `Awake` and poll for orientation changes at a configurable interval.

**2. Configure in Inspector**

- **Screen Data Source:** Enable `Use Unity Screen API` (default) to use `Screen.width`, `Screen.height`, and `Screen.dpi` at runtime
- **Manual Configuration:** If `Use Unity Screen API` is disabled, set manual width, height, and DPI values
- **Runtime Orientation Detection:** Enable `Detect Orientation Change` (default) and set `Check Interval` (default: 0.5s)
- **Debug:** Enable logging for initialization and orientation changes

**3. Query orientation at runtime**

```csharp
using OrientationTracking.Core;

public class GameController : MonoBehaviour
{
    private void Start()
    {
        if (OrientationTracker.IsInitialized)
        {
            var tracker = OrientationTracker.Instance;
            
            if (tracker.IsTablet)
            {
                Debug.Log("Running on tablet");
            }
            
            if (tracker.IsLandscape)
            {
                Debug.Log("Landscape orientation");
            }
        }
    }
}
```

**4. Subscribe to orientation events**

```csharp
using OrientationTracking.Core;

public class UIController : MonoBehaviour
{
    private void OnEnable()
    {
        if (OrientationTracker.IsInitialized)
        {
            OrientationTracker.Instance.OnOrientationChanged += HandleOrientationChanged;
        }
    }

    private void OnDisable()
    {
        if (OrientationTracker.IsInitialized)
        {
            OrientationTracker.Instance.OnOrientationChanged -= HandleOrientationChanged;
        }
    }

    private void HandleOrientationChanged(Orientation oldOrientation, Orientation newOrientation)
    {
        Debug.Log($"Orientation changed: {oldOrientation} → {newOrientation}");
        // Adjust UI layout, camera FOV, etc.
    }
}
```

---

## Architecture

### OrientationTracker

Pure C# sealed class with manual singleton pattern. Namespace: `OrientationTracking.Core`.

**Initialization sequence:**
1. `Initialize()` or `InitializeWithCustom()` creates the singleton instance inside a `lock` block
2. Constructor validates dependencies (null checks without throwing)
3. `RefreshState()` is called immediately to detect initial device type and orientation
4. `_initializeTime` is set to `DateTime.UtcNow` for uptime tracking

**State refresh mechanism:**
- `RefreshState()` calls `_deviceDetector.DetectDevice()` to get current `DeviceInfo`
- `_orientationResolver.ResolveOrientation()` determines `Orientation` from `DeviceInfo`
- If orientation or device type changed, corresponding events are fired
- Events are only fired if the old value was not `Unknown` (skips initial detection)

**Thread safety:** The singleton instance is created inside a `lock (_lock)` block. Once initialized, all property reads are thread-safe (no writes after initialization except via `RefreshState()`).

### Enums

**Orientation** (namespace: `OrientationTracking.Core`)

```csharp
public enum Orientation
{
    Unknown = 0,
    Portrait = 1,
    Landscape = 2
}
```

**DeviceType** (namespace: `OrientationTracking.Core`)

```csharp
public enum DeviceType
{
    Unknown = 0,
    Phone = 1,
    Tablet = 2
}
```

### OrientationTrackerInitializer

MonoBehaviour wrapper for automatic initialization. Namespace: `OrientationTracking.Utils`.

**Awake initialization:**
- If `_useUnityScreenAPI` is true, creates a `DynamicScreenDataProvider` that reads `Screen.width`, `Screen.height`, and `Screen.dpi` at runtime
- If false, uses manual values (`_manualScreenWidth`, `_manualScreenHeight`, `_manualDPI`) with a static `ScreenDataProvider`
- Calls `OrientationTracker.InitializeWithCustom()` or `OrientationTracker.Initialize()` accordingly
- Subscribes to `OnOrientationChanged` and `OnDeviceTypeChanged` events

**Interval-based polling:**
- `Update()` checks if `Time.time >= _nextCheckTime` (default interval: 0.5s)
- Compares current `Screen.width` and `Screen.height` to determine expected orientation
- If expected orientation differs from `_lastOrientation`, calls `RefreshState()` and logs the change

**Event logging:**
- `HandleOrientationChanged()` logs orientation changes if `_logOnOrientationChange` is enabled
- `HandleDeviceTypeChanged()` logs device type changes (rare in practice)

---

## API Reference

### OrientationTracker — Static Initialization

```csharp
public static void Initialize(float screenWidth, float screenHeight, float dpi)
public static void InitializeWithCustom(IDeviceDetector deviceDetector, IOrientationResolver orientationResolver, IScreenDataProvider screenDataProvider)
public static void Reset()
```

| Method | Description |
|---|---|
| `Initialize(float screenWidth, float screenHeight, float dpi)` | Initialize with screen dimensions and DPI. Creates default implementations of `IDeviceDetector`, `IOrientationResolver`, and `IScreenDataProvider`. If already initialized, logs a message and returns |
| `InitializeWithCustom(IDeviceDetector deviceDetector, IOrientationResolver orientationResolver, IScreenDataProvider screenDataProvider)` | Initialize with custom dependency implementations for testing or custom detection logic. If already initialized, logs a message and returns |
| `Reset()` | Clears the singleton instance. Used for testing or re-initialization scenarios. Thread-safe via `lock` |

### OrientationTracker — Static Properties

```csharp
public static bool IsInitialized { get; }
public static OrientationTracker Instance { get; }
```

| Property | Description |
|---|---|
| `IsInitialized` | Returns `true` if `Instance` is not null. Check this before accessing `Instance` |
| `Instance` | Returns the singleton instance. Null if not initialized |

### OrientationTracker — Instance Properties

```csharp
public bool IsPhone { get; }
public bool IsTablet { get; }
public bool IsPortrait { get; }
public bool IsLandscape { get; }
public DeviceType CurrentDeviceType { get; }
public Orientation CurrentOrientation { get; }
public float AspectRatio { get; }
public float DiagonalInches { get; }
public DateTime InitializeTime { get; }
```

| Property | Type | Description |
|---|---|---|
| `IsPhone` | `bool` | Returns `true` if `CurrentDeviceType == DeviceType.Phone` |
| `IsTablet` | `bool` | Returns `true` if `CurrentDeviceType == DeviceType.Tablet` |
| `IsPortrait` | `bool` | Returns `true` if `CurrentOrientation == Orientation.Portrait` |
| `IsLandscape` | `bool` | Returns `true` if `CurrentOrientation == Orientation.Landscape` |
| `CurrentDeviceType` | `DeviceType` | Current device type (Phone, Tablet, or Unknown) |
| `CurrentOrientation` | `Orientation` | Current orientation (Portrait, Landscape, or Unknown) |
| `AspectRatio` | `float` | Screen aspect ratio (width / height) from `DeviceInfo` |
| `DiagonalInches` | `float` | Screen diagonal size in inches, calculated from width, height, and DPI |
| `InitializeTime` | `DateTime` | UTC timestamp when the tracker was initialized |

### OrientationTracker — Instance Methods

```csharp
public void RefreshState()
public DeviceInfo GetDeviceInfo()
public string GetDescription()
```

| Method | Description |
|---|---|
| `RefreshState()` | Manually refresh device type and orientation. Calls `_deviceDetector.DetectDevice()` and `_orientationResolver.ResolveOrientation()`. Fires events if values changed. Called automatically by `OrientationTrackerInitializer` when screen dimensions change |
| `GetDeviceInfo()` | Returns the current `DeviceInfo` struct containing `ScreenWidth`, `ScreenHeight`, `DPI`, `AspectRatio`, `DiagonalInches`, and `Type` |
| `GetDescription()` | Returns a formatted string with all device and screen info: `[DeviceType] Orientation | Screen: WxH | DPI: X | Size: Y" | Aspect: Z` |

### OrientationTracker — Events

```csharp
public event Action<Orientation, Orientation> OnOrientationChanged;
public event Action<DeviceType, DeviceType> OnDeviceTypeChanged;
```

| Event | Signature | Description |
|---|---|---|
| `OnOrientationChanged` | `Action<Orientation, Orientation>` | Fired when orientation changes. Parameters: (old orientation, new orientation). Not fired if old orientation is `Unknown` |
| `OnDeviceTypeChanged` | `Action<DeviceType, DeviceType>` | Fired when device type changes (rare). Parameters: (old device type, new device type). Not fired if old device type is `Unknown` |

---

## OrientationTrackerInitializer Configuration

### Inspector Fields

| Field | Type | Default | Description |
|---|---|---|---|
| **Use Unity Screen API** | `bool` | `true` | If enabled, uses `Screen.width`, `Screen.height`, and `Screen.dpi` at runtime. If disabled, uses manual configuration |
| **Manual Screen Width** | `float` | `1920` | Screen width in pixels (used if `Use Unity Screen API` is false) |
| **Manual Screen Height** | `float` | `1080` | Screen height in pixels (used if `Use Unity Screen API` is false) |
| **Manual DPI** | `float` | `326` | Screen DPI (used if `Use Unity Screen API` is false) |
| **Detect Orientation Change** | `bool` | `true` | If enabled, polls `Screen.width` and `Screen.height` every `Check Interval` seconds and calls `RefreshState()` when dimensions change |
| **Check Interval** | `float` | `0.5` | Seconds between orientation checks. Lower values = more responsive, higher CPU usage |
| **Log On Initialize** | `bool` | `true` | If enabled, logs tracker initialization details to Console |
| **Log On Orientation Change** | `bool` | `true` | If enabled, logs orientation and device type changes to Console |

---

## Editor Tools

### OrientationTrackerWindow

Debug and simulation window for testing orientation and device type in Editor. Open via `Window → Orientation Tracker Debug`.

**Status Tab:**
- Displays current device type, orientation, screen properties, and quick boolean checks
- Shows diagonal size, aspect ratio, DPI, and resolution
- Displays uptime since initialization

**Simulation Tab:**
- Custom device configuration: set width, height, and DPI manually
- Device presets: iPhone 13 Pro, Galaxy S21, iPad Pro 11", iPad Pro 12.9", and special cases (square, ultra-wide)
- Preview panel shows expected device type and orientation before simulation
- "Simulate with Custom Values" button initializes tracker with specified values

**Event History Tab:**
- Lists all orientation changes with timestamps
- Newest events appear first
- Clear History button to reset the log

**Footer:**
- "Log State" button prints current tracker state to Console
- "Refresh" button manually calls `RefreshState()`
- Status bar shows tracker state (Active, Initializing, or Editor Mode)

### OrientationTrackerSettingsProvider

Project Settings integration at `Project Settings → Orientation Tracker`.

**About Section:**
- Framework description and feature list
- SOLID design, dynamic detection, event-driven architecture, zero GC allocation

**Quick Start Section:**
- "Create Initializer GameObject" button creates a GameObject with `OrientationTrackerInitializer` component
- "Open Debug Window" button opens `OrientationTrackerWindow`
- Example usage code snippet

**Detection Thresholds Section:**
- Documents default Phone/Tablet threshold (6.5 inches)
- Recommended check intervals for different game types (0.2s for fast games, 0.5s for normal, 1.0s for turn-based)

**Links & Resources Section:**
- Buttons for documentation, README, and troubleshooting guide (placeholders)

---

## Code Examples

### 1. Initialize and Query Orientation

```csharp
using UnityEngine;
using OrientationTracking.Core;

public class GameManager : MonoBehaviour
{
    private void Awake()
    {
        // Manual initialization (if not using OrientationTrackerInitializer)
        if (!OrientationTracker.IsInitialized)
        {
            OrientationTracker.Initialize(Screen.width, Screen.height, Screen.dpi);
        }
    }

    private void Start()
    {
        var tracker = OrientationTracker.Instance;
        
        // Query device type
        if (tracker.IsTablet)
        {
            Debug.Log("Running on tablet — enable tablet-specific UI");
        }
        else if (tracker.IsPhone)
        {
            Debug.Log("Running on phone — use compact UI");
        }
        
        // Query orientation
        if (tracker.IsLandscape)
        {
            Debug.Log("Landscape mode — adjust camera FOV");
        }
        else if (tracker.IsPortrait)
        {
            Debug.Log("Portrait mode — use vertical layout");
        }
        
        // Get detailed info
        Debug.Log(tracker.GetDescription());
        // Output: [Phone] Portrait | Screen: 1080x1920 | DPI: 401.0 | Size: 5.46" | Aspect: 0.56
    }
}
```

### 2. Subscribe to Orientation Events

```csharp
using UnityEngine;
using OrientationTracking.Core;

public class ResponsiveUI : MonoBehaviour
{
    private void OnEnable()
    {
        if (OrientationTracker.IsInitialized)
        {
            var tracker = OrientationTracker.Instance;
            tracker.OnOrientationChanged += HandleOrientationChanged;
            tracker.OnDeviceTypeChanged += HandleDeviceTypeChanged;
        }
    }

    private void OnDisable()
    {
        if (OrientationTracker.IsInitialized)
        {
            var tracker = OrientationTracker.Instance;
            tracker.OnOrientationChanged -= HandleOrientationChanged;
            tracker.OnDeviceTypeChanged -= HandleDeviceTypeChanged;
        }
    }

    private void HandleOrientationChanged(Orientation oldOrientation, Orientation newOrientation)
    {
        Debug.Log($"Orientation changed: {oldOrientation} → {newOrientation}");
        
        if (newOrientation == Orientation.Landscape)
        {
            // Switch to landscape UI layout
            SetUILayout(UILayout.Landscape);
        }
        else if (newOrientation == Orientation.Portrait)
        {
            // Switch to portrait UI layout
            SetUILayout(UILayout.Portrait);
        }
    }

    private void HandleDeviceTypeChanged(DeviceType oldType, DeviceType newType)
    {
        Debug.Log($"Device type changed: {oldType} → {newType}");
        // Rare in practice, but useful for testing or edge cases
    }

    private void SetUILayout(UILayout layout)
    {
        // Adjust UI anchors, positions, scales, etc.
    }

    private enum UILayout { Portrait, Landscape }
}
```

### 3. Custom Device Detection Logic

```csharp
using OrientationTracking.Core;
using OrientationTracking.Interfaces;

public class CustomDeviceDetector : IDeviceDetector
{
    private readonly IScreenDataProvider _screenDataProvider;
    private const float TABLET_THRESHOLD = 7.0f; // Custom threshold: 7 inches

    public CustomDeviceDetector(IScreenDataProvider screenDataProvider)
    {
        _screenDataProvider = screenDataProvider;
    }

    public DeviceInfo DetectDevice()
    {
        float width = _screenDataProvider.GetScreenWidth();
        float height = _screenDataProvider.GetScreenHeight();
        float dpi = _screenDataProvider.GetDPI();

        float widthInches = width / dpi;
        float heightInches = height / dpi;
        float diagonal = Mathf.Sqrt(widthInches * widthInches + heightInches * heightInches);

        DeviceType type = diagonal >= TABLET_THRESHOLD ? DeviceType.Tablet : DeviceType.Phone;

        return new DeviceInfo
        {
            ScreenWidth = width,
            ScreenHeight = height,
            DPI = dpi,
            AspectRatio = width / height,
            DiagonalInches = diagonal,
            Type = type
        };
    }
}

// Usage:
public class GameInitializer : MonoBehaviour
{
    private void Awake()
    {
        var screenProvider = new OrientationTracking.Implementations.DynamicScreenDataProvider();
        var customDetector = new CustomDeviceDetector(screenProvider);
        var orientationResolver = new OrientationTracking.Implementations.OrientationResolver();

        OrientationTracker.InitializeWithCustom(customDetector, orientationResolver, screenProvider);
    }
}
```

---

## Performance

### Single-Frame Initialization

OrientationTracker initializes in a single frame with no multi-frame overhead. The constructor calls `RefreshState()` once, which performs:
- One `DetectDevice()` call (calculates diagonal size via `Mathf.Sqrt`)
- One `ResolveOrientation()` call (simple width/height comparison)
- No asset loading, no coroutines, no deferred initialization

Total cost: ~0.01ms on modern hardware.

### No Update() Loop

The core `OrientationTracker` class has no `Update()` method and no per-frame overhead. Orientation changes are detected via:
- `OrientationTrackerInitializer.Update()` polling at configurable intervals (default: 0.5s)
- Manual `RefreshState()` calls from gameplay code

This keeps CPU usage minimal — orientation checks happen only when needed, not every frame.

### O(1) Property Queries

All properties (`IsPhone`, `IsTablet`, `IsLandscape`, `IsPortrait`, `CurrentOrientation`, `CurrentDeviceType`, `AspectRatio`, `DiagonalInches`) are simple field reads with no computation. Each query is O(1) with zero allocation.

### Zero GC Allocation

After initialization, the tracker produces zero GC allocations:
- All properties return primitive types or cached structs
- Events use `Action<T, T>` delegates (no boxing)
- `GetDescription()` allocates a string, but is intended for debug logging only

Typical usage (property queries + event subscriptions) generates 0 bytes of garbage per frame.

### Polling Overhead

`OrientationTrackerInitializer` polls `Screen.width` and `Screen.height` every `_checkInterval` seconds (default: 0.5s). Each poll:
- Reads two `int` properties from Unity's native layer (~0.001ms)
- Compares to cached values (one `!=` check)
- Calls `RefreshState()` only if dimensions changed

For a 0.5s interval, this adds ~0.002ms per second of CPU time — negligible even on low-end mobile devices.

**Recommended intervals:**
- Fast-paced games (racing, FPS): 0.2s
- Normal games (platformers, RPGs): 0.5s (default)
- Turn-based games (card games, puzzles): 1.0s
