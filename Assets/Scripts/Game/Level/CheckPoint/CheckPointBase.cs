using ShrinkEventBus;
using UnityEngine;

namespace Game.Level.CheckPoint
{
    public enum HitBy
    {
        Player,Ammo
    }
    public class TouchCheckPointEvent : EventBase
    {
        public HitBy HitBy { get; }
        public CheckPointBase CheckPointBase { get; }

        public TouchCheckPointEvent(HitBy hitBy, CheckPointBase checkPointBase)
        {
            HitBy = hitBy;
            CheckPointBase = checkPointBase;
        }
    }
    public abstract class CheckPointBase : MonoBehaviour
    {
        public bool IsSpecial { get; set; }

        public virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                EventBus.TriggerEvent(new TouchCheckPointEvent(HitBy.Player,this));
            }

            if (other.CompareTag("Ammo"))
            {
                EventBus.TriggerEvent(new TouchCheckPointEvent(HitBy.Ammo, this));
            }
        }
    }
}