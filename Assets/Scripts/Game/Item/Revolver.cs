using UnityEngine;

namespace Game.Item
{
    public class Revolver : ItemBase
    {
        //后坐力衰减速度
        public float fadeSpeed;
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

        public void Awake()
        {
            fadeSpeed = 110;
            ItemSystem.Instance.ItemsPlayerHad.Add(this);
        }
        public override void OnWindupStart()
        {
            _playerController._frameVelocity.x = -46f;
            _playerController._frameVelocity.x = Mathf.MoveTowards(_playerController._frameVelocity.x, 0, 
                fadeSpeed * Time.fixedDeltaTime);
        }

        public override void OnWindupEnd()
        {
            
        }
    }
}
