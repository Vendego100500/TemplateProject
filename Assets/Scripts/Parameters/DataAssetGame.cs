
using System;
using Managers;
using UnityEngine;

namespace Parameters
{
    [Serializable]
    public struct PoolObject : IPoolObjectParameters
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private int _count;
        [SerializeField, Min(1)] private int _steps;
        
        public GameObject Prefab => _prefab;
        public int Count => _count;
        public int Steps => _steps;
    }

    [CreateAssetMenu(fileName = "Game", menuName = "Scriptable Object/Parameters/Game", order = 0)]
    public class DataAssetGame : ScriptableObject
    {
        [SerializeField] private int _tickRate = 30;
        [SerializeField] private int _targetFrameRate = 60;
        [SerializeField] private AudioClip _music;
        [SerializeField] private PoolObject[] _poolObjects = Array.Empty<PoolObject>();
        
        public int TickRate => _tickRate;
        public int TargetFrameRate => _targetFrameRate;
        public AudioClip Music => _music;
        public PoolObject[] PoolObjects => _poolObjects;
    }
}