
using System;
using Parameters;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using Utils;
using ViewSystem;

namespace UI.Popups
{
    public class MessagePopup : Popup
    {
        [SerializeField] private LocalizeStringEvent _label;
        [SerializeField] private LocalizeStringEvent _message;
        [SerializeField] private LocalizeStringEvent _advice;
        [SerializeField] private LocalizeStringEvent _button;
        [SerializeField] private Image _buttonIcon;

        private Action _callback;
        private Action<Action> _action;

        public void Init(DataAssetMessage message, Action callback = null)
        {
            _action = message.OnButtonClicked;
            _callback = callback;
            
            _label.StringReference = message.Label;
            _label.RefreshString();
            
            _message.StringReference = message.Message;
            _message.RefreshString();
            
            _advice.gameObject.SetActive(!message.Advice.IsEmpty);
            _advice.StringReference = message.Advice;
            _advice.RefreshString();
            
            _button.StringReference = message.Button;
            _button.RefreshString();
            
            _buttonIcon.gameObject.SetActive(message.ButtonIcon);
            _buttonIcon.sprite = message.ButtonIcon;
        }

        public void OnButtonClicked()
        {
            _action.Invoke(() =>
            {
                _callback.InvokeSafe();
                
                Close();
            });
        }
    }
}