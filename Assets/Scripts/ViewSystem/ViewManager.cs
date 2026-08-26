
using System;
using System.Collections.Generic;
using AssetsSystem;
using UnityEngine;
using Utils;
using Object = UnityEngine.Object;

namespace ViewSystem
{
    public class ViewManager : Singleton<ViewManager>
    {
        private const string GameRootTag = "GameRoot";
        
        private readonly List<View> _views;
        private readonly List<WindowStackItem> _windowsStack;

        public Transform Root { get; }
        public View Current { get; private set; }


        private ViewManager()
        {
            _views = new List<View>();
            _windowsStack = new List<WindowStackItem>();
            
            GameObject gameRoot = GameObject.FindWithTag(GameRootTag);
            if (!gameRoot)
            {
                throw new ArgumentException("GameRoot not found: " + gameRoot.name);
            }

            Root = gameRoot.GetComponentInChildren<Canvas>().transform;
        }
        

        public View OpenWindow(EPrefabNames name)
        {
            return OpenWindow<View>(name);
        }

        public T OpenWindow<T>(EPrefabNames name) where T : View
        {
            bool firstOpen = false;
            T window = _views.Find(item => item is T && !item.IsActive && item.Prefab == name && item.enabled) as T;

            if (!window)
            {
                firstOpen = true;
                window = AssetsManager.Instance.Instantiate<T>(name, Root.transform);
                if (!window)
                {
                    Debug.LogError("window not found: " + name);
                    return null;
                }

                _views.Add(window);
            }

            window.OnClose += WindowOnClose;

            SetupUIBeforeWindowOpen(window);

            if (window.IgnoreStack)
            {
                _windowsStack.Clear();
            }
            _windowsStack.Add(new WindowStackItem { Name = name, Instance = window });

            if (firstOpen)
            {
                window.Open();
            }
            else
            {
                window.ReOpen();
            }

            Current = window;
            return window;
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
            _windowsStack.RemoveAt(idx);

            if (_windowsStack.Count <= 0)
            {
                return;
            }

            Current = _windowsStack[^1].Instance;
            if (Current && Current.gameObject.activeSelf)
            {
                return;
            }

            Current.ReOpen();
        }

        public void OnSceneClose()
        {
            CloseAll();

            _views.Clear();

            Transform root = Root;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(root.GetChild(i).gameObject);
            }
        }

        public void CloseAll()
        {
            for (int i = _windowsStack.Count - 1; i >= 0; i--)
            {
                _windowsStack[i].Instance.Close();
            }

            _windowsStack.Clear();
        }


        private class WindowStackItem
        {
            public View Instance;
            public EPrefabNames Name;
        }
    }
}