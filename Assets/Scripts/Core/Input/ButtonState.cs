
namespace Core.Input
{
    public enum ButtonPhase : byte
    {
        Released = 0,
        Pressed = 1,
        Hold = 2,
        Downed = 3,
    }
    
    public readonly struct ButtonState
    {
        public static readonly ButtonState PressedState = new ButtonState(ButtonPhase.Pressed);
        public static readonly ButtonState HoldState = new ButtonState(ButtonPhase.Hold);
        public static readonly ButtonState DownedState = new ButtonState(ButtonPhase.Downed);
        public static readonly ButtonState ReleasedState = new ButtonState(ButtonPhase.Released);
        
        private readonly ButtonPhase _phase;
        
        public bool IsPress => _phase == ButtonPhase.Pressed;
        public bool IsHold => _phase == ButtonPhase.Hold;
        public bool IsDown => _phase is not ButtonPhase.Released;
        

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