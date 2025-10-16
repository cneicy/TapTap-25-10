using ShrinkEventBus;

namespace Game.Level.CheckPoint
{
    [EventBusSubscriber]
    public class SpecialCheckPoint : CheckPointBase
    {
        private void Awake()
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