
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Input.Buttons
{
    public class Button : BaseButton<ButtonState>
    {
        private const float SingleClickDeltaMs = 0.29f;

        protected bool _pressed;

        public Button(InputAction action)
            : base(action)
        {
        }

        protected override void OnPressed(InputAction.CallbackContext context)
        {
            base.OnPressed(context);

            _pressed = true;
            _pressTime = Time.time;
        }

        protected override void OnReleased(InputAction.CallbackContext context)
        {
            base.OnReleased(context);

            _pressed = false;
        }

        public override void Tick()
        {
            if (_pressed)
            {
                _pressed = false;
                State = ButtonState.PressedState;
                return;
            }

            if (_down)
            {
                float deltaTime = Time.time - _pressTime;
                State = deltaTime > SingleClickDeltaMs ? ButtonState.HoldState : ButtonState.DownedState;
                return;
            }

            State = ButtonState.ReleasedState;
        }
    }
}
