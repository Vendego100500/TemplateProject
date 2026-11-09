using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace Managers
{
    public interface ITransitionEffect
    {
        IEnumerator Cover(TransitionOverlay overlay);
        IEnumerator Reveal(TransitionOverlay overlay);
    }

    public sealed class TransitionOverlay
    {
        public CanvasGroup CanvasGroup { get; }
        public Image Image { get; }
        public bool IsBusy { get; private set; }

        public TransitionOverlay(CanvasGroup canvasGroup, Image image)
        {
            CanvasGroup = canvasGroup;
            Image = image;
        }

        public void Begin()
        {
            IsBusy = true;
            CanvasGroup.DOKill();
            CanvasGroup.alpha = 0f;
            CanvasGroup.blocksRaycasts = true;
            CanvasGroup.interactable = true;
            Image.raycastTarget = true;
            Image.enabled = true;
        }

        public void Complete()
        {
            CanvasGroup.DOKill();
            CanvasGroup.alpha = 0f;
            CanvasGroup.blocksRaycasts = false;
            CanvasGroup.interactable = false;
            Image.raycastTarget = false;
            Image.enabled = false;
            IsBusy = false;
        }
    }

    [RequireComponent(typeof(CanvasGroup)), RequireComponent(typeof(Image))]
    public class TransitionRunner : MonoBehaviour
    {
        private TransitionOverlay _overlay;

        public bool IsBusy => _overlay is { IsBusy: true };

        
        private void Awake()
        {
            _overlay = new TransitionOverlay(GetComponent<CanvasGroup>(), GetComponent<Image>());
        }
        
        public IEnumerator Play(ITransitionEffect effect, IEnumerator transitionWork, Action onCompleted = null)
        {
            _overlay.Begin();
            yield return effect.Cover(_overlay);
            yield return transitionWork;
            yield return effect.Reveal(_overlay);
            _overlay.Complete();
            
            onCompleted.InvokeSafe();
        }
    }
}
