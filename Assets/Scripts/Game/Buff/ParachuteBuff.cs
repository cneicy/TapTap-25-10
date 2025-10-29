using Data;
using Game.Item;
using Game.Meta;
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
    
            if (StackCount >= 2)
            {
                var hasTriggered = DataManager.Instance.GetData("HasTriggeredParachuteFloat", false);
                var hasExplained = DataManager.Instance.GetData("ParachuteFloatExplained", false);
        
                if (!hasTriggered)
                {
                    // 第一次达到漂浮条件，播放隐晦提示
                    MetaAudioManager.Instance.Play("meta i-3.2-hint");
                    SoundManager.Instance.Play("meta i-3.2-hint");
                    DataManager.Instance.SetData("HasTriggeredParachuteFloat", true, true);
                }
                else if (!hasExplained)
                {
                    // 后续达到漂浮条件，检查是否达到进阶条件
                    var floatCount = DataManager.Instance.GetData("ParachuteFloatCount", 0) + 1;
                    DataManager.Instance.SetData("ParachuteFloatCount", floatCount, true);
            
                    if (floatCount >= 3)
                    {
                        MetaAudioManager.Instance.Play("meta i-3.2-advanced");
                        SoundManager.Instance.Play("meta i-3.2-advanced");
                        DataManager.Instance.SetData("ParachuteFloatExplained", true, true);
                    }
                }
            }
    
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
            
            var targetY = StackCount switch
            {
                <= 1 => -ParachuteMinSpeed,            
                2    => ParachuteMinSpeed * 0.75f,     
                _    => ParachuteMinSpeed * 1.5f      
            };

            // 用 MoveTowards 将竖直速度逼近目标
            var v = _rb.linearVelocity;
            var newY = Mathf.MoveTowards(v.y, targetY, ParachuteFallAcceleration * dt);
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
