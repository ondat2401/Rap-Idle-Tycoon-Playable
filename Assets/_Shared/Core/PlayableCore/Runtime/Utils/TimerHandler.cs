using System;
using System.Collections;
using UnityEngine;

namespace Amanotes.Core
{
    public class TimerHandle
    {
        internal Coroutine coroutine;
    }

    public static class Timer
    {
        private static TimerRunner runner;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            runner = null;
        }

        public static TimerHandle CountDown(float delay, Action onComplete)
        {
            EnsureRunner();

            var handle = new TimerHandle();
            handle.coroutine = runner.StartCoroutine(runner.Run(delay, onComplete));
            return handle;
        }

        public static void Cancel(TimerHandle handle)
        {
            if (handle?.coroutine != null && runner != null)
            {
                runner.StopCoroutine(handle.coroutine);
            }
        }

        private static void EnsureRunner()
        {
            if (runner != null) return;

            var go = new GameObject("[Timer]");
            UnityEngine.Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<TimerRunner>();
        }

        private class TimerRunner : MonoBehaviour
        {
            public IEnumerator Run(float delay, Action callback)
            {
                yield return new WaitForSeconds(delay);
                callback?.Invoke();
            }
        }
    }
}
