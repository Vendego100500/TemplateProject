
using UnityEngine;

namespace Managers
{
    public class SoundManager : ISound, IMusic
    {
        private readonly SfxManager _sfxManager;
        private readonly MusicManager _musicManager;
        
        public SoundManager(SfxManager sfxManager, MusicManager musicManager, SettingsSave settings)
        {
            _sfxManager = sfxManager;
            _sfxManager.Initialize(settings);
            
            _musicManager = musicManager;
            _musicManager.Initialize(settings);
        }

        void ISound.Play(ESfxId id) => _sfxManager.Play(id);
        AudioSource ISound.Play(AudioClip clip) => _sfxManager.Play(clip);
        void ISound.PlayLoop(ESfxId id, MonoBehaviour owner) => _sfxManager.PlayLoop(id, owner);
        void ISound.StopLoop(ESfxId id, MonoBehaviour owner) => _sfxManager.StopLoop(id, owner);
        
        void IMusic.Play(AudioClip clip) => _musicManager.Play(clip);
    }
}
