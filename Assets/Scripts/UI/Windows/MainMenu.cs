
using ViewSystem;

#if UNITY_EDITOR
using UnityEditor;
#else
using UnityEngine;
#endif

namespace UI.Windows
{
    public class MainMenu : View
    {
        public void OnStart()
        {
            //ArenaController.Instance.StartGame((eGameMod)gameMod, Game.PlayerManager.LastSelected, _nickNameInput.text);
            Close();
        }

        public void OnSettings()
        {
            //Game.WindowsManager.OpenWindow(ePrefabNames.SettingsWindow);
        }

        public void OnQuit()
        {
#if UNITY_EDITOR
            EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
        }
    }
}