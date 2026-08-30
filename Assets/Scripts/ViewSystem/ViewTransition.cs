
using System;
using System.Collections;
using DG.Tweening;
using Managers;
using Utils;

namespace ViewSystem
{
    public static class ViewTransition
    {
        private static readonly ITransitionEffect Effect = new FadeTransitionEffect();

        public static void Play(Action onCovered)
        {
            TransitionRunner runner = TransitionRunner.Instance;
            if (runner.IsBusy)
            {
                onCovered.InvokeSafe();
                return;
            }

            Routiner.Start(runner.Play(Effect, TransitionWork(runner, onCovered)));
        }

        private static IEnumerator TransitionWork(TransitionRunner runner, Action onCovered)
        {
            onCovered.InvokeSafe();
            runner.BringToFront();
            yield return ViewManager.Instance.WaitForLocalization();
        }

        private sealed class FadeTransitionEffect : ITransitionEffect
        {
            private const float Duration = 0.1f;

            public IEnumerator Cover(TransitionOverlay overlay)
            {
                overlay.Image.enabled = true;
                yield return overlay.CanvasGroup.DOFade(1f, Duration)
                    .SetEase(Ease.Linear)
                    .SetUpdate(true)
                    .WaitForCompletion();
            }

            public IEnumerator Reveal(TransitionOverlay overlay)
            {
                yield return overlay.CanvasGroup.DOFade(0f, Duration)
                    .SetEase(Ease.Linear)
                    .SetUpdate(true)
                    .WaitForCompletion();
            }
        }
    }
}
