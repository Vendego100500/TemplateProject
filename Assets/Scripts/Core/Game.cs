
using System;
using Input;
using Managers;
using Parameters;
using UnityEngine;
using Utils;
using ViewSystem;

namespace Core
{
    public class Game : Singleton<Game>
    {
        public static event Action ApplicationQuiting;
        public static bool IsQuiting { get; private set; }
        
        public InputDevice InputDevice { get; }
        public PlayerSave Save { get; }
        public IGameFlow Flow { get; }
        public IDataCatalog Catalog { get; }

        private readonly HUD _hud;
        private readonly GlobalTimer _globalTimer;

        private Game()
        {
            _hud = HUD.Instance;
            _hud.ApplicationQuiting += () => ApplicationQuiting.InvokeSafe();;
            
            _globalTimer = GlobalTimer.Instance;
            _globalTimer.Tick += Tick;

            Catalog = DataAssets.Instance;
            Save = new PlayerSave();
            InputDevice = new InputDevice();
            Flow = new GameFlow(ViewManager.Instance, Catalog);
        }

        public void StartGame()
        {
            _globalTimer.Start(Catalog.Game.Fps);
            
            Flow.OpenMainMenu();
        }

        private void Tick(float deltaTime)
        {
            InputDevice.Tick();
            
            _hud.Tick();
        }

        protected override void OnApplicationQuit()
        {
            base.OnApplicationQuit();
            
            IsQuiting = true;
            
            InputDevice.Dispose();
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ApplicationQuiting = null;
            IsQuiting = false;
        }
    }
}