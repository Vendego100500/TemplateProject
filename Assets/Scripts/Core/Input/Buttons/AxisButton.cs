
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Input.Buttons
{
    public class AxisButton : BaseButton<Vector2>
    {
        private readonly bool _inverted;

        public AxisButton(InputAction action, bool inverted = false)
            : base(action)
        {
            _inverted = inverted;
        }

        protected override void OnReleased(InputAction.CallbackContext context)
        {
            base.OnReleased(context);

            State = Vector2.zero;
        }

        public override void Tick()
        {
            State = _action.ReadValue<Vector2>();
            if (_inverted)
            {
                State.Set(-State.y, State.x);
            }
        }
    }
}
