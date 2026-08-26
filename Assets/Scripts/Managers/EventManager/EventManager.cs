
using System;
using System.Collections.Generic;
using Utils;

namespace Managers.EventManager
{
    public class EventManager: Singleton<EventManager>
    {
        private readonly Dictionary<EGameEvents, HashSet<Action<object>>> _subscriptions;

        private EventManager()
        {
            _subscriptions = new Dictionary<EGameEvents, HashSet<Action<object>>>();
        }
        
        public static void AddEventListener(EGameEvents gameEvent, Action<object> callback)
        {
            Instance._subscriptions.GetOrCreate(gameEvent).Add(callback);
        }

        public static void RemoveAllSubscriptions()
        {
            foreach (var callbacks in Instance._subscriptions)
            {
                callbacks.Value.Clear();
            }
        }

        public static void RemoveEventSubscription(EGameEvents gameEvent, Action<object> callback)
        {
            if (!Instance._subscriptions.TryGetValue(gameEvent, out HashSet<Action<object>> callbacks))
            {
                return;
            }
            
            callbacks.Remove(callback);
        }

        public static void RemoveEventListener(EGameEvents gameEvent, Action<object> callback)
        {
            if (!Instance._subscriptions.TryGetValue(gameEvent, out HashSet<Action<object>> callbacks))
            {
                return;
            }

            callbacks.Remove(callback);
        }

        public static void CallEvent(EGameEvents gameEvent, object obj = null)
        {
            if (!Instance._subscriptions.TryGetValue(gameEvent, out HashSet<Action<object>> callbacks))
            {
                return;
            }
            
            List<Action<object>> copy = new(callbacks);
            foreach (var callback in copy)
            {
                if (callbacks.Contains(callback))    //prevent calling listener that already deleted while iterating
                {
                    callback(obj);
                }
            }
        }
    }
}
