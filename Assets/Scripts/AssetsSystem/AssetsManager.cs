
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Utils;
using Object = UnityEngine.Object;

namespace AssetsSystem
{
    public class AssetsManager : Singleton<AssetsManager>
    {
        private static readonly Regex RegexResources = new(@"^.*\bResources/\b");

        private readonly Dictionary<EResourceNames, Sprite[]> _atlasesCache;
        private readonly Ways _ways;
        
        private AssetsManager()
        {
            _atlasesCache =  new Dictionary<EResourceNames, Sprite[]>();
            _ways = Resources.Load<Ways>("ScriptableObjects/Ways");
        }
        
        public static GameObject Instantiate(GameObject prefab, Transform parent)
        {
            if (!prefab)
            {
                Debug.LogError($"Fail to instantiate prefab {prefab}");
                return null;
            }
            
            GameObject result = Object.Instantiate(prefab);
            // ReSharper disable once Unity.InstantiateWithoutParent
            //если парент будет задизейблен во время инстанса - на инстанциируемом обьекте не вызовется Awake и OnDestroy
            //поэтому парент ставим после инстанциирования
            result.transform.SetParent(parent, false);
            return result;
        }

        public GameObject Instantiate(EPrefabNames name, Transform parent)
        {
            GameObject prefab = GetPrefab(name);
            return Instantiate(prefab, parent);
        }
        
        public T Instantiate<T>(GameObject prefab, Transform parent) where T : Component
        {
            return Instantiate(prefab, parent)?.GetComponent<T>();
        }
        
        public T Instantiate<T>(EPrefabNames name, Transform parent) where T : Component
        {
            return Instantiate(name, parent)?.GetComponent<T>();
        }

        public static GameObject AddEmptyObject(Transform parent, string name = "Empty")
        {
            GameObject result = new GameObject(name);
            result.transform.SetParent(parent, false);
            return result;
        }

        public T GetPrefab<T>(long id) where T : Component
        {
            return GetPrefab(id).GetComponent<T>();
        }

        public T GetPrefab<T>(EPrefabNames name) where T : Component
        {
            return GetPrefab(name).GetComponent<T>();
        }

        public T GetPrefab<T>(string path) where T : Component
        {
            return GetPrefab(path).GetComponent<T>();
        }

        public GameObject GetPrefab(long id)
        {
            return GetPrefab((EPrefabNames)id);
        }

        public GameObject GetPrefab(EPrefabNames name)
        {
            if (!_ways)
            {
                Debug.LogError("AssetsManager.GetPrefab Ways is null, only string path allowed");
                return null;
            }

            if (_ways.PrefabsPathways.TryGetValue(name, out string path))
            {
                return GetPrefab(path);
            }

            Debug.LogErrorFormat(
                "Error loading object of type GameObject - prefab not found by specified enum name {0}",
                name.ToString());
            return null;
        }

        public static GameObject GetPrefab(string path)
        {
            GameObject go = LoadAsset<GameObject>(path);
            if (!go)
            {
                Debug.LogErrorFormat("Error loading object of type GameObject - prefab not found by specified path {0}",
                    path);
            }

            return go;
        }

        public T GetResource<T>(long id) where T : Object
        {
            return GetResource<T>((EResourceNames)id);
        }

        public T GetResource<T>(EResourceNames name, string resourceNameWithExt = "") where T : Object
        {
            if (!_ways)
            {
                Debug.LogError("AssetsManager.GetPrefab Ways is null, only string path allowed");
                return null;
            }

            if (_ways.ResourcesPathways.TryGetValue(name, out string path))
            {
                if (!string.IsNullOrEmpty(resourceNameWithExt))
                {
                    path += "/" + resourceNameWithExt;
                }

                return LoadAsset<T>(path);
            }

            Debug.LogErrorFormat("Error getting resource of type {0} - resource not found by specified enum name {1}",
                typeof(T), name.ToString());
            return null;
        }

        public T[] GetAllResources<T>(EResourceNames name, string resourceNameWithExt = "") where T : Object
        {
            if (!_ways)
            {
                Debug.LogError("AssetsManager.GetPrefab Ways is null, only string path allowed");
                return null;
            }

            if (_ways.ResourcesPathways.TryGetValue(name, out string path))
            {
                if (!string.IsNullOrEmpty(resourceNameWithExt))
                {
                    path += "/" + resourceNameWithExt;
                }

                return LoadAllAssets<T>(path);
            }

            Debug.LogErrorFormat("Error getting resource of type {0} - resource not found by specified enum name {1}",
                typeof(T), name.ToString());
            return null;
        }

        public static T LoadAsset<T>(string path) where T : Object
        {
#if UNITY_EDITOR
            if (!IsResourcesPath(path) && path.StartsWith("Assets/", System.StringComparison.Ordinal))
            {
                return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            }
#endif
            return Resources.Load<T>(PathFromResources(path));
        }

        public static T[] LoadAllAssets<T>(string path) where T : Object
        {
#if UNITY_EDITOR
            if (!IsResourcesPath(path) && path.StartsWith("Assets/", System.StringComparison.Ordinal))
            {
                T asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
                return asset ? new[] { asset } : System.Array.Empty<T>();
            }
#endif
            return Resources.LoadAll<T>(PathFromResources(path));
        }

        public static bool IsResourcesPath(string fullPath)
        {
            return !string.IsNullOrEmpty(fullPath) && RegexResources.IsMatch(fullPath);
        }

        public static string PathFromResources(string fullPath)
        {
            return Path.Combine(Path.GetDirectoryName(RegexResources.Replace(fullPath, "")) ?? string.Empty,
                Path.GetFileNameWithoutExtension(fullPath));
        }


        public Sprite GetSpriteFromAtlas(EResourceNames name, string spriteName)
        {
            if (_ways)
            {
                return GetSpriteFromAtlas(LoadAtlas(name), spriteName);
            }

            Debug.LogError("AssetsManager.GetPrefab Ways is null, only string path allowed");
            return null;
        }

        public Sprite GetSpriteFromAtlas(EResourceNames name, EResourceNames spriteName)
        {
            if (!_ways)
            {
                Debug.LogError("AssetsManager.GetPrefab Ways is null, only string path allowed");
                return null;
            }

            if (_ways.ResourcesPathways.TryGetValue(spriteName, out string path))
            {
                return GetSpriteFromAtlas(LoadAtlas(name), path);
            }

            return null;
        }

        private static Sprite GetSpriteFromAtlas(Sprite[] sprites, string spriteName)
        {
            return sprites?.FirstOrDefault(spriteAtlas => spriteAtlas.name.Equals(spriteName));
        }

        // if called with name, thinks that the path is internal, otherwise the path is a full path
        private Sprite[] LoadAtlas(EResourceNames name)
        {
            if (_atlasesCache.TryGetValue(name, out Sprite[] atlas))
            {
                return atlas;
            }

            Sprite[] sprites = GetAllResources<Sprite>(name);
            if (sprites is { Length: > 0 })
            {
                _atlasesCache.Add(name, sprites);
            }

            return sprites;
        }
    }
}