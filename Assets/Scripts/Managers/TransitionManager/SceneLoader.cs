using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;
using ViewSystem;

namespace Managers
{
    public static class SceneLoader
    {
        private static readonly ITransitionEffect Effect = new EmptyTransitionEffect();

        public static void Load(string sceneName, Action onCovered = null, Action onLoaded = null)
        {
            TransitionRunner runner = TransitionRunner.Instance;
            if (runner.IsBusy)
            {
                LoadCovered(sceneName, onLoaded);
                return;
            }

            Routiner.Start(LoadRoutine(runner, sceneName, onCovered, onLoaded));
        }

        private static IEnumerator LoadRoutine(TransitionRunner runner, string sceneName, Action onCovered, Action onLoaded)
        {
            yield return runner.Play(Effect, TransitionWork(runner, sceneName, onCovered, onLoaded));
        }

        private static IEnumerator TransitionWork(TransitionRunner runner, string sceneName, Action onCovered, Action onLoaded)
        {
            onCovered.InvokeSafe();

            bool sceneLoaded = false;
            LoadCovered(sceneName, () =>
            {
                onLoaded.InvokeSafe();
                sceneLoaded = true;
            });

            yield return new WaitUntil(() => sceneLoaded);
            
            runner.BringToFront();
            
            yield return ViewManager.Instance.WaitForLocalization();
        }

        private static void LoadCovered(string sceneName, Action onLoaded)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.LoadScene(sceneName);
            return;

            void OnSceneLoaded(Scene scene, LoadSceneMode mode)
            {
                if (scene.name != sceneName)
                {
                    return;
                }

                SceneManager.sceneLoaded -= OnSceneLoaded;
                onLoaded.InvokeSafe();
            }
        }

        private sealed class EmptyTransitionEffect : ITransitionEffect
        {
            public IEnumerator Cover(TransitionOverlay overlay)
            {
                yield return null;
            }

            public IEnumerator Reveal(TransitionOverlay overlay)
            {
                yield return null;
            }
        }
    }
}
