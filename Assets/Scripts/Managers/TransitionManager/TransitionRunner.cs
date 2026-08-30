
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

        private readonly Transform _transform;

        public TransitionOverlay(Transform transform, CanvasGroup canvasGroup, Image image)
        {
            _transform = transform;
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
            BringToFront();
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

        public void BringToFront()
        {
            _transform.SetAsLastSibling();
        }
    }

    public class TransitionRunner : MonoBehaviourSingleton<TransitionRunner>
    {
        private const string HUDTag = "HUD";

        private TransitionOverlay _overlay;

        public bool IsBusy => _overlay is { IsBusy: true };

        protected override void Awake()
        {
            base.Awake();

            _overlay = CreateOverlay();
        }

        public IEnumerator Play(ITransitionEffect effect, IEnumerator transitionWork)
        {
            _overlay.Begin();
            yield return effect.Cover(_overlay);
            yield return transitionWork;
            yield return effect.Reveal(_overlay);
            _overlay.Complete();
        }

        public void BringToFront()
        {
            _overlay.BringToFront();
        }

        private TransitionOverlay CreateOverlay()
        {
            GameObject hud = GameObject.FindWithTag(HUDTag);
            if (!hud)
            {
                throw new ArgumentException("HUD not found");
            }

            Canvas canvas = hud.GetComponentInChildren<Canvas>();
            if (!canvas)
            {
                throw new ArgumentException($"Canvas not found under GameObject with tag '{HUDTag}'");
            }

            transform.SetParent(canvas.transform, false);
            transform.SetAsLastSibling();

            if (transform is not RectTransform rectTransform)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            CanvasGroup canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            Image image = gameObject.AddComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;

            return new TransitionOverlay(transform, canvasGroup, image);
        }
    }
}
