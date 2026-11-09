
using System.Collections.Generic;
using Core;
using Managers;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ViewSystem;

namespace UI.Popups
{
    public class SettingsPopup : Popup
    {
        [SerializeField] private Toggle _musicToggle;
        [SerializeField] private Toggle _soundsToggle;
        [SerializeField] private MultiGraphicButton _resetButton;
        
        private PlayerSave _save;


        protected override void Awake()
        {
            base.Awake();
            
            _save = Game.Instance.Save;
        }

        protected override void ActivateWindow(EOpenType openType)
        {
            _musicToggle.SetIsOnWithoutNotify(!_save.Settings.IsMusicEnabled());
            _soundsToggle.SetIsOnWithoutNotify(!_save.Settings.IsSoundsEnabled());
            
            _resetButton.interactable = SceneManager.GetActiveScene().name == GameScenes.Main;

            base.ActivateWindow(openType);
        }

        public void OnSoundToggleClicked(bool isOn)
        {
            _save.Settings.SetSoundsEnabled(!isOn);
        }
        
        public void OnMusicToggleClicked(bool isOn)
        {
            _save.Settings.SetMusicEnabled(!isOn);
        }

        public void OnLanguageButtonClicked()
        {
            List<Locale> availableLocales = LocalizationSettings.AvailableLocales.Locales;
            int currentLanguage = _save.Settings.GetLanguage();
            if (++currentLanguage >= availableLocales.Count)
            {
                currentLanguage = 0;
            }

            LocalizationSettings.SelectedLocale = availableLocales[currentLanguage];
            _save.Settings.SetLanguage(currentLanguage);
        }

        public void OnResetButtonClicked()
        {
            _save.Reset();
        }

        public void OnRestoreButtonClicked()
        {
            
        }
        
        public void OnPrivacyButtonClicked()
        {
            //Application.OpenURL("https://herocraft.com/privacy/");
        }
    }
}