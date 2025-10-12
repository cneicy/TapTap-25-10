using ShrinkEventBus;
using UnityEngine;

namespace Game.Player
{
    public class ParachuteEvent : EventBase
    {
        public float Duration { get; set; }
        public float  SpeedDif { get; set; }
    }
}
