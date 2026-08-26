using System;
using Core;

namespace Utils
{
    public class Singleton<T> where T : Singleton<T>
    {
        private static T _instance;
        private static bool _isQuitting;
	
        public static T Instance 
        {
            get
            {
                if (_isQuitting)
                {
                    return null;
                }
				
                if (_instance != null)
                {
                    return _instance;
                }
	        
                _instance = Activator.CreateInstance(typeof(T), true) as T;
                if (_instance == null)
                {
                    throw new InvalidOperationException($"Could not create singleton instance for {typeof(T).Name}.");
                }
                _instance.Init();
				
                return _instance;
            }
        }

        public bool Initialized { get; private set; }

        protected virtual void Init()
        {
            Initialized = true;

            Game.ApplicationQuiting += OnApplicationQuit;
        }

        protected virtual void OnApplicationQuit()
        {
            _instance = null;
            _isQuitting = true;
        }
    }
}