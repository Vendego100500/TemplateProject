
using System;
using UnityEngine;
using UnityEngine.Localization;

namespace Parameters
{
    public enum EMessage : byte
    {
    }
    
    [CreateAssetMenu(fileName = "Message", menuName = "Scriptable Object/Parameters/Messages/Message", order = 2)]
    public class DataAssetMessage : ScriptableObject
    {
        [SerializeField] private EMessage _type;
        [SerializeField] private LocalizedString _label;
        [SerializeField] private LocalizedString _message;
        [SerializeField] private LocalizedString _advice;
        [SerializeField] private LocalizedString _button;
        [SerializeField] private Sprite _buttonIcon;
        
        public EMessage Type => _type;
        public LocalizedString Label => _label;
        public LocalizedString Message => _message;
        public LocalizedString Advice => _advice;
        public LocalizedString Button => _button;
        public Sprite ButtonIcon => _buttonIcon;
        

        public virtual void OnButtonClicked(Action callback) => callback();
    }
}