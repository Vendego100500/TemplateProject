
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ViewSystem
{
    public class LocalizationTracker
    {
        private const float WaitTimeout = 2f;

        private readonly LocalizeStringEvent[] _stringLocalizers;
        private readonly LocalizeSpriteEvent[] _spriteLocalizers;
        private readonly Dictionary<LocalizeSpriteEvent, LocalizedAsset<Sprite>.ChangeHandler> _spriteAssetListeners = new();
        private readonly HashSet<LocalizeStringEvent> _pendingStrings = new();
        private readonly HashSet<LocalizeSpriteEvent> _pendingSprites = new();

        private int _updateVersion;

        public LocalizationTracker(View view)
        {
            _stringLocalizers = view.GetComponentsInChildren<LocalizeStringEvent>(true);
            _spriteLocalizers = view.GetComponentsInChildren<LocalizeSpriteEvent>(true);

            foreach (LocalizeStringEvent localizer in _stringLocalizers)
            {
                localizer.OnUpdateString.AddListener(_ => _pendingStrings.Remove(localizer));
            }

            foreach (LocalizeSpriteEvent localizer in _spriteLocalizers)
            {
                localizer.OnUpdateAsset.AddListener(_ => _pendingSprites.Remove(localizer));
                _spriteAssetListeners.Add(localizer, _ => _pendingSprites.Remove(localizer));
            }
        }

        public IEnumerator WaitForUpdate()
        {
            yield return WaitForUpdate(false);
        }

        public IEnumerator ForceUpdate()
        {
            yield return WaitForUpdate(true);
        }

        private IEnumerator WaitForUpdate(bool includeInactive)
        {
            int updateVersion = ++_updateVersion;

            _pendingStrings.Clear();
            _pendingSprites.Clear();

            AddPendingStringLocalizers(includeInactive);
            AddPendingSpriteLocalizers(includeInactive);

            if (_pendingStrings.Count == 0 && _pendingSprites.Count == 0)
            {
                yield break;
            }

            if (includeInactive)
            {
                ForceRefreshStrings(updateVersion);
                ForceLoadSprites(updateVersion);
            }
            else
            {
                RefreshStrings();
                SubscribeSpriteAssetChanges();
            }

            float timeoutAt = Time.realtimeSinceStartup + WaitTimeout;
            yield return new WaitUntil(() => IsReady || Time.realtimeSinceStartup >= timeoutAt);

            if (!includeInactive)
            {
                UnsubscribeSpriteAssetChanges();
            }

            if (!IsReady)
            {
                _pendingStrings.Clear();
                _pendingSprites.Clear();
            }

            yield return null;
        }

        private bool IsReady => _pendingStrings.Count == 0 && _pendingSprites.Count == 0;
        

        private void AddPendingStringLocalizers(bool includeInactive)
        {
            foreach (LocalizeStringEvent localizer in _stringLocalizers)
            {
                if (!localizer || (!includeInactive && !localizer.isActiveAndEnabled) || localizer.StringReference.IsEmpty)
                {
                    continue;
                }

                _pendingStrings.Add(localizer);
            }
        }

        private void AddPendingSpriteLocalizers(bool includeInactive)
        {
            foreach (LocalizeSpriteEvent localizer in _spriteLocalizers)
            {
                if (!localizer || (!includeInactive && !localizer.isActiveAndEnabled) || localizer.AssetReference.IsEmpty)
                {
                    continue;
                }

                _pendingSprites.Add(localizer);
            }
        }

        private void RefreshStrings()
        {
            foreach (LocalizeStringEvent localizer in _stringLocalizers)
            {
                if (_pendingStrings.Contains(localizer))
                {
                    localizer.RefreshString();
                }
            }
        }

        private void ForceRefreshStrings(int updateVersion)
        {
            foreach (LocalizeStringEvent localizer in _stringLocalizers)
            {
                if (!_pendingStrings.Contains(localizer))
                {
                    continue;
                }

                AsyncOperationHandle<string> operation = localizer.StringReference.GetLocalizedStringAsync();
                if (operation.IsDone)
                {
                    ApplyString(localizer, operation.Result, updateVersion);
                    continue;
                }

                operation.Completed += handle => ApplyString(localizer, handle.Result, updateVersion);
            }
        }

        private void ForceLoadSprites(int updateVersion)
        {
            foreach (LocalizeSpriteEvent localizer in _spriteLocalizers)
            {
                if (!_pendingSprites.Contains(localizer))
                {
                    continue;
                }

                AsyncOperationHandle<Sprite> operation = localizer.AssetReference.LoadAssetAsync();
                if (operation.IsDone)
                {
                    ApplySprite(localizer, operation.Result, updateVersion);
                    continue;
                }

                operation.Completed += handle => ApplySprite(localizer, handle.Result, updateVersion);
            }
        }

        private void ApplyString(LocalizeStringEvent localizer, string value, int updateVersion)
        {
            if (updateVersion != _updateVersion)
            {
                return;
            }

            if (!localizer)
            {
                _pendingStrings.Remove(localizer);
                return;
            }

            localizer.OnUpdateString.Invoke(value);
            _pendingStrings.Remove(localizer);
        }

        private void ApplySprite(LocalizeSpriteEvent localizer, Sprite value, int updateVersion)
        {
            if (updateVersion != _updateVersion)
            {
                return;
            }

            if (!localizer)
            {
                _pendingSprites.Remove(localizer);
                return;
            }

            localizer.OnUpdateAsset.Invoke(value);
            _pendingSprites.Remove(localizer);
        }

        private void SubscribeSpriteAssetChanges()
        {
            foreach (LocalizeSpriteEvent localizer in _spriteLocalizers)
            {
                if (!_pendingSprites.Contains(localizer))
                {
                    continue;
                }

                localizer.AssetReference.AssetChanged += _spriteAssetListeners[localizer];
            }
        }

        private void UnsubscribeSpriteAssetChanges()
        {
            foreach (KeyValuePair<LocalizeSpriteEvent, LocalizedAsset<Sprite>.ChangeHandler> listener in _spriteAssetListeners)
            {
                if (listener.Key)
                {
                    listener.Key.AssetReference.AssetChanged -= listener.Value;
                }
            }
        }
    }
}
