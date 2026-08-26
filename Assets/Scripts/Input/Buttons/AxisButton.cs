
using UnityEngine;
using UnityEngine.InputSystem;

namespace Input.Buttons
{
    public class AxisButton : BaseButton<Vector2>
    {
        public AxisButton(InputAction action) : base(action) { }

        protected override void OnReleased(InputAction.CallbackContext context)
        {
            base.OnReleased(context);

            State = Vector2.zero;
        }

        public override void Tick()
        {
            State = _action.ReadValue<Vector2>();
        }
    }

    public class InvertedAxisButton : AxisButton
    {
        public InvertedAxisButton(InputAction action) : base(action) { }

        public override void Tick()
        {
            base.Tick();
            
            State = new Vector2(-State.y, State.x);
        }
    }
}
