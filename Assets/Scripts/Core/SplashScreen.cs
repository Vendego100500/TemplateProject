
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Core
{
    public class SplashScreen : MonoBehaviour
    {
        [SerializeField] private Image _logo;
        [SerializeField] private Image _background;
        [SerializeField, Min(1f)] private float _scale = 1.2f;
        [SerializeField, Min(0f)] private float _duration = 1f;
        [SerializeField, Min(0f)] private float _disableDelay = 1f;
        [SerializeField] private Sprite[] _logos;
        

        private IEnumerator Start()
        {
#if UNITY_EDITOR
            
            _background.color = Color.clear;
            GameInitializer.Instance.Initializer.SplashScreen = ESplashScreen.Disabled;
            yield break;
            
#else

            _logo.DOKill();
            _logo.color = new Color(
                _logo.color.r,
                _logo.color.g,
                _logo.color.b,
                0f
            );
            
            yield return null;

            InitializerStage initializer = GameInitializer.Instance.Initializer;
            initializer.SplashScreen = ESplashScreen.Started;

            foreach (var logo in _logos)
            {
                //Debug.Log($"Initializer SplashScreen: Logo: {logo.name} - Time: {Time.realtimeSinceStartup:F2}");
                _logo.sprite = logo;

                _logo.transform.DOScale(_scale, 2 * _duration)
                    .SetEase(Ease.Linear)
                    .SetUpdate(true);
                
                yield return FadeLogo(1f);
                yield return FadeLogo(0f);

                _logo.transform.localScale = Vector3.one;
            }
            
            _background.color = Color.clear;
            
            initializer.SplashScreen = ESplashScreen.Completed;
            
            yield return new WaitForSecondsRealtime(_disableDelay);
            
            initializer.SplashScreen = ESplashScreen.Disabled;
#endif
        }

#if !UNITY_EDITOR
        private IEnumerator FadeLogo(float alpha)
        {
            yield return _logo
                .DOFade(alpha, _duration)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true)
                .WaitForCompletion();
        }
#endif

        private void OnDisable()
        {
            if (_logo)
            {
                _logo.DOKill();
            }
        }
    }
}