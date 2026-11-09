
using System;
using System.Collections.Generic;
using AssetsSystem;
using DG.Tweening;
using Managers;
using UnityEngine;
using Utils;
using Random = UnityEngine.Random;

namespace UI
{
    public class CoinRewardAnimator : MonoBehaviour
    {
        private const int DefaultVisualCoinCount = 10;
        private const float FlightDuration = 1f;
        private const float Stagger = 0.1f;

        [SerializeField] private RectTransform _rewardCoinIcon;
        
        private readonly List<PoolComponent> _coins = new();

        private CoinBalance _coinBalance;
        private bool _isPlaying;
        private int _startBalance;
        private Action _onComplete;

        public void PrepareDisplay(int startBalance, CoinBalance coinBalance)
        {
            _coinBalance = coinBalance;
            _coinBalance.SetIgnoreCoinsChanged(true);
            _coinBalance.SetDisplay(startBalance);
        }

        public void Play(int rewardAmount, int startBalance, Action onComplete = null)
        {
            if (_isPlaying)
            {
                return;
            }

            if (rewardAmount <= 0)
            {
                onComplete?.Invoke();
                return;
            }

            _isPlaying = true;
            _startBalance = startBalance;
            _onComplete = onComplete;
            
            RectTransform target = _coinBalance.CoinAnchor;

            int arrived = 0;
            int distributed = 0;
            int visualCount = Mathf.Min(DefaultVisualCoinCount, rewardAmount);

            PoolComponent coinPrefab = AssetsManager.Instance.GetPrefab<PoolComponent>(EPrefabNames.Coin);
            for (int i = 0; i < visualCount; i++)
            {
                int chunk = GetCoinChunk(rewardAmount, visualCount, i);
                float delay = i * Stagger;
                DOVirtual.DelayedCall(delay, () =>
                {
                    if (!_isPlaying)
                    {
                        return;
                    }

                    PoolComponent coin = PoolManager.Instance.Get(coinPrefab, transform);
                    _coins.Add(coin);

                    Vector3 from = GetWorldCenter(_rewardCoinIcon);
                    Vector3 to = GetWorldCenter(target);
                    coin.transform.position = from + (Vector3)(Random.insideUnitCircle * 12f);
                    coin.transform
                        .DOMove(to, FlightDuration)
                        .SetEase(Ease.InQuad)
                        .SetTarget(gameObject)
                        .OnComplete(() =>
                        {
                            coin.BackToPool();
                            FinishCoin(chunk);
                            _coins.Remove(coin);
                        });
                }).SetTarget(gameObject);
            }
            
            return;

            void FinishCoin(int chunk)
            {
                if (!_isPlaying)
                {
                    return;
                }

                arrived++;
                distributed += chunk;
                _coinBalance.SetDisplay(_startBalance + distributed);

                if (arrived < visualCount)
                {
                    return;
                }

                FinishRewardAnimation();
            }
        }

        public void StopAndSyncDisplay()
        {
            if (!_isPlaying)
            {
                return;
            }
            
            foreach (var coin in _coins)
            {
                coin.BackToPool();
            }
            _coins.Clear();

            _onComplete = null;
            DOTween.Kill(gameObject);
            FinishRewardAnimation();
        }

        private void FinishRewardAnimation()
        {
            _isPlaying = false;
            _coinBalance.SetIgnoreCoinsChanged(false);
            _coinBalance.Refresh();

            _onComplete.InvokeSafe();
            _onComplete = null;
        }

        private void OnDisable()
        {
            StopAndSyncDisplay();
        }

        private static Vector3 GetWorldCenter(RectTransform rect)
        {
            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return (corners[0] + corners[2]) * 0.5f;
        }

        private static int GetCoinChunk(int totalReward, int visualCount, int index)
        {
            int baseChunk = totalReward / visualCount;
            int remainder = totalReward % visualCount;
            return baseChunk + (index < remainder ? 1 : 0);
        }
    }
}
