
using UnityEngine;

namespace Parameters
{
    [CreateAssetMenu(fileName = "GameParams", menuName = "Scriptable Object/Parameters/DataAssetGame", order = 51)]
    public class DataAssetGame : ScriptableObject
    {
        [SerializeField] private int _fps;
        [SerializeField] private AudioClip _music;
        
        public int Fps => _fps;
        public AudioClip Music => _music;
    }
}