
using System;
using Core;
using Managers;
using UI;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace ViewSystem
{
    public class HUD : MonoBehaviour
    {
        [SerializeField] private TopBar _topBar;
        [SerializeField] private Transform _windows;
        [SerializeField] private Transform _popups;
        [SerializeField] private Image _background;
        [SerializeField] private TransitionRunner _transitionRunner;
        
        public CoinBalance CoinBalance => _topBar.CoinBalance;
        public Transform Windows => _windows;
        public Transform Popups => _popups;
        public TransitionRunner TransitionRunner => _transitionRunner;
        
        public event Action ApplicationQuiting;
        
        private IViewManager _viewManager;


        private void OnDestroy()
        {
            _viewManager.OnWindowChanged -= OnWindowChanged;
        }

        public void Initialize(GameFlow flow, IViewManager viewManager)
        {
            _viewManager = viewManager;
            _viewManager.OnWindowChanged += OnWindowChanged;
            
            _topBar.Initialize(flow, viewManager);
            
            DontDestroyOnLoad(gameObject);
        }

        public void EnableTopBar(bool value) => _topBar.gameObject.SetActive(value);

        private void OnWindowChanged(Window window)
        {
            if (!window)
            {
                return;
            }

            _background.enabled = window.Background; 
            _background.sprite = window.Background;
        }

        private void OnApplicationQuit()
        {
            ApplicationQuiting.InvokeSafe();
        }
    }
}