
using System.Collections;
using DG.Tweening;
using Managers;
using UnityEngine;

namespace ViewSystem
{
    public class Popup : View
    {
        private const float OpenDuration = 0.2f;
        private const float CloseDuration = 0.15f;
        
        [SerializeField] private Transform _view;

        protected override void Awake()
        {
            base.Awake();
            
            _view.localScale  = Vector3.zero;
        }


        public override void Close() => Close(true);


        protected override void ActivateWindow(bool reopen = false)
        {
            base.ActivateWindow(reopen);
            
            _view.localScale = Vector3.zero;
            
            Routiner.Start(OpenRoutine(reopen));
        }
        
        private IEnumerator OpenRoutine(bool reopen)
        {
            _sfxManager.Play(ESfxId.ui_popup_open);

            if (LocalizationTracker != null)
            {
                yield return LocalizationTracker.ForceUpdate();
            }

            _view.DOScale(1, OpenDuration).SetUpdate(true);
            
            transform.SetAsLastSibling();
        }

        protected override void DeactivateWindow()
        {
            _sfxManager.Play(ESfxId.ui_popup_close);

            _view.DOScale(0, CloseDuration).SetUpdate(true)
                .OnComplete(() => base.DeactivateWindow());
        }
    }
}