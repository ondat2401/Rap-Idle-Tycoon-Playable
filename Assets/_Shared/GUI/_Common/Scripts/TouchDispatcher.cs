using System.Collections.Generic;
using UnityEngine;

namespace Amanotes.MagicTilesCore
{
    /// <summary>
    /// Central input router that dispatches mouse/touch events to registered
    /// <see cref="ITouchReceiver"/> instances. A single dispatcher lives in the scene;
    /// it is created on demand the first time <see cref="Instance"/> is accessed.
    ///
    /// Uses the legacy <see cref="Input"/> API so it stays compatible with the Luna
    /// Playworks transpiler (no multi-touch-specific or unsupported APIs).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class TouchDispatcher : MonoBehaviour
    {
        private static TouchDispatcher s_instance;

        /// <summary>
        /// Returns the current dispatcher without creating one. May be null during shutdown.
        /// </summary>
        public static TouchDispatcher InstanceIfExists => s_instance;

        /// <summary>
        /// Returns the current dispatcher, creating a hidden GameObject to host it if needed.
        /// </summary>
        public static TouchDispatcher Instance
        {
            get
            {
                if (s_instance != null) return s_instance;

                s_instance = FindObjectOfType<TouchDispatcher>();
                if (s_instance == null)
                {
                    var go = new GameObject(nameof(TouchDispatcher));
                    s_instance = go.AddComponent<TouchDispatcher>();
                }

                return s_instance;
            }
        }

        [Tooltip("Camera used to convert world points to screen space. Defaults to Camera.main when null.")]
        [SerializeField] private Camera raycastCamera;

        /// <summary>
        /// Camera used to project world points to screen coordinates. Falls back to Camera.main.
        /// </summary>
        public Camera RaycastCamera
        {
            get
            {
                if (raycastCamera == null) raycastCamera = Camera.main;
                return raycastCamera;
            }
            set => raycastCamera = value;
        }

        private readonly List<ITouchReceiver> _receivers = new List<ITouchReceiver>();

        // Tracks which receiver captured a given fingerId. Key -1 is reserved for the mouse.
        private readonly Dictionary<int, ITouchReceiver> _captures = new Dictionary<int, ITouchReceiver>();

        private const int MouseFingerId = -1;

        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            s_instance = this;
        }

        private void OnDestroy()
        {
            if (s_instance == this) s_instance = null;
        }

        public void Register(ITouchReceiver receiver)
        {
            if (receiver == null || _receivers.Contains(receiver)) return;
            _receivers.Add(receiver);
        }

        public void Unregister(ITouchReceiver receiver)
        {
            if (receiver == null) return;
            _receivers.Remove(receiver);

            // Drop any captures held by this receiver so we don't dispatch to a dead object.
            var staleKeys = new List<int>();
            foreach (var kvp in _captures)
                if (kvp.Value == receiver)
                    staleKeys.Add(kvp.Key);

            for (int i = 0; i < staleKeys.Count; i++)
                _captures.Remove(staleKeys[i]);
        }

        private void Update()
        {
            if (Input.touchSupported && Input.touchCount > 0)
                ProcessTouches();
            else
                ProcessMouse();
        }

        private void ProcessTouches()
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                Vector2 screenPos = touch.position;
                Vector3 worldPos = ScreenToWorld(screenPos);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        Begin(touch.fingerId, screenPos, worldPos);
                        break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        Move(touch.fingerId, screenPos, worldPos);
                        break;
                    case TouchPhase.Ended:
                        End(touch.fingerId, screenPos, worldPos, cancelled: false);
                        break;
                    case TouchPhase.Canceled:
                        End(touch.fingerId, screenPos, worldPos, cancelled: true);
                        break;
                }
            }
        }

        private void ProcessMouse()
        {
            Vector2 screenPos = Input.mousePosition;
            Vector3 worldPos = ScreenToWorld(screenPos);

            if (Input.GetMouseButtonDown(0))
                Begin(MouseFingerId, screenPos, worldPos);
            else if (Input.GetMouseButton(0))
                Move(MouseFingerId, screenPos, worldPos);
            else if (Input.GetMouseButtonUp(0))
                End(MouseFingerId, screenPos, worldPos, cancelled: false);
        }

        private void Begin(int fingerId, Vector2 screenPos, Vector3 worldPos)
        {
            ITouchReceiver picked = Pick(worldPos);
            if (picked == null) return;

            if (picked.HandleTouchBegin(fingerId, screenPos, worldPos))
                _captures[fingerId] = picked;
        }

        private void Move(int fingerId, Vector2 screenPos, Vector3 worldPos)
        {
            if (_captures.TryGetValue(fingerId, out ITouchReceiver receiver) && receiver != null)
                receiver.HandleTouchMove(fingerId, screenPos, worldPos);
        }

        private void End(int fingerId, Vector2 screenPos, Vector3 worldPos, bool cancelled)
        {
            if (_captures.TryGetValue(fingerId, out ITouchReceiver receiver))
            {
                _captures.Remove(fingerId);
                if (receiver != null)
                    receiver.HandleTouchEnd(fingerId, screenPos, worldPos, cancelled);
            }
        }

        /// <summary>
        /// Picks the highest-priority receiver whose area contains the world point.
        /// </summary>
        private ITouchReceiver Pick(Vector3 worldPos)
        {
            ITouchReceiver best = null;
            int bestPriority = int.MinValue;

            for (int i = 0; i < _receivers.Count; i++)
            {
                ITouchReceiver receiver = _receivers[i];
                if (receiver == null) continue;
                if (!receiver.ContainsWorldPoint(worldPos)) continue;

                if (receiver.TouchPickPriority > bestPriority)
                {
                    bestPriority = receiver.TouchPickPriority;
                    best = receiver;
                }
            }

            return best;
        }

        private Vector3 ScreenToWorld(Vector2 screenPos)
        {
            Camera cam = RaycastCamera;
            if (cam == null) return screenPos;

            float depth = cam.orthographic ? 0f : Mathf.Abs(cam.transform.position.z);
            return cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
        }
    }
}
