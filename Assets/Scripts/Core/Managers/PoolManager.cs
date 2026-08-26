
using System.Collections.Generic;
using AssetsSystem;
using Parameters;
using UnityEngine;
using Utils;

namespace Core.Managers
{
    public interface IPool
    {
        GameObject Template { get; }
        Transform Root { get; }
    }
    
    public interface IPoolObject
    {
        GameObject GameObject { get; }
        IPool Pool { get; }
        bool InPool { get; }
        EntityId TemplateId { get; }

        void Initialize(IPool pool, EntityId templateId);
        GameObject GetFromPool();
        void BackToPool();
    }
    
    public interface IPoolParameters
    {
        List<IPoolObjectParameters> GetPoolObjects();
    }
    
    public interface IPoolObjectParameters
    {
        GameObject Prefab { get; }
        int Count { get; }
    }
    
    public class PoolManager : MonoBehaviourSingleton<PoolManager>
    {
        private readonly Dictionary<EntityId, Pool> _pools = new ();

        protected override void Awake()
        {   
            foreach (ScriptableObject dataAsset in DataAssets.Instance.Parameters)
            {
                if (dataAsset is not IPoolParameters poolParameters)
                {
                    continue;
                }
                
                Transform root = AssetsManager.AddEmptyObject(transform, dataAsset.name).transform;
                foreach (IPoolObjectParameters poolObject in poolParameters.GetPoolObjects())
                {
                    if (poolObject.Prefab.GetComponent<IPoolObject>() == null)
                    {
                        Debug.LogError($"Pool object {dataAsset.name} has no IPoolObject component");
                        continue;
                    }
                    Pool pool = new Pool(root, poolObject.Prefab, poolObject.Count);
                    _pools.Add(poolObject.Prefab.GetEntityId(), pool);
                }
            }

            gameObject.SetActive(false);
            
            base.Awake();
        }

        public static GameObject Get<T>(T obj) where T : IPoolObject
        {
            if (!Instance._pools.TryGetValue(obj.TemplateId, out Pool pool))
            {
                Transform root = AssetsManager.AddEmptyObject(Instance.transform, obj.GameObject.name).transform;
                pool = new Pool(root, obj.GameObject);
                Instance._pools.Add(obj.TemplateId, pool);
            }
            
            return pool.GetNextFree();
        }

        public static T Get<T>(T obj, Transform parent) where T : IPoolObject
        {
            Transform transform = Get(obj).transform;
            transform.SetParent(parent, false);
            transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            transform.localScale = Vector3.one;
            return transform.GetComponent<T>();
        }

        public static T2 Get<T, T2>(T obj) where T : IPoolObject
        {
            return Get(obj).GetComponent<T2>();
        }


        private class Pool : IPool
        {
            private readonly Transform _root;
            private readonly GameObject _template;
            private readonly List<IPoolObject> _objects;
            private int _current;
            
            public Pool(Transform parent, GameObject template, int size = 1)
            {
                _template = template;
                _objects = new List<IPoolObject>(size);
                _root = AssetsManager.AddEmptyObject(parent, _template.ToString()).transform;
                for (int i = 0; i < size; i++)
                {
                    AddNewObject();
                }
                _current = -1;
            }

            public Transform Root => _root;
            public GameObject Template => _template;

            public GameObject GetNextFree()
            {
                int next = _current + 1;
                for (int i = next; i <= _objects.Count; i++)
                {
                    // Reached the end of the list
                    if (i == _objects.Count)
                    {
                        i = 0;
                    }

                    // All objects are busy
                    if (i == _current)
                    {
                        break;
                    }

                    if (_objects[i].InPool)
                    {
                        _current = i;
                        return _objects[i].GetFromPool();
                    }
                }

                _current = _objects.Count;

                return AddNewObject().GetFromPool();
            }

            private IPoolObject AddNewObject()
            {
                GameObject obj = AssetsManager.Instantiate(_template, _root);
                obj.SetActive(false);
                    
                IPoolObject poolObject = obj.GetComponent<IPoolObject>();
                poolObject.Initialize(this, _template.GetEntityId());
                _objects.Add(poolObject);

                return poolObject;
            }
        }
    }
}