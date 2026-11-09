
using System;
using Core;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace ViewSystem
{
    public interface ITopBarElements
    {
        event Action<ITopBarElements> OnTobBarElementsChanged;
        Sprite Currency { get; }
        Sprite Settings { get; }
        bool ShowsTopBarElement(ETopBarElement element);
    }
    
    public class TopBar : MonoBehaviour
    {
        [SerializeField] private CoinBalance _coinBalance;
        [SerializeField] private Image _settings;
        [SerializeField] private Button _shop;

        public CoinBalance CoinBalance => _coinBalance;
        
        private GameFlow _flow;
        private IViewManager _viewManager;
        private ITopBarElements _currentWindow;


        public void Initialize(GameFlow flow, IViewManager viewManager)
        {
            _flow = flow;
            
            _viewManager = viewManager;
            _viewManager.OnWindowChanged += OnWindowChanged;
        }

        private void OnDestroy()
        {
            if (_currentWindow != null)
            {
                _currentWindow.OnTobBarElementsChanged -= OnWindowChanged;
            }
            
            if (_viewManager != null)
            {
                _viewManager.OnWindowChanged -= OnWindowChanged;
            }
        }
        
        public void OnSettingsButtonClicked() => _flow.ShowSettings();


        private void OnWindowChanged(ITopBarElements window)
        {
            if (_currentWindow != null)
            {
                _currentWindow.OnTobBarElementsChanged -= OnWindowChanged;
            }
            
            _currentWindow = window;
            if (_currentWindow == null)
            {
                return;
            }

            _currentWindow.OnTobBarElementsChanged += OnWindowChanged;
            
            UpdateElements(window);

            _settings.sprite = window.Settings;
            _coinBalance.Background = window.Currency;
        }

        private void UpdateElements(ITopBarElements window)
        {
            SetActive(_coinBalance.gameObject, window.ShowsTopBarElement(ETopBarElement.CoinBalance));
            SetActive(_settings.gameObject, window.ShowsTopBarElement(ETopBarElement.Settings));
            
            _shop.interactable = window.ShowsTopBarElement(ETopBarElement.Shop);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
