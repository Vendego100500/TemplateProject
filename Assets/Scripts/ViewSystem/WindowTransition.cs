
using System;
using System.Collections;
using DG.Tweening;
using Managers;
using Utils;

namespace ViewSystem
{
    public class WindowTransition
    {
        private static readonly ITransitionEffect Effect = new FadeTransitionEffect();
        
        private readonly TransitionRunner _runner;
        private readonly ILocalization _localization;

        public WindowTransition(ILocalization localization, TransitionRunner runner)
        {
            _runner = runner;
            _localization = localization;
        }

        public void Play(Action onCovered)
        {
            if (_runner.IsBusy)
            {
                onCovered.InvokeSafe();
                return;
            }

            Routiner.Start(_runner.Play(Effect, TransitionWork(onCovered)));
        }

        private IEnumerator TransitionWork(Action onCovered)
        {
            onCovered.InvokeSafe();
            yield return _localization.WaitForLocalization();
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
