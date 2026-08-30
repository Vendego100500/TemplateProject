
using UnityEngine;

namespace Parameters
{
    [CreateAssetMenu(fileName = "Game", menuName = "Scriptable Object/Parameters/Game", order = 0)]
    public class DataAssetGame : ScriptableObject
    {
        [SerializeField] private int _fps;
        [SerializeField] private AudioClip _music;
        
        public int Fps => _fps;
        public AudioClip Music => _music;
    }
}