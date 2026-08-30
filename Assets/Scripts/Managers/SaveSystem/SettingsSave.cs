
using System;
using UnityEngine;
using Utils;

namespace Managers
{
    public class SettingsSave
    {
        private const string LanguagePrefix = "Game.Language";
        private const string MusicKey = "Game.Music";
        private const string SoundsKey = "Game.Sounds";

        public event Action<bool> OnMusicChanged;
        public event Action<bool> OnSoundsChanged;

        public int GetLanguage()
        {
            return PlayerPrefs.GetInt(LanguagePrefix, 0);
        }

        public void SetLanguage(int language)
        {
            PlayerPrefs.SetInt(LanguagePrefix, language);
            PlayerPrefs.Save();
        }

        public bool IsMusicEnabled()
        {
            return PlayerPrefs.GetInt(MusicKey, 1) == 1;
        }

        public void SetMusicEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(MusicKey, enabled ? 1 : 0);
            PlayerPrefs.Save();

            OnMusicChanged.InvokeSafe(enabled);
        }

        public bool IsSoundsEnabled()
        {
            return PlayerPrefs.GetInt(SoundsKey, 1) == 1;
        }

        public void SetSoundsEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(SoundsKey, enabled ? 1 : 0);
            PlayerPrefs.Save();

            OnSoundsChanged.InvokeSafe(enabled);
        }
    }
}