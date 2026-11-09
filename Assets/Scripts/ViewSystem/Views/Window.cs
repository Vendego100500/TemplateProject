
using System;
using UnityEngine;
using Utils;

namespace ViewSystem
{
    [Flags]
    public enum ETopBarElement : byte
    {
        None = 0,
        CoinBalance = 1 << 0,
        Settings = 1 << 1,
        Shop = 1 << 2,
        
        All = byte.MaxValue
    }

    public class Window : View, ITopBarElements
    {
        [SerializeField] private ETopBarElement _topBarElements = ETopBarElement.All;
        [SerializeField] protected Sprite _background;
        
        [Header("Top bar")]
        [SerializeField] protected Sprite _currency;
        [SerializeField] protected Sprite _settings;

        public event Action<ITopBarElements> OnTobBarElementsChanged;
        
        private Action<Window, bool> _onClose;
        private Action<Action> _onCloseTransition;
        
        public Sprite Background => _background;
        public Sprite Currency => _currency;
        public Sprite Settings => _settings;

        
        public void Open(EOpenType openType, Action<Window, bool> onClose, Action<Action> onCloseTransition)
        {
            if (openType == EOpenType.Open)
            {
                _onClose = onClose;
                _onCloseTransition = onCloseTransition;
            }

            Open(openType);
        }
        
        public override void Close()
        {
            base.Close();
            
            _onCloseTransition.InvokeSafe(() => Close(true));
        }

        public override void Close(bool removeFromStack)
        {
            base.Close(removeFromStack);
            
            _onClose.InvokeSafe(this, removeFromStack);
        }

        public bool ShowsTopBarElement(ETopBarElement element)
        {
            return (_topBarElements & element) == element;
        }

        protected void UpdateTopBarElements(ETopBarElement elements)
        {
            if (_topBarElements == elements)
            {
                return;
            }
            
            _topBarElements = elements;
            OnTobBarElementsChanged.InvokeSafe(this);
        }
    }
}