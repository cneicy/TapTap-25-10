using UnityEngine;

namespace Game.Buff
{
    public class ParachuteBuff : BuffBase
    {
        private float _fallSpeedMultiplier; // 下落速度倍率（例如 0.3f 表示速度变为原来的 30%）

        public ParachuteBuff(float duration, float fallSpeedMultiplier)
        {
            BuffName = "Parachute";
            Duration = duration;
            _fallSpeedMultiplier = fallSpeedMultiplier;
        }

        public override void OnApply(Player.Player target)
        {
            Debug.Log($"Parachute Buff applied for {Duration}s, fall speed reduced to {_fallSpeedMultiplier * 100}%.");
        }

        public override void OnUpdate(Player.Player target, float deltaTime)
        {
            base.OnUpdate(target, deltaTime);

            if (target.rb2d != null)
            {
                var velocity = target.rb2d.linearVelocity;

                // 仅在角色下落时生效（y 为负）
                if (velocity.y < 0)
                {
                    // 让下落速度变慢，例如原来是 -10，现在变成 -10 * 0.3 = -3
                    velocity.y -= _fallSpeedMultiplier;

                    target.rb2d.linearVelocity = velocity;
                }
            }
        }

        public override void OnRemove(Player.Player target)
        {
            Debug.Log("Parachute Buff expired.");
        }
    }
}

