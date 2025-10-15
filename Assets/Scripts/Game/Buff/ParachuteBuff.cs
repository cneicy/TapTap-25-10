using Game.Player;
using UnityEngine;

namespace Game.Buff
{
    public class ParachuteBuff : BuffBase
    {
        private bool _isBug;//如果速度方向向上则进入bug模式
        private PlayerController _playerController;
        
        public float ParachuteMinSpeed { get; set; } //使用降落伞后可以减速到的最小速度 （无方向 应为正数）
        public float ParachuteFallAcceleration{ get; set; }// 使用降落伞后减速的能力大小

        public ParachuteBuff(float duration, float parachuteMinSpeed, float parachuteFallAcceleration)
        {
            BuffName = "Parachute";
            Duration = duration;
            ParachuteMinSpeed = parachuteMinSpeed;
            ParachuteFallAcceleration = parachuteFallAcceleration;
        }

        public override void OnApply(Player.Player target)
        {
            base.OnApply(target);
            _playerController = target.GetComponent<PlayerController>();
            if (_playerController._frameVelocity.y > 0)
            {
                _isBug = true;
            }
            else
            {
                _isBug = false;
            }
            _playerController.ParachuteSpeed = _playerController._frameVelocity.y;
            if (_playerController._frameVelocity.y == _playerController.ParachuteSpeed)
            {
                _isBug = true;
            }
        }

        public override void OnUpdate(Player.Player target, float deltaTime)
        {
            base.OnUpdate(target, deltaTime);
            if (_isBug)
            {
                _playerController._frameVelocity.y = Mathf.MoveTowards
                    (_playerController._frameVelocity.y, 
                        ParachuteMinSpeed, 
                        ParachuteFallAcceleration * Time.fixedDeltaTime);
            }
            else
            {
                _playerController._frameVelocity.y = Mathf.MoveTowards
                (_playerController._frameVelocity.y, 
                    -ParachuteMinSpeed, 
                    ParachuteFallAcceleration * Time.fixedDeltaTime);
            }
        }

        public override void OnRemove(Player.Player target)
        {
            base.OnRemove(target);
            _playerController.ParachuteSpeed = _playerController._stats.MaxFallSpeed;
        }
    }
}

