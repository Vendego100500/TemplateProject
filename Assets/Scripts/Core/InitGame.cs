
using System.Collections;
using Managers;
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
            var game = Game.Instance;
            SettingsSave settings = game.Save.Settings;
            MusicManager.Instance.Bind(settings);
            SfxManager.Instance.Bind(settings);
            
            ViewManager.Instance.OpenInitView();
        }

        private void Start()
        {
            Routiner.Start(StartGameCoroutine());
        }

        private static IEnumerator StartGameCoroutine()
        {
            Game game = Game.Instance;
            IDataCatalog catalog = game.Catalog;
            
            yield return new WaitUntil(() => catalog.Initialized);
            
            Application.targetFrameRate = catalog.Game.Fps;

            yield return LocalizationSettings.InitializationOperation;

            PlayerSave save = game.Save;
            
            int currentLanguage = save.Settings.GetLanguage();
            var locales = LocalizationSettings.AvailableLocales.Locales;
            if (locales.Count > 0)
            {
                int languageIndex = Mathf.Clamp(currentLanguage, 0, locales.Count - 1);
                if (languageIndex != currentLanguage)
                {
                    save.Settings.SetLanguage(languageIndex);
                }

                LocalizationSettings.SelectedLocale = locales[languageIndex];
            }

            game.Flow.ToMain(onLoaded: game.StartGame);
        }
    }
}
