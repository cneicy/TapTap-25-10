using ShrinkEventBus;

namespace Game.Buff
{
    public class BuffAppliedEvent : EventBase 
    {
        public BuffBase Buff { get; set; }
        public Player.Player Player { get; set; }
    }
}
