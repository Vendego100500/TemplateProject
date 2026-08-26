
using UnityEngine;

namespace Parameters
{
    [CreateAssetMenu(fileName = "GameParams", menuName = "Scriptable Object/Parameters/DataAssetGame", order = 51)]
    public class DataAssetGame : ScriptableObject
    {
        [SerializeField] private int _fps;
        
        public int Fps => _fps;
    }
}