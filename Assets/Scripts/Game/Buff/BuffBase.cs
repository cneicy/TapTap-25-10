using UnityEngine;

namespace Game.Buff
{
    public abstract class BuffBase
    {
        public string BuffName;
        public float Duration;
        public bool IsExpired => _timeElapsed >= Duration;

        private float _timeElapsed;
        public int StackCount { get; private set; } = 0;
        protected virtual int MaxStacks => 1; // 默认最大叠加层数

        public virtual void AddStack()
        {
            StackCount = Mathf.Min(StackCount + 1, MaxStacks);
            OnStackChanged();
        }

        // 可被子类重写：根据层数调整数值
        protected virtual void OnStackChanged() { }

        public virtual void OnApply(Player.Player target) { }

        public virtual void OnUpdate(Player.Player target, float deltaTime)
        {
            _timeElapsed += deltaTime;
        }

        public virtual void OnRemove(Player.Player target) { }

        // 重新计时（当Buff叠加时刷新持续时间）
        public void RefreshDuration()
        {
            _timeElapsed = 0f;
        }
    }
}