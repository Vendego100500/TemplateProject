
using System;
using Managers;
using Parameters;
using UnityEngine;
using ViewSystem;

namespace Core
{
    public static class GameScenes
    {
        public const string Main = "Main";
        public const string Level = "Level";
    }
    
    public class GameFlow : IGameFlow
    {
        private readonly ViewManager _views;
        private readonly IDataCatalog _catalog;

        public GameFlow(ViewManager views, IDataCatalog catalog)
        {
            _views = views;
            _catalog = catalog;
        }

        public void ToMain(Action onCovered = null, Action onLoaded = null)
        {
            SceneLoader.Load(GameScenes.Main, onCovered, () =>
            {
                MusicManager.Instance.Play(_catalog.Game.Music);
                onLoaded?.Invoke();
            });
        }

        public void ToLevel(Action onCovered = null, Action onLoaded = null)
        {
            AudioClip levelMusic = null;
            SceneLoader.Load(GameScenes.Level, onCovered, () =>
            {
                MusicManager.Instance.Play(levelMusic);
                onLoaded?.Invoke();
            });
        }

        public void OpenMainMenu()
        {
            _views.OpenWindow(EPrefabNames.MainMenu);
        }

        public void ShowSettings()
        {
            //_views.ShowPopup<SettingsPopup>(EPrefabNames.Settings);
        }
    }
}
