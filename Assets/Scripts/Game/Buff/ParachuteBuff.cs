using Game.Player;
using UnityEngine;

namespace Game.Buff
{
    public class ParachuteBuff : BuffBase
    {
        private PlayerController _pc;
        private Rigidbody2D _rb;
        private float _oldGravityScale;

        // 期望为正数幅度，内部用负号表示向下
        public float ParachuteMinSpeed { get; }
        public float ParachuteFallAcceleration { get; }

        protected override int MaxStacks => 3;

        public ParachuteBuff(float duration, float minSpeed, float accel)
        {
            BuffName = "Parachute";
            Duration = duration;
            ParachuteMinSpeed = Mathf.Abs(minSpeed);
            ParachuteFallAcceleration = accel;
        }

        public override void OnApply(Player.Player target)
        {
            base.OnApply(target);

            _pc = target.GetComponent<PlayerController>();
            _rb = target.GetComponent<Rigidbody2D>();

            if (_pc == null || _rb == null)
            {
                Debug.LogWarning("[ParachuteBuff] 找不到 PlayerController 或 Rigidbody2D。");
                return;
            }

            if (StackCount == 0) AddStack();

            // 关闭控制器重力逻辑 + 物理重力设 0
            _pc.HandleGravityByController = false;
            _oldGravityScale = _rb.gravityScale;
            _rb.gravityScale = 0f;

            // 降低跳跃与横向移动（保留这些体验）
            _pc.JumpPowerRate = 1f;      
            _pc.HorizontalPowerRate = 0.5f; 
            
            //标记是否在使用
            _pc.IsParachute = true;
        }

        public override void OnUpdate(Player.Player target, float dt)
        {
            base.OnUpdate(target, dt);
            if (_rb == null || _pc == null) return;

            // 叠层决定目标下落速度（负号向下）
            float targetY = StackCount switch
            {
                <= 1 => -ParachuteMinSpeed,          
                2    => ParachuteMinSpeed * 0.5f,   
                _    => ParachuteMinSpeed * 1.5f    
            };

            Vector2 v = _rb.linearVelocity;
            float newY = Mathf.MoveTowards(v.y, targetY, ParachuteFallAcceleration * dt);

            if (Mathf.Abs(newY - targetY) <= 0.05f) newY = targetY;

            v.y = newY;
            _rb.linearVelocity = v;
            _pc._frameVelocity = v; // 同步缓存
        }

        protected override void OnStackChanged()
        {
            Debug.Log($"[Parachute] 当前层数: {StackCount}");
        }

        public override void OnRemove(Player.Player target)
        {
            base.OnRemove(target);

            if (_pc != null)
            {
                _pc.HandleGravityByController = true;
                _pc.VerticalSpeed = _pc._stats.MaxFallSpeed;
                _pc.JumpPowerRate = 1f;
                _pc.HorizontalPowerRate = 1f;
            }

            if (_rb != null)
            {
                _rb.gravityScale = _oldGravityScale;
            }
            
            _pc.IsParachute = false;
        }
    }
}
