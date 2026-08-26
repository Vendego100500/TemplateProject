
using System;
using Core.Input;
using Core.Managers;
using UnityEngine;
using Utils;
using ViewSystem;

namespace Core
{
    public class Game : Singleton<Game>
    {
        private const string SceneTag = "Scene";

        public static event Action ApplicationQuiting;
        public static void ApplicationQuitingInvoke() => ApplicationQuiting?.Invoke();

        public SceneController SceneController { get; }

        private readonly HUD _hud;
        private readonly GlobalTimer _globalTimer;
        private readonly InputDevice _inputDevice;
        private readonly ViewManager _viewManager;

        private Game()
        {
            _hud = HUD.Instance;
            _inputDevice = new InputDevice();
            
            SceneController = new SceneController(GameObject.FindWithTag(SceneTag), _inputDevice);
            
            _globalTimer = GlobalTimer.Instance;
            _globalTimer.Tick += Tick;
            
            _viewManager = ViewManager.Instance;
        }

        public void StartGame()
        {
            _globalTimer.Start();
            
            ViewManager.Instance.OpenWindow(EPrefabNames.MainMenu);
        }

        private void Tick()
        {
            _inputDevice.Tick();
            
            SceneController.Tick();
            
            _hud.Tick();
            _viewManager.Current.Tick();
        }

        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            
            _inputDevice.Dispose();
        }
    }
}