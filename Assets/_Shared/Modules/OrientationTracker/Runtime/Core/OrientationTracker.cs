using UnityEngine;
using Amanotes.Core;

namespace OrientationTracking.Core
{
    /// <summary>
    /// Lightweight orientation and device type tracker.
    /// </summary>
    public sealed class OrientationTracker
    {
        public static OrientationTracker Instance;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            Instance = null;
        }

        // Tablet threshold: aspect ratio < 1.6 = tablet
        private const float TABLET_ASPECT_THRESHOLD = 1.6f;

        public Orientation CurrentOrientation { get; private set; }
        public bool IsPhone { get; private set; }
        public bool IsTablet { get; private set; }
        public bool IsLandscape { get { return CurrentOrientation == Orientation.Landscape; } }
        public bool IsPortrait { get { return CurrentOrientation == Orientation.Portrait; } }

        public static bool IsInitialized
        {
            get { return Instance != null; }
        }

        private OrientationTracker()
        {
            RefreshState();
        }

        public static void Initialize()
        {
            if (Instance != null) return;
            Instance = new OrientationTracker();
        }

        /// <summary>
        /// Legacy overload — parameters ignored, uses Screen API directly.
        /// </summary>
        public static void Initialize(float screenWidth, float screenHeight, float dpi)
        {
            Initialize();
        }

        /// <summary>
        /// Legacy overload for custom providers — ignored, uses Screen API directly.
        /// </summary>
        public static void InitializeWithCustom(object deviceDetector, object orientationResolver, object screenDataProvider)
        {
            Initialize();
        }

        public void RefreshState()
        {
            float w = Screen.width;
            float h = Screen.height;

            Orientation oldOrientation = CurrentOrientation;

            // Orientation
            CurrentOrientation = w > h ? Orientation.Landscape : Orientation.Portrait;

            // Device type based on aspect ratio
            float larger = w > h ? w : h;
            float smaller = w < h ? w : h;
            float aspect = smaller > 0 ? larger / smaller : 1f;

            IsTablet = aspect < TABLET_ASPECT_THRESHOLD;
            IsPhone = !IsTablet;

            // Publish event if orientation changed
            if (oldOrientation != Orientation.Unknown && oldOrientation != CurrentOrientation)
            {
                EventBus.Publish(new OnOrientationChangedEvent
                {
                    OldOrientation = oldOrientation,
                    NewOrientation = CurrentOrientation
                });
            }
        }

        public DeviceType CurrentDeviceType
        {
            get { return IsTablet ? DeviceType.Tablet : DeviceType.Phone; }
        }

        public static void Reset()
        {
            Instance = null;
        }

        public string GetDescription()
        {
            return string.Format("[{0}] {1} | Screen: {2}x{3}",
                IsTablet ? "Tablet" : "Phone",
                CurrentOrientation,
                Screen.width,
                Screen.height);
        }

        /// <summary>
        /// Returns a simple DeviceInfo struct for Editor display purposes.
        /// </summary>
        public DeviceInfo GetDeviceInfo()
        {
            return new DeviceInfo(Screen.width, Screen.height, Screen.dpi);
        }

        public System.DateTime InitializeTime { get { return _initializeTime; } }
        private readonly System.DateTime _initializeTime = System.DateTime.UtcNow;
    }

    public struct DeviceInfo
    {
        public float ScreenWidth;
        public float ScreenHeight;
        public float DPI;
        public float AspectRatio;
        public float DiagonalInches;

        public DeviceInfo(float width, float height, float dpi)
        {
            ScreenWidth = width;
            ScreenHeight = height;
            DPI = dpi > 0 ? dpi : 96f;
            float larger = width > height ? width : height;
            float smaller = width < height ? width : height;
            AspectRatio = smaller > 0 ? larger / smaller : 1f;
            float wInch = width / DPI;
            float hInch = height / DPI;
            DiagonalInches = (float)System.Math.Sqrt(wInch * wInch + hInch * hInch);
        }
    }

    // Events
    public struct OnOrientationChangedEvent
    {
        public Orientation OldOrientation;
        public Orientation NewOrientation;
    }

    public struct OnDeviceTypeChangedEvent
    {
        public DeviceType OldDeviceType;
        public DeviceType NewDeviceType;
    }

    // Enums
    public enum Orientation
    {
        Unknown = 0,
        Portrait = 1,
        Landscape = 2
    }

    public enum DeviceType
    {
        Unknown = 0,
        Phone = 1,
        Tablet = 2
    }
}
