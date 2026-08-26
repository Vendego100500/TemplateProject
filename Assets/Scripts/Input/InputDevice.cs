
using System;
using System.Collections.Generic;
using Input.Buttons;
using UnityEditor;
using UnityEngine.InputSystem;

namespace Input
{
    public class InputDevice : IDisposable
    {
        #region Player
        
        public Button Space { get; }

        #endregion

        private readonly InputActions _actions;
        private readonly IButton[] _buttons;

        private bool _disposed;

        public InputDevice()
        {
            _actions = new InputActions();
            _actions.Enable();
            _actions.LevelEditor.Disable();
            _actions.Player.Disable();

            var buttons = new List<IButton>(8);
            
            Space = Register(buttons, new Button(_actions.Player.Space));

            _buttons = buttons.ToArray();

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
        }

        public void Tick()
        {
            if (_disposed)
            {
                return;
            }

            foreach (var button in _buttons)
            {
                button.Tick();
            }
        }

        public void SetPlayerInputEnabled(bool isEnabled)
        {
            if (_disposed)
            {
                return;
            }

            if (isEnabled)
            {
                _actions.Player.Enable();
                return;
            }

            _actions.Player.Disable();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif
            
            foreach (var button in _buttons)
            {
                button.Dispose();
            }

            _actions.Disable();
            _actions.Dispose();
        }

#if UNITY_EDITOR
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Dispose();
            }
        }
#endif

        private static T Register<T>(List<IButton> list, T button) where T : IButton
        {
            list.Add(button);
            return button;
        }
    }
}
