
using System;
using UnityEngine;
using Utils;

namespace Managers
{
    public static class SaveSystem
    {
        private const string LanguageKey = "GaneName.Language";
        private const string MusicKey = "GaneName.Music";
        private const string SoundsKey = "GaneName.Sounds";

        public static event Action OnReset;
        public static event Action<bool> OnMusicChanged;
        public static event Action<bool> OnSoundsChanged;
        
        public static int GetLanguage()
        {
            return PlayerPrefs.GetInt(LanguageKey, 0);
        }

        public static void SetLanguage(int language)
        {
            PlayerPrefs.SetInt(LanguageKey, language);
            PlayerPrefs.Save();
        }

        public static bool IsMusicEnabled()
        {
            return PlayerPrefs.GetInt(MusicKey, 1) == 1;
        }

        public static void SetMusicEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(MusicKey, enabled ? 1 : 0);
            PlayerPrefs.Save();

            OnMusicChanged.InvokeSafe(enabled);
        }
        
        public static bool IsSoundsEnabled()
        {
            return PlayerPrefs.GetInt(SoundsKey, 1) == 1;
        }

        public static void SetSoundsEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(SoundsKey, enabled ? 1 : 0);
            PlayerPrefs.Save();

            OnSoundsChanged.InvokeSafe(enabled);
        }
        
        public static void Reset()
        {
            PlayerPrefs.DeleteAll();    //TODO: replace with PlayerPrefs.DeleteKey()

            PlayerPrefs.Save();
            OnReset.InvokeSafe();
        }
    }
}
