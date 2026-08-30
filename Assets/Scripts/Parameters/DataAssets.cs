
using UnityEngine;
using Utils;
using static AssetsSystem.AssetsManager;

namespace Parameters
{
    public class DataAssets : Singleton<DataAssets>, IDataCatalog
    {
        private const string ParametersPath = "Parameters/";

        public DataAssetGame Game { get; }
        
        
        private DataAssets()
        {
            Game = GetParameters<DataAssetGame>();
        }

        private T GetParameters<T>() where T : ScriptableObject
        {
            T obj = LoadAsset<T>(ParametersPath + typeof(T).Name);
            if (!obj)
            {
                Debug.LogError($"Failed to load {typeof(T).Name}. Make sure it exists in {ParametersPath}.");
            }

            return obj;
        }
    }
}