using Game.Player;
using UnityEngine;

namespace Game.Buff
{
    public class ParachuteBuff : BuffBase
    {
        private bool _isBug;//如果速度方向向上则进入bug模式
        private PlayerController _playerController;
        
        public float BaseParachuteMinSpeed { get; set; } //使用降落伞后可以减速到的最小速度 （无方向 应为正数）
        public float BaseParachuteFallAcceleration{ get; set; }// 使用降落伞后减速的能力大小
        
        protected override int MaxStacks => 3;

        public ParachuteBuff(float duration, float parachuteMinSpeed, float parachuteFallAcceleration)
        {
            BuffName = "Parachute";
            Duration = duration;
            BaseParachuteMinSpeed = parachuteMinSpeed;
            BaseParachuteFallAcceleration = parachuteFallAcceleration;
        }

        public float ParachuteMinSpeed =>BaseParachuteMinSpeed=5.2f*(StackCount-1);
        public float ParachuteFallAcceleration => BaseParachuteFallAcceleration;
        public override void OnApply(Player.Player target)
        {
            base.OnApply(target);
            _playerController = target.GetComponent<PlayerController>();
            if (_playerController._frameVelocity.y > 10000)
            {
                _isBug = true;
                _playerController.VerticalSpeed = BaseParachuteMinSpeed;
            }
            else
            {
                _isBug = false;
                _playerController.VerticalSpeed = BaseParachuteMinSpeed;
            }
        }

        public override void OnUpdate(Player.Player target, float deltaTime)
        {
            base.OnUpdate(target, deltaTime);
            if (_isBug)
            {
                _playerController._frameVelocity.y = Mathf.MoveTowards
                    (_playerController._frameVelocity.y, 
                        -ParachuteMinSpeed, 
                        ParachuteFallAcceleration * Time.fixedDeltaTime);
            }
            else
            {
                _playerController._frameVelocity.y = Mathf.MoveTowards
                (_playerController._frameVelocity.y, 
                    ParachuteMinSpeed, 
                    ParachuteFallAcceleration * Time.fixedDeltaTime);
            }
        }

        protected override void OnStackChanged()
        {
            Debug.Log($"[Parachute] 当前层数: {StackCount}");
        }
        
        public override void OnRemove(Player.Player target)
        {
            base.OnRemove(target);
            _playerController._frameVelocity.y = 2f;
            _playerController.VerticalSpeed = _playerController._stats.MaxFallSpeed;
        }
    }
}

