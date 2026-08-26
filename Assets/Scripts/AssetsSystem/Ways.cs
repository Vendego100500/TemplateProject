
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AssetsSystem
{
    public class Ways : ScriptableObject
    {
        [SerializeField, HideInInspector] private List<PathKeeper> _prefabsPathways = new();
        [SerializeField, HideInInspector] private List<PathKeeper> _resourcesPathways = new();

        private Dictionary<EPrefabNames, string> _prefabsPathwaysDict;
        private Dictionary<EResourceNames, string> _resourcesPathwaysDict;

        public Dictionary<EPrefabNames, string> PrefabsPathways
        {
            get
            {
                if (_prefabsPathwaysDict != null)
                {
                    return _prefabsPathwaysDict;
                }
                
                _prefabsPathwaysDict = new Dictionary<EPrefabNames, string>();
                if (_prefabsPathways.Count > 0)
                {
                    foreach (var item in _prefabsPathways)
                    {
                        _prefabsPathwaysDict.Add((EPrefabNames)item.Name, item.Path);
                    }
                }
                else
                {
                    Debug.LogError("Ways serialized data for prefabs empty");
                }
                return _prefabsPathwaysDict;
            }
        }

        public Dictionary<EResourceNames, string> ResourcesPathways
        {
            get
            {
                if (_resourcesPathwaysDict != null)
                {
                    return _resourcesPathwaysDict;
                }
                
                _resourcesPathwaysDict = new Dictionary<EResourceNames, string>();
                if (_resourcesPathways.Count > 0)
                {
                    foreach (var item in _resourcesPathways)
                    {
                        _resourcesPathwaysDict.Add((EResourceNames)item.Name, item.Path);
                    }
                }
                else
                {
                    Debug.LogError("Ways serialized data for resources empty");
                }
                return _resourcesPathwaysDict;
            }
        }

        public void Log()
        {
            Debug.Log("PrefabsPathways");
            foreach (var pr in PrefabsPathways)
            {
                Debug.LogWarning(pr.Key + " " + pr.Value);
            }
            Debug.Log("ResourcesPathways");
            foreach (var pr in ResourcesPathways)
            {
                Debug.LogWarning(pr.Key + " " + pr.Value);
            }
        }

#if UNITY_EDITOR
        public List<PathKeeper> PrefabsPathwaysSerializedData
        {
            get => _prefabsPathways;
            set => _prefabsPathways = value;
        }

        public List<PathKeeper> ResourcesPathwaysSerializedData
        {
            get => _resourcesPathways;
            set => _resourcesPathways = value;
        }
#endif
    }

    [Serializable]
    public class PathKeeper
    {
        public int Name;
        public string Path;

        public PathKeeper(int name, string path)
        {
            Name = name;
            Path = path;
        }
    }
}