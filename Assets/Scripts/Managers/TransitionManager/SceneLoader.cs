
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

namespace Managers
{
    public class SceneLoader
    {
        private static ITransitionEffect _effect;

        private readonly TransitionRunner _runner;
        private readonly ILocalization _localization;

        public SceneLoader(ILocalization localization, TransitionRunner runner)
        {
            _runner = runner;
            _localization = localization;
        }

        public void Load(string sceneName, Action onCovered = null, Action onLoaded = null, Action onCompleted = null,
            Func<IEnumerator> pause = null)
        {
            if (_runner.IsBusy)
            {
                LoadCovered(sceneName, onLoaded);
                return;
            }

            Routiner.Start(LoadRoutine(sceneName, onCovered, onLoaded, onCompleted, pause));
        }

        private IEnumerator LoadRoutine(string sceneName, Action onCovered, Action onLoaded, Action onCompleted,
            Func<IEnumerator> pause)
        {
            yield return _runner.Play(GetEffect(), TransitionWork(sceneName, onCovered, onLoaded, pause), onCompleted);
        }

        private IEnumerator TransitionWork(string sceneName, Action onCovered, Action onLoaded, Func<IEnumerator> pause)
        {
            if (pause != null)
            {
                yield return pause();
            }

            onCovered.InvokeSafe();

            bool sceneLoaded = false;
            LoadCovered(sceneName, () =>
            {
                onLoaded.InvokeSafe();
                sceneLoaded = true;
            });

            yield return new WaitUntil(() => sceneLoaded);
            yield return _localization.WaitForLocalization();
        }

        private static ITransitionEffect GetEffect()
        {
            _effect ??= new EmptyTransitionEffect();
            return _effect;
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