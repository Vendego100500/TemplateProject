
using System;
using System.Collections;
using Input;
using Managers;
using Parameters;
using UI;
using UnityEngine;
using UnityEngine.Localization.Settings;
using Utils;
using ViewSystem;

namespace Core
{
    public class Game : Singleton<Game>
    {
        public static event Action ApplicationQuiting;
        public static bool IsQuiting { get; private set; }
        
        public GlobalTimer GlobalTimer { get; }
        public InputDevice InputDevice { get;}
        public PlayerSave Save { get; }
        public DataCatalog Catalog { get; }
        public GameFlow Flow { get; private set; }
        public IViewManager ViewManager { get; private set; }
        public SoundManager SoundManager { get; private set; }
        public CoinBalance CoinBalance => _hud.CoinBalance;

        private HUD _hud;


        private Game()
        {
            GlobalTimer = new GlobalTimer(Time.unscaledTime);
            GlobalTimer.Tick += Tick;
            
            Catalog = new DataCatalog();
            Save = new PlayerSave();
            InputDevice = new InputDevice();
        }

        public IEnumerator Initialize(HUD hud, SoundManager soundManager, InitializerStage initializer)
        {
            initializer.Game = EGameStage.Started;
            
            yield return new WaitUntil(() => initializer.Catalog >= ECatalogStage.GameDataAssetLoadingEnd);
            
            initializer.Game = EGameStage.FirstStageBegin;
            
            SoundManager = soundManager;
            
            _hud = hud;
            _hud.ApplicationQuiting += () => ApplicationQuiting.InvokeSafe();
            yield return null;
            
            ILocalization localization = new LocalizationManager(Save.Settings);
            ViewManager = new ViewManager(_hud.Windows, _hud.Popups, localization, _hud.TransitionRunner);
            ViewManager.OnWindowChanged += localization.OnWindowChanged;
            
            Flow = new GameFlow(ViewManager, localization, Catalog, _hud.TransitionRunner, SoundManager);
            
            initializer.Game = EGameStage.FirstStageEnd;
            
            yield return LocalizationSettings.InitializationOperation;
            
            initializer.Game = EGameStage.LocalizationInitializationEnd;
            
            yield return new WaitUntil(() => initializer.Catalog >= ECatalogStage.Initialized);
            
            _hud.Initialize(Flow, ViewManager);
            
            initializer.Game = EGameStage.Initialized;
        }

        public void StartGame()
        {
            _hud.EnableTopBar(true);
            
            InputDevice.Activate();
            
            GlobalTimer.Start(Catalog.Game.TickRate);
            
            ViewManager.Clean();
            Flow.OpenMainMenu();
        }

        private void Tick(float deltaTime)
        {
            InputDevice.Tick();
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
