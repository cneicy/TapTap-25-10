using UnityEngine;

namespace Game.Buff
{
    public abstract class BuffBase 
    {
        public string BuffName;
        public float Duration;
        public bool IsExpired => _timeElapsed >= Duration;

        private float _timeElapsed;

        public virtual void OnApply(Player.Player target) { }
        public virtual void OnUpdate(Player.Player target, float deltaTime)
        {
            _timeElapsed += deltaTime;
        }
        public virtual void OnRemove(Player.Player target) { }
    }
}
