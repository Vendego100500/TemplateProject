
using System;
using System.Collections.Generic;
using AssetsSystem;
using Managers;
using UnityEngine;
using Utils;

namespace ViewSystem
{
    public class ViewManager : IViewManager
    {
        private readonly Transform _windowsRoot;
        private readonly Transform _popupsRoot;
        private readonly List<View> _views;
        private readonly List<WindowStackItem> _windowsStack;
        private readonly WindowTransition _transition;
        
        public event Action<Window> OnWindowChanged;
        public event Action<Popup> OnShowPopup;


        public ViewManager(Transform windowsRoot, Transform popupsRoot, ILocalization localization, TransitionRunner runner)
        {
            _views = new List<View>();
            _windowsStack = new List<WindowStackItem>();
            _transition = new WindowTransition(localization, runner);
            
            _windowsRoot = windowsRoot;
            _popupsRoot = popupsRoot;
        }

        public void Clean()
        {
            for (int i = _windowsRoot.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_windowsRoot.GetChild(i).gameObject);
            }
            
            for (int i = _popupsRoot.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_popupsRoot.GetChild(i).gameObject);
            }
        }

        public T ShowPopup<T>(EPrefabNames name, Action<T> preOpen = null)  where T : Popup
        {
            T popup = GetView<T>(name, _popupsRoot, out bool firstOpen);
            
            preOpen.InvokeSafe(popup);
            
            popup.Open(firstOpen ? EOpenType.Open : EOpenType.Reopen);
            
            OnShowPopup.InvokeSafe(popup);
            return popup;
        }

        public void OpenWindow(EPrefabNames name) => OpenWindow<Window>(name);

        public T OpenWindow<T>(EPrefabNames name, Action<T> preOpen = null) where T : Window
        {
            T window = GetView<T>(name, _windowsRoot, out bool firstOpen);
            
            preOpen.InvokeSafe(window);

            _transition.Play(() => OpenWindow(window, firstOpen));
            
            return window;
        }

        private T GetView<T>(EPrefabNames name, Transform root, out bool firstOpen) where T : View
        {
            T view = _views.Find(item => item is T && !item.IsActive && item.Prefab == name && item.enabled) as T;
            if (view)
            {
                firstOpen = false;
                return view;
            }

            view = AssetsManager.Instance.Instantiate<T>(name, root);
            if (!view)
            {
                throw new ArgumentException("Window not found: " + name);
            }
            
            view.gameObject.SetActive(false);
            _views.Add(view);

            firstOpen = true;
            return view;
        }

        private void OpenWindow<T>(T window, bool firstOpen) where T : Window
        {
            if (_windowsStack.Count == 0 || _windowsStack[^1].Instance != window)
            {
                SetupUIBeforeWindowOpen(window);

                _windowsStack.Add(new WindowStackItem { Name = window.Prefab, Instance = window });
            }

            window.Open(firstOpen ? EOpenType.Open : EOpenType.Reopen, WindowOnClose, _transition.Play);

            OnWindowChanged.InvokeSafe(window);
        }

        private void SetupUIBeforeWindowOpen(View window)
        {
            if (_windowsStack.Count > 0)
            {
                View lastWindow = _windowsStack[^1].Instance;
                if (lastWindow)
                {
                    lastWindow.Close(false);
                }
            }

            window.transform.SetAsLastSibling();
        }

        private void WindowOnClose(Window window, bool removeFromStack)
        {
            if (!removeFromStack)
            {
                OnWindowChanged.InvokeSafe(null);
                return;
            }

            int idx = _windowsStack.FindLastIndex(item => item.Name == window.Prefab);
            if (idx >= 0)
            {
                _windowsStack.RemoveAt(idx);
            }

            if (_windowsStack.Count <= 0)
            {
                OnWindowChanged.InvokeSafe(null);
                return;
            }

            Window lastWindow = _windowsStack[^1].Instance;
            OnWindowChanged.InvokeSafe(lastWindow);
            
            if (lastWindow && lastWindow.gameObject.activeSelf)
            {
                return;
            }

            _transition.Play(() =>
            {
                lastWindow.Open(EOpenType.Back);
            });
        }

        private class WindowStackItem
        {
            public Window Instance;
            public EPrefabNames Name;
        }
    }
}