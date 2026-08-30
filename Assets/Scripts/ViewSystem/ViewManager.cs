
using System;
using System.Collections;
using System.Collections.Generic;
using AssetsSystem;
using UnityEngine;
using Utils;

namespace ViewSystem
{
    public class ViewManager : Singleton<ViewManager>
    {
        private const string HUDTag = "HUD";
        
        private readonly Transform _transform;
        private readonly List<View> _views;
        private readonly List<WindowStackItem> _windowsStack;
        
        public View Current { get; private set; }


        private ViewManager()
        {
            _views = new List<View>();
            _windowsStack = new List<WindowStackItem>();
            
            GameObject gameRoot = GameObject.FindWithTag(HUDTag);
            if (!gameRoot)
            {
                throw new ArgumentException("GameRoot not found");
            }

            Canvas canvas = gameRoot.GetComponentInChildren<Canvas>();
            if (!canvas)
            {
                throw new ArgumentException($"Canvas not found under GameObject with tag '{HUDTag}'");
            }

            _transform = canvas.transform;
            for (int i = _transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_transform.GetChild(i).gameObject);
            }
        }
        

        public IEnumerator WaitForLocalization()
        {
            yield return Current?.LocalizationTracker?.WaitForUpdate();
        }

        public void OpenInitView()
        {
            View window = GetWindow<View>(EPrefabNames.Init, out bool firstOpen);
            window.OnClose += WindowOnClose;
            OpenWindow(window, firstOpen);
        }

        public T ShowPopup<T>(EPrefabNames name, Action<T> preOpen = null)  where T : Popup
        {
            T window = GetWindow<T>(name, out bool firstOpen);
            preOpen.InvokeSafe(window);
            
            if (firstOpen)
            {
                window.Open();
            }
            else
            {
                window.ReOpen();
            }
            return window;
        }

        public View OpenWindow(EPrefabNames name)
        {
            return OpenWindow<View>(name);
        }

        public T OpenWindow<T>(EPrefabNames name) where T : View
        {
            T window = GetWindow<T>(name, out bool firstOpen);

            window.OnClose += WindowOnClose;

            ViewTransition.Play(() => OpenWindow(window, firstOpen));
            
            return window;
        }

        private T GetWindow<T>(EPrefabNames name, out bool firstOpen) where T : View
        {
            T window = _views.Find(item => item is T && !item.IsActive && item.Prefab == name && item.enabled) as T;
            if (window)
            {
                firstOpen = false;
                return window;
            }

            window = AssetsManager.Instance.Instantiate<T>(name, _transform.transform);
            if (!window)
            {
                throw new ArgumentException("Window not found: " + name);
            }
            
            window.gameObject.SetActive(false);
            _views.Add(window);

            firstOpen = true;
            return window;
        }

        private void OpenWindow<T>(T window, bool firstOpen) where T : View
        {
            SetupUIBeforeWindowOpen(window);

            if (window.IgnoreStack)
            {
                _windowsStack.Clear();
            }
            _windowsStack.Add(new WindowStackItem { Name = window.Prefab, Instance = window });

            if (firstOpen)
            {
                window.Open();
            }
            else
            {
                window.ReOpen();
            }

            Current = window;
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

        private void WindowOnClose(View obj, bool removeFromStack)
        {
            Current = null;
            if (!removeFromStack)
            {
                return;
            }

            int idx = _windowsStack.FindLastIndex(item => item.Instance == obj);
            if (idx >= 0)
            {
                _windowsStack.RemoveAt(idx);
            }

            if (_windowsStack.Count <= 0)
            {
                return;
            }

            Current = _windowsStack[^1].Instance;
            if (Current && Current.gameObject.activeSelf)
            {
                return;
            }

            ViewTransition.Play(() =>
            {
                Current.OnClose += WindowOnClose;
                Current.ReOpen();
            });
        }

        private class WindowStackItem
        {
            public View Instance;
            public EPrefabNames Name;
        }
    }
}