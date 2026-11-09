
using System.Collections;
using System.Collections.Generic;
using AssetsSystem;
using UnityEngine;
using Utils;

namespace Managers
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
        void GetFromPool(bool activate);
        void BackToPool();
    }
    
    public interface IPoolObjectParameters
    {
        GameObject Prefab { get; }
        int Count { get; }
        int Steps { get; }
    }
    
    public class PoolManager : MonoBehaviourSingleton<PoolManager>
    {
        private readonly Dictionary<EntityId, Pool> _pools = new ();

        protected override void Awake()
        {   
            gameObject.SetActive(false);
            
            base.Awake();
        }

        public IEnumerator AddObjectsPool(IPoolObjectParameters source)
        {
            Transform root = AssetsManager.AddEmptyObject(transform, source.Prefab.name).transform;
            if (source.Prefab.GetComponent<IPoolObject>() == null)
            {
                Debug.LogError($"Pool object {source.Prefab.name} has no IPoolObject component");
                yield return null;
            }

            int max = Mathf.CeilToInt((float)source.Count / source.Steps);
            int count = Mathf.Min(source.Count, max);
            Pool pool = new Pool(root, source.Prefab, count);
            _pools.Add(source.Prefab.GetEntityId(), pool);
            
            while (count < source.Count)
            {
                yield return null;
                
                int n = Mathf.Min(source.Count - count, max);
                count += n;
                pool.AddNewObjects(n);
            }
        }

        public T Get<T>(T obj, Transform parent, bool activate = true) where T : IPoolObject
        {
            if (!Instance._pools.TryGetValue(obj.TemplateId, out Pool pool))
            {
                Transform root = AssetsManager.AddEmptyObject(Instance.transform, obj.GameObject.name).transform;
                pool = new Pool(root, obj.GameObject);
                Instance._pools.Add(obj.TemplateId, pool);
            }

            return GetNext<T>(pool, parent, activate);
        }

        public void Get<T>(T obj, Transform parent, int count, List<T> output, bool activate = true) where T : IPoolObject
        {
            output.Clear();
            if (!Instance._pools.TryGetValue(obj.TemplateId, out Pool pool))
            {
                Transform root = AssetsManager.AddEmptyObject(Instance.transform, obj.GameObject.name).transform;
                pool = new Pool(root, obj.GameObject);
                Instance._pools.Add(obj.TemplateId, pool);
            }

            if (output.Capacity < count)
            {
                output.Capacity = count;
            }

            for (int i = 0; i < count; i++)
            {
                output.Add(GetNext<T>(pool, parent, activate));
            }
        }

        private static T GetNext<T>(Pool pool, Transform parent, bool activate) where T : IPoolObject
        {
            IPoolObject poolObject = pool.GetNextFree(activate);
            Transform item = poolObject.GameObject.transform;
            item.SetParent(parent, false);
            item.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            item.localScale = Vector3.one;
            return (T)poolObject;
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
                _current = -1;
                
                AddNewObjects(size);
            }

            public Transform Root => _root;
            public GameObject Template => _template;


            public IPoolObject GetNextFree(bool activate)
            {
                int count = _objects.Count;
                for (int offset = 1; offset <= count; offset++)
                {
                    int i = (_current + offset) % count;
                    IPoolObject poolObject = _objects[i];
                    if (!poolObject.InPool)
                    {
                        continue;
                    }

                    _current = i;
                    return Activate(poolObject, activate);
                }

                IPoolObject added = AddNewObject();
                _current = _objects.Count - 1;
                return Activate(added, activate);
            }

            public void AddNewObjects(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    AddNewObject();
                }
            }

            private static IPoolObject Activate(IPoolObject poolObject, bool activate)
            {
                poolObject.GetFromPool(activate);
                return poolObject;
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