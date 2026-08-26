
using UnityEngine;
using UnityEngine.InputSystem;

namespace Input.Buttons
{
    public abstract class BaseButton<T> : IButton
    {
        protected readonly InputAction _action;

        protected float _pressTime;
        protected bool _pressed;

        public T State { get; protected set; }

        protected BaseButton(InputAction action)
        {
            _action = action;
            _action.started += OnPressed;
            _action.canceled += OnReleased;
            _pressTime = Time.time;
        }

        protected virtual void OnPressed(InputAction.CallbackContext context)
        {
            _pressed = true;
            _pressTime = Time.time;
        }

        protected virtual void OnReleased(InputAction.CallbackContext context)
        {
            _pressed = false;
        }

        public abstract void Tick();

        public virtual void Dispose()
        {
            _action.started -= OnPressed;
            _action.canceled -= OnReleased;
        }
    }
}
