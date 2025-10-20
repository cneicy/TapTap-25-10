using ShrinkEventBus;

namespace Game.Level.CheckPoint
{
    [EventBusSubscriber]
    public class SpecialCheckPoint : CheckPointBase
    {
        public SpecialCheckPoint()
        {
            IsSpecial = true;
        }

        [EventSubscribe]
        public void OnTouchCheckPointEvent(TouchCheckPointEvent evt)
        {
            print(evt.HitBy.ToString()+evt.CheckPointBase.IsSpecial);
        }
    }
}