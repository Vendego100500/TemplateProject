
using System;

namespace ViewSystem
{
    public interface IViewManager
    {
        event Action<Window> OnWindowChanged;
        event Action<Popup> OnShowPopup;
        void Clean();
        T ShowPopup<T>(EPrefabNames name, Action<T> preOpen = null) where T : Popup;
        void OpenWindow(EPrefabNames name) => OpenWindow<Window>(name);
        T OpenWindow<T>(EPrefabNames name, Action<T> preOpen = null) where T : Window;
    }
}