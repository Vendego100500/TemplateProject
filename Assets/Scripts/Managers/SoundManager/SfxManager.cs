
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Utils;

namespace Managers
{
    public class SfxManager : MonoBehaviourSingleton<SfxManager>
    {
        private const int PoolSize = 10;
        private const string SfxPath = "Audio/Sfx";

        private readonly Dictionary<ESfxId, AudioClip> _clips = new();
        private readonly Queue<AudioSource> _pool = new();
        private readonly Dictionary<LoopKey, AudioSource> _loops = new();
        
        private bool _canPlay;
        private SettingsSave _settings;
        

        public void Bind(SettingsSave settings)
        {
            if (_settings != null)
            {
                _settings.OnSoundsChanged -= OnSoundsChanged;
            }

            _settings = settings;
            _canPlay = _settings.IsSoundsEnabled();
            _settings.OnSoundsChanged += OnSoundsChanged;
        }
        
        protected override void Awake()
        {
            base.Awake();
            
            LoadClips();
            WarmPool();

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        protected override void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (_settings != null)
            {
                _settings.OnSoundsChanged -= OnSoundsChanged;
            }
            
            base.OnDestroy();
        }

        public void Play(ESfxId id)
        {
            if (!_canPlay || !_clips.TryGetValue(id, out AudioClip clip))
            {
                return;
            }

            AudioSource source = GetPooledSource();
            source.clip = clip;
            source.loop = false;
            source.pitch = 1f;
            source.volume = 1f;
            source.Play();
            
            Routiner.Start(ReturnWhenDone(source));
        }

        public void PlayDeath(ESfxId sound)
        {
            if (_canPlay)
            {
                Play(sound);
            }
        }

        public void PlayLoop(ESfxId id, MonoBehaviour owner)
        {
            if (!_canPlay || !_clips.TryGetValue(id, out AudioClip clip))
            {
                return;
            }

            LoopKey key = new(id, owner);
            if (_loops.TryGetValue(key, out AudioSource existing) && existing.isPlaying)
            {
                return;
            }

            AudioSource source = existing ?? CreateLoopSource();
            source.clip = clip;
            source.loop = true;
            source.pitch = 1f;
            source.volume = 1f;
            source.Play();
            
            _loops[key] = source;
        }

        public void StopLoop(ESfxId id, MonoBehaviour owner)
        {
            LoopKey key = new(id, owner);
            if (!_loops.TryGetValue(key, out AudioSource source))
            {
                return;
            }

            source.Stop();
            ReturnSource(source);
            
            _loops.Remove(key);
        }

        private void StopAllLoops()
        {
            foreach (var pair in _loops)
            {
                pair.Value.Stop();
                ReturnSource(pair.Value);
            }

            _loops.Clear();
        }

        private void LoadClips()
        {
            foreach (AudioClip clip in Resources.LoadAll<AudioClip>($"{SfxPath}"))
            {
                if (Enum.TryParse(clip.name, out ESfxId id))
                {
                    _clips[id] = clip;
                }
            }
        }

        private void WarmPool()
        {
            for (int i = 0; i < PoolSize; i++)
            {
                ReturnSource(CreateLoopSource());
            }
        }

        private AudioSource CreateLoopSource()
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        private AudioSource GetPooledSource()
        {
            return _pool.Count > 0 ? _pool.Dequeue() : CreateLoopSource();
        }

        private void ReturnSource(AudioSource source)
        {
            source.clip = null;
            source.loop = false;
            _pool.Enqueue(source);
        }

        private IEnumerator ReturnWhenDone(AudioSource source)
        {
            yield return new WaitWhile(() => source && source.isPlaying);
            if (source)
            {
                ReturnSource(source);
            }
        }

        private void OnSoundsChanged(bool value)
        {
            _canPlay = value;
            if (!value)
            {
                StopAllLoops();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => StopAllLoops();

        private readonly struct LoopKey : IEquatable<LoopKey>
        {
            private readonly ESfxId _id;
            private readonly MonoBehaviour _owner;

            public LoopKey(ESfxId id, MonoBehaviour owner)
            {
                _id = id;
                _owner = owner;
            }

            public bool Equals(LoopKey other) => _id == other._id && _owner == other._owner;
            public override bool Equals(object obj) => obj is LoopKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(_id, _owner);
        }
    }
}
