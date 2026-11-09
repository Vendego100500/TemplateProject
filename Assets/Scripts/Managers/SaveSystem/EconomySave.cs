
using System;
using UnityEngine;
using Utils;

namespace Managers
{
    public class EconomySave
    {
        private const string CoinsKey = "Game.Coins";

        public event Action<int> OnCoinsChanged;


        public int GetCoins()
        {
            return PlayerPrefs.GetInt(CoinsKey, 0);
        }

        public void AddCoins(int amount)
        {
            int value = GetCoins() + amount;
            PlayerPrefs.SetInt(CoinsKey, value);
            PlayerPrefs.Save();

            OnCoinsChanged.InvokeSafe(value);
        }
    }
}
