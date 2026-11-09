
using Managers;
using UnityEngine;

namespace ViewSystem
{
    public enum EOpenType : byte
    {
        Open = 0,
        Reopen = 1,
        Back = 2
    }
    
    public abstract class View : MonoBehaviour
    {
        [SerializeField] private EPrefabNames _prefab;

        public EPrefabNames Prefab => _prefab;
        public bool IsActive { get; private set; }
        public LocalizationTracker LocalizationTracker { get; private set; }


        protected virtual void Awake()
        {
            LocalizationTracker = new LocalizationTracker(this);
        }


        public void Open(EOpenType openType)
        {
            ActivateWindow(openType);
        }
        
        public virtual void Close() { }

        public virtual void Close(bool removeFromStack)
        {
            DeactivateWindow();
        }

        protected virtual void ActivateWindow(EOpenType openType)
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