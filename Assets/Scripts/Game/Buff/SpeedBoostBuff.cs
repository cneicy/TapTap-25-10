using UnityEngine;
using Game.Player;
using Game.Mechanism; // 用到 SpeedUpDirection

namespace Game.Buff
{
    /// <summary>
    /// 沿指定方向施加一股外力速度，并以给定的衰减速率快速衰减到0。
    /// 方向/初始速度/衰减速率/持续时间均由机关 SpeedTrack 写入 Player 的上下文后再触发此 Buff。
    /// </summary>
    public class SpeedBoostBuff : BuffBase
    {
        private Player.Player _target;
        private Vector2 _dir;      // 单位方向
        private float _current;    // 当前外力速度大小（会逐步衰减到0）
        private float _fade;       // 衰减速率（单位：速度/秒）

        // 不叠层；再次触发时刷新方向/强度/时长（通过 AddStack 覆盖）
        protected override int MaxStacks => 1;

        public SpeedBoostBuff()
        {
            BuffName = "SpeedBoost";
        }

        public override void OnApply(Player.Player target)
        {
            _target = target;

            // 从 Player 的上下文读取机关写入的配置
            var ctx = _target.LatestSpeedBoostContext;
            Duration = Mathf.Max(0f, ctx.duration);
            _dir     = DirToVector(ctx.direction);
            _current = Mathf.Max(0f, ctx.boost);
            _fade    = Mathf.Max(0f, ctx.fade);

            // 首次应用时立即生效：把外力速度写到 PlayerController.ExternalVelocity
            var pc = _target.GetComponent<PlayerController>();
            if (pc != null)
                pc.ExternalVelocity = _dir * _current;

            // 把层数设到1（父类维护 StackCount 的私有setter，因此用 base.AddStack）
            base.AddStack();
        }

        public override void OnUpdate(Player.Player target, float deltaTime)
        {
            // 大小向0收敛
            _current = Mathf.MoveTowards(_current, 0f, _fade * deltaTime);

            var pc = target.GetComponent<PlayerController>();
            if (pc != null)
                pc.ExternalVelocity = _dir * _current;

            base.OnUpdate(target, deltaTime); // 计时
        }

        public override void OnRemove(Player.Player target)
        {
            // 到期时清理我们这股外力（通常_current≈0，额外保护一下）
            var pc = target.GetComponent<PlayerController>();
            if (pc != null)
            {
                var ev = pc.ExternalVelocity;
                // 如果仍然基本同向且幅度不大，直接清零；（项目如需更精细的混合，再改这里）
                if (Vector2.Dot(ev, _dir) >= 0f && ev.magnitude <= _current + 0.05f)
                    pc.ExternalVelocity = Vector2.zero;
            }
        }

        /// <summary>
        /// 再次进入加速轨道时：刷新方向/强度/时长，相当于“重新点燃”这股外力。
        /// </summary>
        public override void AddStack()
        {
            if (_target != null)
            {
                var ctx = _target.LatestSpeedBoostContext;
                Duration = Mathf.Max(0f, ctx.duration);
                _dir     = DirToVector(ctx.direction);
                _current = Mathf.Max(0f, ctx.boost);
                _fade    = Mathf.Max(0f, ctx.fade);

                var pc = _target.GetComponent<PlayerController>();
                if (pc != null)
                    pc.ExternalVelocity = _dir * _current;
            }

            base.AddStack(); // 维持 StackCount=1
        }

        private static Vector2 DirToVector(SpeedUpDirection dir)
        {
            switch (dir)
            {
                case SpeedUpDirection.Up:    return Vector2.up;
                case SpeedUpDirection.Down:  return Vector2.down;
                case SpeedUpDirection.Left:  return Vector2.left;
                case SpeedUpDirection.Right: return Vector2.right;
                default:                     return Vector2.zero;
            }
        }
    }
}
