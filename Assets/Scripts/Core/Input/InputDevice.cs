
using System;
using System.Collections.Generic;
using Core.Input.Buttons;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Core.Input
{
    public class InputDevice : IDisposable
    {
        public Button LeftButton { get; }
        public Button RightButton { get; }
        public Button MiddleButton { get; }
        public AxisButton Move { get; }
        public AxisButton Camera { get; }

        private readonly InputActions _actions;
        private readonly IButton[] _buttons;

        private bool _disposed;

        public InputDevice()
        {
            _actions = new InputActions();
            _actions.Enable();

            var buttons = new List<IButton>(8);

            LeftButton = Register(buttons, new Button(_actions.Player.LeftClick));
            RightButton = Register(buttons, new Button(_actions.Player.RightClick));
            MiddleButton = Register(buttons, new Button(_actions.Player.MiddleClick));
            Move = Register(buttons, new AxisButton(_actions.Player.Move));
            Camera = Register(buttons, new AxisButton(_actions.Player.Camera, true));

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

            foreach (IButton button in _buttons)
            {
                button.Tick();
            }
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
            
            foreach (IButton button in _buttons)
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
