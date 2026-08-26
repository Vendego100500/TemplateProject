
using System;
using System.Collections;
using AssetsSystem;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utils;
using ViewSystem;

namespace Managers
{
    public class SceneTransition : MonoBehaviourSingleton<SceneTransition>
    {
        private const float FadeDuration = 0.1f;

        private CanvasGroup _canvasGroup;
        private Image _image;
        private bool _isTransitioning;

        protected override void Awake()
        {
            base.Awake();

            EnsureOverlay();
        }

        public static void LoadScene(string sceneName, Action onLoaded = null)
        {
            if (Instance._isTransitioning)
            {
                Instance.LoadSceneCovered(sceneName, onLoaded);
                return;
            }

            Routiner.Start(Instance.LoadSceneCoroutine(sceneName, onLoaded));
        }

        public static void Play(Action onCovered)
        {
            if (Instance._isTransitioning)
            {
                onCovered.InvokeSafe();
                return;
            }

            Routiner.Start(Instance.PlayCoroutine(onCovered));
        }

        private IEnumerator LoadSceneCoroutine(string sceneName, Action onLoaded)
        {
            transform.SetAsLastSibling();

            BeginTransition();
            _canvasGroup.alpha = 1;
            _image.enabled = false;

            bool sceneLoaded = false;
            LoadSceneCovered(sceneName, () =>
            {
                onLoaded.InvokeSafe();
                sceneLoaded = true;
            });

            yield return new WaitUntil(() => sceneLoaded);

            transform.SetAsLastSibling();

            yield return ViewManager.Instance.Current?.LocalizationTracker?.WaitForUpdate();
            
            CompleteTransition();
        }

        private IEnumerator PlayCoroutine(Action onCovered)
        {
            BeginTransition();

            yield return _canvasGroup.DOFade(1, FadeDuration)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .WaitForCompletion();

            onCovered.InvokeSafe();
            transform.SetAsLastSibling();

            yield return ViewManager.Instance.Current?.LocalizationTracker?.WaitForUpdate();
            yield return _canvasGroup.DOFade(0, FadeDuration)
                .SetEase(Ease.Linear)
                .SetUpdate(true)
                .WaitForCompletion();

            CompleteTransition();
        }

        private void LoadSceneCovered(string sceneName, Action onLoaded)
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
                onLoaded?.Invoke();
            }
        }
        
        private void BeginTransition()
        {
            _isTransitioning = true;
            _canvasGroup.DOKill();
            _canvasGroup.alpha = 0;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
            _image.raycastTarget = true;
            _image.enabled = true;
            
            transform.SetAsLastSibling();
        }

        private void CompleteTransition()
        {
            _canvasGroup.DOKill();
            _canvasGroup.alpha = 0;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
            _image.raycastTarget = false;
            _image.enabled = false;
            _isTransitioning = false;
        }

        private void EnsureOverlay()
        {
            transform.SetParent(ViewManager.Instance.Root, false);
            transform.SetAsLastSibling();

            if (transform is not RectTransform rectTransform)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }
            
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            _image = gameObject.AddComponent<Image>();
            _image.color = Color.black;
            _image.raycastTarget = false;
        }
    }
}
