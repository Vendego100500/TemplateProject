using System;
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

        public void Open()
        {
            ActivateWindow();
        }

        public void ReOpen()
        {
            ActivateWindow();
        }
        
        public void Close() => Close(true);

        internal void Close(bool removeFromStack)
        {
            DeactivateWindow();

            if (OnClose != null)
            {
                OnClose.Invoke(this, removeFromStack);
                OnClose = null;
            }
        }
        
        public virtual void Tick() { }

        private void ActivateWindow()
        {
            gameObject.SetActive(true);
            IsActive = true;
        }

        private void DeactivateWindow()
        {
            gameObject.SetActive(false);
            IsActive = false;
        }
    }
}