
namespace Input
{
    public enum ButtonPhase : byte
    {
        Released = 0,
        Down = 1,
        Hold = 2,
        Pressed = 3,
    }
    
    public readonly struct ButtonState
    {
        public static readonly ButtonState DownState = new (ButtonPhase.Down);
        public static readonly ButtonState HoldState = new (ButtonPhase.Hold);
        public static readonly ButtonState PressedState = new (ButtonPhase.Pressed);
        public static readonly ButtonState ReleasedState = new (ButtonPhase.Released);
        
        private readonly ButtonPhase _phase;
        
        public bool IsDown => _phase == ButtonPhase.Down;
        public bool IsHold => _phase == ButtonPhase.Hold;
        public bool IsPressed => _phase != ButtonPhase.Released;
        public bool IsReleased => _phase == ButtonPhase.Released;
        

        private ButtonState(ButtonPhase phase)
        {
            _phase = phase;
        }

        public override string ToString()
        {
            return $"[{nameof(ButtonState)}]: {_phase}";
        }
    }
}