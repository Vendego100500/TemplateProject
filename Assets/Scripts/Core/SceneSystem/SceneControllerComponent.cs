
using System;
using System.Collections.Generic;
using AssetsSystem;
using Input;
using Managers;
using Parameters;
using UnityEngine;
using UnityEngine.Tilemaps;
using Utils;

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
        public IGameFlow Flow { get; private set; }
        public IDataCatalog Catalog { get; private set; }

        public SpriteRenderer Background => _background;
        

        private void Awake()
        {
            Camera = Camera.main;
            
            InputDevice = Game.Instance.InputDevice;
            Save = Game.Instance.Save;
            Flow = Game.Instance.Flow;
            Catalog = Game.Instance.Catalog;

            _controller = CreateSession();
        }

        private void Start()
        {
            OpenLevel();

            GlobalTimer.Instance.Tick += Tick;
        }

        private void OnDestroy()
        {
            if (GlobalTimer.Instance != null)
            {
                GlobalTimer.Instance.Tick -= Tick;
            }

            _controller?.OnDestroy();
            
            Clear();
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

        private void OpenLevel()
        {
            Clear();

            OnLevelLoaded.InvokeSafe();
        }

        private void Clear()
        {
        }
    }
}
