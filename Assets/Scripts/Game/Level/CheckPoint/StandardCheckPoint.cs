using System;
using ShrinkEventBus;
using UnityEngine;

namespace Game.Level.CheckPoint
{
    [EventBusSubscriber]
    public class StandardCheckPoint : CheckPointBase
    {
        [EventSubscribe]
        public void OnTouchCheckPointEvent(TouchCheckPointEvent evt)
        {
            print(evt.HitBy.ToString()+evt.CheckPointBase.IsSpecial);
        }
    }
}