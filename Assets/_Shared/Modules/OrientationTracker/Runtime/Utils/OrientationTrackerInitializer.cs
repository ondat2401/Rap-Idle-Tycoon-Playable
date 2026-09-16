using UnityEngine;
using OrientationTracking.Core;

namespace OrientationTracking.Utils
{
    /// <summary>
    /// MonoBehaviour that initializes OrientationTracker and polls for orientation changes.
    /// </summary>
    public class OrientationTrackerInitializer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _checkInterval = 0.5f;
        [SerializeField] private bool _logChanges = false;

        private Orientation _lastOrientation;
        private float _nextCheckTime;

        private void Awake()
        {
            OrientationTracker.Initialize();
            _lastOrientation = OrientationTracker.Instance.CurrentOrientation;

            if (_logChanges)
            {
                Debug.Log("[OrientationTracker] Initialized: " + OrientationTracker.Instance.GetDescription());
            }
        }

        private void Update()
        {
            if (Time.time < _nextCheckTime) return;
            _nextCheckTime = Time.time + _checkInterval;

            Orientation current = Screen.width > Screen.height
                ? Orientation.Landscape
                : Orientation.Portrait;

            if (current != _lastOrientation)
            {
                OrientationTracker.Instance.RefreshState();
                _lastOrientation = OrientationTracker.Instance.CurrentOrientation;

                if (_logChanges)
                {
                    Debug.Log("[OrientationTracker] Changed: " + OrientationTracker.Instance.GetDescription());
                }
            }
        }
    }
}
