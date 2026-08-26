
using System;
using Managers;
using Managers.SoundManager;
using UnityEngine;

namespace ViewSystem
{
    public class View : MonoBehaviour
    {
        [SerializeField] private EPrefabNames _prefab;
        [SerializeField] private bool _ignoreStack;

        public event Action<View, bool> OnClose;

        public EPrefabNames Prefab => _prefab;
        public bool IgnoreStack => _ignoreStack;
        public bool IsActive { get; private set; }
        public LocalizationTracker LocalizationTracker { get; private set; }
        
        protected SfxManager _sfxManager;


        protected virtual void Awake()
        {
            LocalizationTracker = new LocalizationTracker(this);
            _sfxManager = SfxManager.Instance;
        }


        public void Open()
        {
            ActivateWindow();
        }

        public void ReOpen()
        {
            ActivateWindow(true);
        }
        
        public virtual void Close()
        {
            SceneTransition.Play(() => Close(true));
        }

        internal void Close(bool removeFromStack)
        {
            DeactivateWindow();

            if (OnClose != null)
            {
                OnClose.Invoke(this, removeFromStack);
                OnClose = null;
            }
        }

        protected virtual void ActivateWindow(bool reopen = false)
        {
            gameObject.SetActive(true);
            IsActive = true;
        }

        protected virtual void DeactivateWindow()
        {
            gameObject.SetActive(false);
            IsActive = false;
        }
    }
}