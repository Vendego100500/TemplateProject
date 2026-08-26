
using Managers.PoolManager;
using UnityEngine;

namespace Core.Actors
{
    public abstract class Actor : MonoBehaviour, IPoolObject
    {
        public GameObject GameObject
        {
            get
            {
                if (!_gameObject)
                {
                    _gameObject = gameObject;
                }

                return _gameObject;
            }
        }
        public IPool Pool { get; private set; }
        public bool InPool { get; private set; }
        public EntityId TemplateId { get; private set; }
        
        
        private GameObject _gameObject;
        

        public void Initialize(IPool pool, EntityId templateId)
        {
            Pool = pool;
            InPool = true;
            TemplateId = templateId;
            _gameObject = gameObject;
        }
        
        public virtual GameObject GetFromPool()
        {
            InPool = false;
            _gameObject.SetActive(true);
            return _gameObject;
        }

        public virtual void BackToPool()
        {
            InPool = true;
            transform.SetParent(Pool.Root);
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            _gameObject.SetActive(false);
        }
    }
}