using System;
using System.Collections.Generic;

namespace Amanotes.Core
{
    public static class EventBus
    {
        // key = type of event, value = delegate
        private static readonly Dictionary<Type, Delegate> _subscribers = new Dictionary<Type, Delegate>();

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            _subscribers.Clear();
        }

        // Subscribe
        public static void Subscribe<T>(Action<T> callback)
        {
            Delegate del;
            if (_subscribers.TryGetValue(typeof(T), out del))
                _subscribers[typeof(T)] = (Action<T>)del + callback;
            else
                _subscribers[typeof(T)] = callback;
        }

        // Unsubscribe
        public static void Unsubscribe<T>(Action<T> callback)
        {
            Delegate del;
            if (_subscribers.TryGetValue(typeof(T), out del))
            {
                var newDel = (Action<T>)del - callback;
                if (newDel == null)
                    _subscribers.Remove(typeof(T));
                else
                    _subscribers[typeof(T)] = newDel;
            }
        }

        // Publish
        public static void Publish<T>(T evt)
        {
            Delegate del;
            if (_subscribers.TryGetValue(typeof(T), out del))
                ((Action<T>)del).Invoke(evt);
        }
    }
}
