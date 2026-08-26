using UnityEngine;

namespace Managers.PoolManager
{
    public class PoolMonoBehaviour : MonoBehaviour, IPoolObject
    {
        protected GameObject _gameObject;
        
        #region IPoolObject

        public GameObject GameObject
        {
            get
            {
                if (!_gameObject)
                {
                    _gameObject = gameObject;
                }

                return gameObject;
            }
        }
        public IPool Pool { get; private set; }
        public bool InPool { get; private set; }

        public EntityId TemplateId => _templateId.IsValid() ? _templateId : gameObject.GetEntityId();

        private EntityId _templateId;
        

        public virtual void Initialize(IPool pool, EntityId templateId)
        {
            Pool = pool;
            InPool = true;
            _templateId = templateId;
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
            _gameObject.SetActive(false);

            if (Pool.Root)
            {
                transform.SetParent(Pool.Root);
                transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
        }
        #endregion
    }
}