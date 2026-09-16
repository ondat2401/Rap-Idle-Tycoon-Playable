using System;
using System.Collections.Generic;

namespace _Playable.Runtime.Core
{
    /// <summary>
    /// Event bus toi gian cho playable. Khong dung MessageBus cua _Framework de package doc lap.
    /// Khong phai singleton: <c>PlayableFlow</c> tao mot instance roi tiem xuong cac view.
    /// </summary>
    public sealed class PlayableEventBus
    {
        private readonly Dictionary<PlayableSignal, Action> _handlers = new Dictionary<PlayableSignal, Action>();

        public void Subscribe(PlayableSignal signal, Action handler)
        {
            if (handler == null || signal == PlayableSignal.None)
            {
                return;
            }

            if (this._handlers.TryGetValue(signal, out Action existing))
            {
                this._handlers[signal] = existing + handler;
            }
            else
            {
                this._handlers[signal] = handler;
            }
        }

        public void Unsubscribe(PlayableSignal signal, Action handler)
        {
            if (handler == null || !this._handlers.TryGetValue(signal, out Action existing))
            {
                return;
            }

            Action remaining = existing - handler;
            if (remaining == null)
            {
                this._handlers.Remove(signal);
            }
            else
            {
                this._handlers[signal] = remaining;
            }
        }

        public void Emit(PlayableSignal signal)
        {
            if (this._handlers.TryGetValue(signal, out Action handler))
            {
                handler?.Invoke();
            }
        }

        public void Clear()
        {
            this._handlers.Clear();
        }
    }
}
