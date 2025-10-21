using UnityEngine;
using Game.Player;

namespace Game.Item
{
    public class Revolver : ItemBase
    {
        // 后坐力大小
        [SerializeField] private float recoilImpulse = 12f;
        // 回弹速度
        [SerializeField] private float fadeSpeed = 110f;

        private float _baselineX;

        public Revolver()
        {
            Name = "左轮手枪";
            Description = "";
            WindupDuration = 0.2f;
            Duration = 0.2f;
            RecoveryDuration = 0.2f;
            Cooldown = 1f;
            IsBuff = false;
            IsBasement = false;
        }

        public override void Start()
        {
            base.Start();
            ItemSystem.Instance.ItemsPlayerHad.Add(this);
            recoilImpulse = 30f;
        }

        public override void OnWindupStart()
        {
            // 记录开枪前的速度
            _baselineX = _playerController._frameVelocity.x;

            // 计算反方向的后坐力
            int facing = 1;
            if (_playerController != null) facing = _playerController.FacingSign; // 若使用了上面的 FacingSign

            float impulse = -facing * recoilImpulse;

            // 施加瞬时到帧速度
            _playerController._frameVelocity.x += impulse;
            
            
        }

        public override void ApplyEffectTick()
        {
            _playerController._frameVelocity.x = Mathf.MoveTowards(
                _playerController._frameVelocity.x,
                _baselineX,
                fadeSpeed * Time.fixedDeltaTime
            );
        }
    }
}