
using System.Collections;
using Core;
using DG.Tweening;
using Managers;
using UnityEngine;

namespace ViewSystem
{
    public class Popup : View
    {
        private const float OpenDuration = 0.2f;
        private const float CloseDuration = 0.15f;
        
        [SerializeField] private Transform _content;
        [SerializeField] private ESfxId _openSound = ESfxId.ui_popup_open;
        [SerializeField] private ESfxId _closeSound =  ESfxId.ui_popup_close;

        protected ISound _sound;
        

        protected override void Awake()
        {
            base.Awake();

            _sound = Game.Instance.SoundManager;
            _content.localScale  = Vector3.zero;
        }


        public override void Close() => Close(true);


        protected override void ActivateWindow(EOpenType openType)
        {
            base.ActivateWindow(openType);
            
            _content.localScale = Vector3.zero;
            
            Routiner.Start(OpenRoutine());
        }
        
        private IEnumerator OpenRoutine()
        {
            _sound.Play(_openSound);

            if (LocalizationTracker != null)
            {
                yield return LocalizationTracker.ForceUpdate();
            }

            _content.DOScale(1, OpenDuration)
                .SetUpdate(true)
                .OnComplete(OnOpened);
            
            transform.SetAsLastSibling();
        }

        protected virtual void OnOpened()
        {
        }

        protected override void DeactivateWindow()
        {
            _sound.Play(_closeSound);

            _content.DOScale(0, CloseDuration).SetUpdate(true)
                .OnComplete(() => base.DeactivateWindow());
        }
    }
}