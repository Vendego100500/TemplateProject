
using Managers;
using TMPro;
using UnityEngine;
using Core;
using UnityEngine.UI;

namespace UI
{
    public class CoinBalance : MonoBehaviour
    {
        [SerializeField] private Image _background;
        [SerializeField] private TMP_Text _amountText;
        [SerializeField] private RectTransform _coinAnchor;
        
        public RectTransform CoinAnchor => _coinAnchor;
        public Sprite Background { set => _background.sprite = value; }

        private int _displayedAmount;
        private bool _ignoreCoinsChanged;
        private EconomySave _economy;


        private void OnEnable()
        {
            _economy = Game.Instance.Save.Economy;
            _economy.OnCoinsChanged += HandleCoinsChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (_economy != null)
            {
                _economy.OnCoinsChanged -= HandleCoinsChanged;
            }
            _ignoreCoinsChanged = false;
        }

        public void SetIgnoreCoinsChanged(bool ignore)
        {
            _ignoreCoinsChanged = ignore;
        }

        public void SetDisplay(int amount)
        {
            _displayedAmount = Mathf.Max(0, amount);
            _amountText.text = _displayedAmount.ToString("N0");
        }

        private void HandleCoinsChanged(int amount)
        {
            if (_ignoreCoinsChanged)
            {
                return;
            }
            
            SetDisplay(amount);
        }

        public void Refresh()
        {
            SetDisplay(_economy.GetCoins());
        }
    }
}