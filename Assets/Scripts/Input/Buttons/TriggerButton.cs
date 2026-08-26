
using UnityEngine;
using UnityEngine.InputSystem;
using static Utils.Utils;

namespace Input.Buttons
{
    public class TriggerButton : Button
    {
        private const float PressThreshold = 0.7f;

        private float _value;

        public TriggerButton(InputAction action)
            : base(action)
        {
            _action.started -= OnPressed;
            _action.performed += OnPerformed;
        }

        private void OnPerformed(InputAction.CallbackContext context)
        {
            var prev = _value;
            _value = context.ReadValue<float>();

            if (Less(prev, _value) && GreaterOrEqual(_value, PressThreshold))
            {
                _pressed = true;
                _pressTime = Time.time;
            }
            else if (Greater(prev, _value) && Less(_value, PressThreshold))
            {
                _pressed = false;
            }
        }

        protected override void OnReleased(InputAction.CallbackContext context)
        {
            _value = 0;

            base.OnReleased(context);
        }

        public override void Dispose()
        {
            _action.performed -= OnPerformed;
            base.Dispose();
        }
    }
}
