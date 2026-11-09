
using UnityEngine;

namespace Managers
{
    public interface ISound
    {
        void Play(ESfxId id);
        AudioSource Play(AudioClip clip);
        void PlayLoop(ESfxId id, MonoBehaviour owner);
        void StopLoop(ESfxId id, MonoBehaviour owner);
    }
}