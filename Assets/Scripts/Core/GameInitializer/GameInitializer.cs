
using System.Collections;
using Managers;
using Parameters;
using UnityEngine;
using Utils;
using ViewSystem;

namespace Core
{
    public class GameInitializer : Singleton<GameInitializer>
    {
        public InitializerStage Initializer { get; } = new ();

        private bool _initialized;
        private Game _game;
        private HUD _hud;
        private SoundManager _soundManager;
        
        public void Initialize(Game game, SfxManager sfxManager, MusicManager musicManager, HUD hud)
        {
            if (_initialized)
            {
                return;
            }

            _game = game;
            _hud = hud;
            _soundManager = new SoundManager(sfxManager, musicManager, _game.Save.Settings);
            
            Routiner.Start(Initialize());
        }

        private IEnumerator Initialize()
        {
            DataCatalog catalog = _game.Catalog;
            
            Routiner.Start(_game.Initialize(_hud, _soundManager, Initializer));
            Routiner.Start(catalog.Initialize(Initializer));
            
            yield return new WaitUntil(() => Initializer.Game >= EGameStage.Initialized);
            
            Initializer.Game = EGameStage.PoolInitializationBegin;
            
            PoolManager poolManager = PoolManager.Instance;
            foreach (var obj in catalog.Game.PoolObjects)
            {
                yield return poolManager.AddObjectsPool(obj);
            }
            
            Initializer.Game = EGameStage.PoolInitializationEnd;
        }  
    }
}