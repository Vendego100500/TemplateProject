
using System;
using Input;
using Managers;
using Parameters;
using UI;
using UnityEngine;
using Utils;
using ViewSystem;

namespace Core.SceneSystem
{
    public class SceneControllerComponent : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _background;
        
        private ISceneController _controller;
        
        public Action OnLevelLoaded;

        public Camera Camera { get; private set; }
        public InputDevice InputDevice { get; private set; }
        public PlayerSave Save { get; private set; }
        public GameFlow Flow { get; private set; }
        public IDataCatalog Catalog { get; private set; }
        public IViewManager ViewManager { get; private set; }
        public ISound Sound { get; private set; }
        public CoinBalance CoinBalance { get; private set; }

        private GlobalTimer _globalTimer;
        

        private void Awake()
        {
            Camera = Camera.main;
            
            _globalTimer = Game.Instance.GlobalTimer;
            
            InputDevice = Game.Instance.InputDevice;
            Save = Game.Instance.Save;
            Flow = Game.Instance.Flow;
            Catalog = Game.Instance.Catalog;
            ViewManager = Game.Instance.ViewManager;
            Sound = Game.Instance.SoundManager;
            CoinBalance = Game.Instance.CoinBalance;

            _controller = CreateSession();
        }

        private void Start()
        {
            OpenLevel();

            _globalTimer.Tick += Tick;
        }

        private void OnDestroy()
        {
            _globalTimer.Tick -= Tick;

            _controller?.OnDestroy();
        }

        public Vector2 ScreenToWorldPoint(Vector2 screenPosition)
        {
            Vector3 point = Camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -Camera.transform.position.z));
            return new Vector2(point.x, point.y);
        }

        private ISceneController CreateSession()
        {
            return null;
        }

        private void Tick(float deltaTime)
        {
            if (_controller != null && !_controller.Tick(deltaTime))
            {
                return;
            }
        }

        private void LateUpdate()
        {
            _controller.OnLateUpdate(Time.deltaTime);
        }

        private void OpenLevel()
        {
            OnLevelLoaded.InvokeSafe();
        }
    }
}
