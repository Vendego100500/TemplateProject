
using System.Collections;
using Core.SceneSystem;
using Parameters;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

namespace Managers.SoundManager
{
    public class MusicManager : MonoBehaviourSingleton<MusicManager>
    {
        private const float CrossfadeDuration = 0.4f;

        private AudioSource _audioSource;
        private AudioClip _currentClip;
        private Coroutine _transitionRoutine;

        protected override void Awake()
        {
            base.Awake();

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.loop = true;
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;
            _audioSource.volume = 1f;

            SceneManager.sceneLoaded += OnSceneLoaded;
            SaveSystem.OnMusicChanged += OnMusicEnabledChanged;

            ApplyMusicEnabled(SaveSystem.IsMusicEnabled());
        }

        protected override void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SaveSystem.OnMusicChanged -= OnMusicEnabledChanged;

            base.OnDestroy();
        }

        private void OnMusicEnabledChanged(bool value) => ApplyMusicEnabled(value);

        private void ApplyMusicEnabled(bool value)
        {
            if (value)
            {
                if (_currentClip && !_audioSource.isPlaying)
                {
                    _audioSource.Play();
                    return;
                }

                _audioSource.UnPause();
                return;
            }

            _audioSource.Pause();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            PlayForScene(scene.name);
        }

        private void PlayForScene(string sceneName)
        {
            AudioClip clip = sceneName switch
            {
                "Main" => DataAssets.Instance.Game.Music,
                _ => null
            };

            Play(clip);
        }

        private void Play(AudioClip clip)
        {
            if (!clip || clip == _currentClip)
            {
                return;
            }

            _currentClip = clip;
            
            StartTransition(TransitionToClip(clip));
        }

        private void StartTransition(IEnumerator routine)
        {
            if (_transitionRoutine != null)
            {
                Routiner.Stop(_transitionRoutine);
            }

            _transitionRoutine = Routiner.Start(WrapTransition(routine));
        }

        private IEnumerator WrapTransition(IEnumerator routine)
        {
            yield return routine;
            
            _transitionRoutine = null;
        }

        private IEnumerator TransitionToClip(AudioClip clip)
        {
            if (_audioSource.isPlaying)
            {
                yield return FadeVolume(0f, CrossfadeDuration);
                _audioSource.Stop();
            }

            _audioSource.clip = clip;

            if (!SaveSystem.IsMusicEnabled())
            {
                yield break;
            }

            _audioSource.Play();
            yield return FadeVolume(1f, CrossfadeDuration);
        }

        private IEnumerator FadeVolume(float targetVolume, float duration)
        {
            yield return FadeVolume(_audioSource, targetVolume, duration);
        }

        private static IEnumerator FadeVolume(AudioSource source, float targetVolume, float duration)
        {
            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                yield return null;
            }

            source.volume = targetVolume;
        }
    }
}
