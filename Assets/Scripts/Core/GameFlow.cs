
using System;
using Managers;
using Parameters;
using UI.Popups;
using ViewSystem;

namespace Core
{
    public static class GameScenes
    {
        public const string Main = "Main";
    }
    
    public class GameFlow
    {
        private readonly IViewManager _viewManager;
        private readonly IMusic _music;
        private readonly IDataCatalog _catalog;
        private readonly SceneLoader _sceneLoader;
        

        public GameFlow(IViewManager viewManager, ILocalization localization, IDataCatalog catalog, 
           TransitionRunner runner, IMusic music)
        {
            _catalog = catalog;
            _music = music;
            _sceneLoader = new SceneLoader(localization, runner);
            _viewManager = viewManager;
        }

        public void ToMain(Action onCovered = null, Action onLoaded = null, Action onCompleted = null, bool showAds = false)
        {
            _sceneLoader.Load(GameScenes.Main, onCovered, () =>
            {
                _music.Play(_catalog.Game.Music);
                onLoaded?.Invoke();
            },
            onCompleted);
        }

        public void OpenMainMenu()
        {
            _viewManager.OpenWindow(EPrefabNames.MainMenu);
        }

        public void ShowSettings()
        {
            _viewManager.ShowPopup<SettingsPopup>(EPrefabNames.Settings);
        }

        public void ShowMessage(DataAssetMessage message, Action callback = null)
        {
            _viewManager.ShowPopup<MessagePopup>(EPrefabNames.Message, popup => popup.Init(message, callback));
        }
    }
}
