
using UnityEngine;

namespace Managers
{
    public class PoolComponent : MonoBehaviour, IPoolObject
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

        public virtual void GetFromPool(bool activate = true)
        {
            InPool = false;
            if (activate)
            {
                _gameObject.SetActive(true);
            }
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