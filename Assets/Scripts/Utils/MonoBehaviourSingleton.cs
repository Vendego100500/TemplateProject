using Core;
using UnityEngine;

namespace Utils
{
    public abstract class MonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviourSingleton<T>
    {
        private static T _instance;
        
        public static T Instance
        {
            get
            {
                if (Game.IsQuiting)
                {
                    return null;
                }
                
                if (_instance)
                {
                    return _instance;
                }
                
                _instance = FindAnyObjectByType<T>();
                if (_instance)
                {
                    return _instance;
                }
                
                _instance = new GameObject(typeof(T).Name).AddComponent<T>();
                return _instance;
            }
        }
        
        protected virtual void Awake()
        {
            if (_instance && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = (T)this;
            DontDestroyOnLoad(gameObject);
        }
        
        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}