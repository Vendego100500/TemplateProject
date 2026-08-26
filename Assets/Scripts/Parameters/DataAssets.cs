
using System.Collections.Generic;
using UnityEngine;
using Utils;
using static AssetsSystem.AssetsManager;

namespace Parameters
{
    public class DataAssets : Singleton<DataAssets>
    {
        private const string ParametersPath = "Parameters/";
        
        public readonly List<ScriptableObject> Parameters;
        
        public readonly DataAssetGame Game;
        
        private DataAssets()
        {
            Parameters =  new List<ScriptableObject>();
            
            Game = GetParameters<DataAssetGame>();
        }

        private T GetParameters<T>() where T : ScriptableObject
        {
            T obj = LoadAsset<T>(ParametersPath + typeof(T).Name);
            if (!obj)
            {
                Debug.LogError($"Failed to load {typeof(T).Name}. Make sure it exists in {ParametersPath}.");
            }
            
            Parameters.Add(obj);

            return obj;
        }
    }
}