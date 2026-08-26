
using System;
using System.Collections;
using UnityEngine;
using Utils;
using static Utils.Utils;

namespace Managers
{
    public class Routiner : MonoBehaviourSingleton<Routiner>
    {
        public static Coroutine Start(IEnumerator routine)
        {
            return Instance.StartCoroutine(routine);
        }

        public static void Stop(Coroutine routine)
        {
            Instance.StopCoroutine(routine);
        }

        public static Coroutine StartOnNextFrame(Action action)
        {
            return Instance.StartCoroutine(StartOnNextFrameCoroutine(action));
        }

        public static Coroutine StartAfterFixedUpdate(Action action)
        {
            return Instance.StartCoroutine(StartAfterFixedUpdateCoroutine(action));
        }
        
        public static Coroutine StartDelayed(Action action, float delay)
        {
            return Instance.StartCoroutine(StartDelayedCoroutine(action, delay));
        }


        private static IEnumerator StartOnNextFrameCoroutine(Action action)
        {
            yield return new WaitForEndOfFrame();
            action.Invoke();
        }

        private static IEnumerator StartAfterFixedUpdateCoroutine(Action action)
        {
            yield return new WaitForFixedUpdate();
            action.Invoke();
        }

        private static IEnumerator StartDelayedCoroutine(Action action, float delay)
        {
            IEnumerator wait = WaitForRealSeconds(delay);
            while (wait.MoveNext())
            {
                yield return wait.Current;
            }
            action.Invoke();
        }

        private static IEnumerator WaitForRealSeconds(float delay)
        {
            float start = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup <= start + delay + EPS)
            {
                yield return null;
            }
        }
        
        
        protected override void OnDestroy()
        {
            CancelInvoke();

            StopAllCoroutines();

            base.OnDestroy();
        }
    }
}