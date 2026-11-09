
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Managers
{
    public class SfxManager : MonoBehaviour, ISound
    {
        private const string SfxPath = "Audio/Sfx";

        private static Action<ESfxId> _onPlay;

        [SerializeField] private AudioClip[] _clips;

        private readonly Queue<AudioSource> _pool = new();
        private readonly Dictionary<LoopKey, AudioSource> _loops = new();
        
        private bool _canPlay;
        private SettingsSave _settings;
        

        public void Initialize(SettingsSave settings)
        {
            if (_settings != null)
            {
                _settings.OnSoundsChanged -= OnSoundsChanged;
            }

            _settings = settings;
            _canPlay = _settings.IsSoundsEnabled();
            _settings.OnSoundsChanged += OnSoundsChanged;
        }
        
        private void Awake()
        {
            WarmPool();

            _onPlay = Play;

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (_settings != null)
            {
                _settings.OnSoundsChanged -= OnSoundsChanged;
            }
        }

        public static void InvokePlay(ESfxId id) => _onPlay?.Invoke(id);

        public void Play(ESfxId id)
        {
            AudioClip clip = GetClip(id);
            Play(clip);
        }
        
        public AudioSource Play(AudioClip clip)
        {
            if (!_canPlay || !clip)
            {
                return null;
            }

            AudioSource source = GetPooledSource();
            source.clip = clip;
            source.loop = false;
            source.pitch = 1f;
            source.volume = 1f;
            source.Play();
            
            Routiner.Start(ReturnWhenDone(source));
            return source;
        }

        public void PlayLoop(ESfxId id, MonoBehaviour owner)
        {
            AudioClip clip = GetClip(id);
            if (!_canPlay || !clip)
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

        private AudioClip GetClip(ESfxId id)
        {
            int index = (byte)id;
            return _clips != null && index < _clips.Length ? _clips[index] : null;
        }

#if UNITY_EDITOR
        public int CacheClips()
        {
            int maxIndex = 0;
            foreach (ESfxId id in Enum.GetValues(typeof(ESfxId)))
            {
                if (id != ESfxId.None)
                {
                    maxIndex = Mathf.Max(maxIndex, (byte)id);
                }
            }

            _clips = new AudioClip[maxIndex + 1];
            int cachedCount = 0;
            foreach (AudioClip clip in Resources.LoadAll<AudioClip>(SfxPath))
            {
                if (Enum.TryParse(clip.name, out ESfxId id) && id != ESfxId.None)
                {
                    _clips[(byte)id] = clip;
                    cachedCount++;
                }
            }

            return cachedCount;
        }
#endif

        private void WarmPool()
        {
            foreach (var source in GetComponents<AudioSource>())
            {
                ReturnSource(source);
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
