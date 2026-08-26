
using System;
using Input;
using Managers;
using Utils;
using ViewSystem;

namespace Core
{
    public class Game : Singleton<Game>
    {
        public static event Action ApplicationQuiting;
        public static void ApplicationQuitingInvoke() => ApplicationQuiting?.Invoke();
        
        public InputDevice InputDevice { get; }

        private readonly HUD _hud;
        private readonly GlobalTimer _globalTimer;
        private readonly ViewManager _viewManager;

        private Game()
        {
            _hud = HUD.Instance;
            
            InputDevice = new InputDevice();
            
            _globalTimer = GlobalTimer.Instance;
            _globalTimer.Tick += Tick;
            
            _viewManager = ViewManager.Instance;
        }

        public void StartGame()
        {
            _globalTimer.Start();
            
            _viewManager.OpenWindow(EPrefabNames.MainMenu);
        }

        private void Tick(float deltaTime)
        {
            InputDevice.Tick();
            
            _hud.Tick();
        }

        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            
            InputDevice.Dispose();
        }
    }
}