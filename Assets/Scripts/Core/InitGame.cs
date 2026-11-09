
using System.Collections;
using System.Collections.Generic;
using Managers;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using ViewSystem;

namespace Core
{
    public class InitGame : MonoBehaviour
    {
        [SerializeField] private HUD _hud;
        [SerializeField] private SplashScreen _splashScreen;
        [SerializeField] private SfxManager _sfxManager;
        [SerializeField] private MusicManager _musicManager;
        
        
        private void Awake()
        {
            DontDestroyOnLoad(_sfxManager);
            DontDestroyOnLoad(_musicManager);
        }

        private IEnumerator Start()
        {
            yield return null;
            
            Game game = Game.Instance;
            GameInitializer gameInitializer = GameInitializer.Instance;
            gameInitializer.Initialize(game, _sfxManager, _musicManager, _hud);
            
            InitializerStage initializer = gameInitializer.Initializer;
            yield return new WaitUntil(() => initializer.Catalog >= ECatalogStage.GameDataAssetLoadingEnd);
            
            Application.targetFrameRate = game.Catalog.Game.TargetFrameRate;
            
            yield return new WaitUntil(() => initializer.Game >= EGameStage.LocalizationInitializationEnd);
            
            SettingsSave settings = game.Save.Settings;
            int currentLanguage = settings.GetLanguage();
            List<Locale> locales = LocalizationSettings.AvailableLocales.Locales;
            if (locales.Count > 0)
            {
                int languageIndex = Mathf.Clamp(currentLanguage, 0, locales.Count - 1);
                if (languageIndex != currentLanguage)
                {
                    settings.SetLanguage(languageIndex);
                }
                
                LocalizationSettings.SelectedLocale = locales[languageIndex];
            }
            
            yield return new WaitUntil(() => initializer.Game >= EGameStage.Initialized);
            yield return new WaitUntil(() => initializer.SplashScreen >= ESplashScreen.Disabled);
            
            game.Flow.ToMain(onLoaded: game.StartGame, onCompleted: () => initializer.Game = EGameStage.GameStarted);
        }
    }
}
