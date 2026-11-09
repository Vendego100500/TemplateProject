
using System;
using UnityEngine;
using Utils;

namespace Managers
{
    public class PlayerSave
    {
        public ProgressSave Progress { get; }
        public EconomySave Economy { get; }
        public SettingsSave Settings { get; }

        public event Action OnReset;

        
        public PlayerSave()
        {
            Progress = new ProgressSave();
            Economy = new EconomySave();
            Settings = new SettingsSave();
        }

        public void Reset()
        {
#if UNITY_EDITOR || DEVELOPMENT
            PlayerPrefs.DeleteAll();
#else
            Progress.Clear();
#endif
            PlayerPrefs.Save();
            OnReset.InvokeSafe();
        }
    }
}
