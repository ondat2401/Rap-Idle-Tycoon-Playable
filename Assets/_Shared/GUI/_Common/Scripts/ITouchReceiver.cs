using UnityEngine;

namespace Amanotes.MagicTilesCore
{
    /// <summary>
    /// Contract for objects that want to receive touch/pointer events routed by
    /// <see cref="TouchDispatcher"/>. Receivers are hit-tested via
    /// <see cref="ContainsWorldPoint"/> and picked by <see cref="TouchPickPriority"/>
    /// (higher priority wins when multiple receivers overlap).
    /// </summary>
    public interface ITouchReceiver
    {
        /// <summary>
        /// Higher values are picked first when several receivers overlap the same point.
        /// </summary>
        int TouchPickPriority { get; }

        /// <summary>
        /// Returns true when the given world position falls inside this receiver.
        /// </summary>
        bool ContainsWorldPoint(Vector3 worldPos);

        /// <summary>
        /// Called when a touch/press begins. Return true to capture the finger so that
        /// subsequent move/end events are delivered to this receiver.
        /// </summary>
        bool HandleTouchBegin(int fingerId, Vector2 screenPos, Vector3 worldPos);

        /// <summary>
        /// Called while a captured finger moves.
        /// </summary>
        void HandleTouchMove(int fingerId, Vector2 screenPos, Vector3 worldPos);

        /// <summary>
        /// Called when a captured finger is released. <paramref name="cancelled"/> is true
        /// when the touch was lost rather than cleanly lifted.
        /// </summary>
        void HandleTouchEnd(int fingerId, Vector2 screenPos, Vector3 worldPos, bool cancelled);
    }
}
