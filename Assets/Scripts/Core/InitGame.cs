
using System.Collections;
using Managers;
using Managers.SoundManager;
using Parameters;
using UnityEngine;
using UnityEngine.Localization.Settings;
using ViewSystem;

namespace Core
{
    public class InitGame : MonoBehaviour
    {
        private void Awake()
        {
            ViewManager.Instance.OpenInitView();
            
            _ = MusicManager.Instance;
            _ = SfxManager.Instance;
        }

        private void Start()
        {
            Routiner.Start(StartGameCoroutine());
        }

        private static IEnumerator StartGameCoroutine()
        {
            yield return new WaitUntil(() => DataAssets.Instance.Initialized);
            
            Application.targetFrameRate = DataAssets.Instance.Game.Fps;

            yield return LocalizationSettings.InitializationOperation;
            
            int currentLanguage = SaveSystem.GetLanguage();
            var locales = LocalizationSettings.AvailableLocales.Locales;
            if (locales.Count > 0)
            {
                int languageIndex = Mathf.Clamp(currentLanguage, 0, locales.Count - 1);
                if (languageIndex != currentLanguage)
                {
                    SaveSystem.SetLanguage(languageIndex);
                }

                LocalizationSettings.SelectedLocale = locales[languageIndex];
            }

            SceneTransition.LoadScene("Main", () => Game.Instance.StartGame());
        }
    }
}