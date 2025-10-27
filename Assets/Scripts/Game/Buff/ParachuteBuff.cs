using Game.Item;
using Game.Player;
using UnityEngine;

namespace Game.Buff
{
    public class ParachuteBuff : BuffBase
    {
        private PlayerController _pc;
        private Rigidbody2D _rb;
        private float _oldGravityScale;

        // 期望为正数幅度（内部统一用负号表示向下）
        public float ParachuteMinSpeed { get; }
        public float ParachuteFallAcceleration { get; }

        // 允许最多3层
        protected override int MaxStacks => 3;

        public ParachuteBuff(float duration, float minSpeed, float accel)
        {
            BuffName = "Parachute";
            Duration = duration;
            ParachuteMinSpeed = Mathf.Abs(minSpeed);
            ParachuteFallAcceleration = Mathf.Abs(accel);
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
            
            _pc.HandleGravityByController = false;
            _oldGravityScale = _rb.gravityScale;
            _rb.gravityScale = 0f;
            
            _pc.JumpPowerRate = 1f;
            _pc.HorizontalPowerRate = 0.5f;

            _pc.IsParachute = true;
        }

        public override void OnUpdate(Player.Player target, float dt)
        {
            base.OnUpdate(target, dt);
            if (_rb == null || _pc == null) return;
            
            float targetY = StackCount switch
            {
                <= 1 => -ParachuteMinSpeed,            
                2    => ParachuteMinSpeed * 0.75f,     
                _    => ParachuteMinSpeed * 1.5f      
            };

            // 用 MoveTowards 将竖直速度逼近目标
            var v = _rb.linearVelocity;
            float newY = Mathf.MoveTowards(v.y, targetY, ParachuteFallAcceleration * dt);
            if (Mathf.Abs(newY - targetY) <= 0.05f) newY = targetY;
            
            v.y = newY;
            _rb.linearVelocity = v;
            
            var fv = _pc._frameVelocity;
            fv.y = newY;
            _pc._frameVelocity = fv;
        }

        protected override void OnStackChanged()
        {
            Debug.Log($"[Parachute] 当前层数: {StackCount}");
        }

        public override void OnRemove(Player.Player target)
        {
            base.OnRemove(target);

            // 还原玩家控制参数
            if (_pc != null)
            {
                _pc.HandleGravityByController = true;
                _pc.VerticalSpeed = _pc._stats.MaxFallSpeed;
                _pc.JumpPowerRate = 1f;
                _pc.HorizontalPowerRate = 1f;
                _pc.IsParachute = false;
            }

            if (_rb != null)
                _rb.gravityScale = _oldGravityScale;

            // 清理场景中的伞实体
            if (_pc != null)
            {
                ParachuteUtils.DestroyAllParachutesUnder(_pc.transform);
            }
        }
    }
}
