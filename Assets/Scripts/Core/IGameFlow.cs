
using System;

namespace Core
{
    public interface IGameFlow
    {
        void ToMain(Action onCovered = null, Action onLoaded = null);
        void ToLevel(Action onCovered = null, Action onLoaded = null);
        void OpenMainMenu();
        void ShowSettings();
    }
}
