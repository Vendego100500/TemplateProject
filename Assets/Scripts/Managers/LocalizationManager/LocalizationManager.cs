
using System.Collections;
using ViewSystem;

namespace Managers
{
    public class LocalizationManager : ILocalization
    {
        private View _currentView;


        public LocalizationManager(SettingsSave settings)
        {
            settings.OnLanguageChanged += ForceUpdate;
        }
        

        public void OnWindowChanged(View view)
        {
            _currentView = view;
        }
        
        public IEnumerator WaitForLocalization()
        {
            yield return _currentView?.LocalizationTracker?.WaitForUpdate();
        }

        private void ForceUpdate()
        {
            if (_currentView?.LocalizationTracker != null)
            {
                Routiner.Start(_currentView.LocalizationTracker.ForceUpdate());
            }
        }
    }
}
