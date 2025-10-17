using Data;
using ShrinkEventBus;
using UnityEngine;

namespace Game.Level.CheckPoint
{
    public enum HitBy
    {
        Player, Ammo
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
        
        private float _lastTriggerTime = -999f;
        
        [SerializeField] private float triggerCooldown = 2f;

        public virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (Time.time - _lastTriggerTime < triggerCooldown)
                return;
            if (other.CompareTag("Player"))
            {
                EventBus.TriggerEvent(new TouchCheckPointEvent(HitBy.Player, this));
                _lastTriggerTime = Time.time;
            }
            else if (other.CompareTag("Ammo"))
            {
                EventBus.TriggerEvent(new TouchCheckPointEvent(HitBy.Ammo, this));
                _lastTriggerTime = Time.time;
            }

            DataManager.Instance.ForceSave();
        }
    }
}