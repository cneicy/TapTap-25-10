using System.Collections;
using Game.Player;
using UnityEngine;

namespace Game.Item
{
    /*
1. 初始状态
   ↓
   CanUse = true (可以使用)

2. OnUseStart() - 使用开始
   ↓
   CanUse = false (锁定使用)

3. WindupTimer(前摇阶段)
   ↓
   IsWindingUp = true
   OnWindupStart() - 播放准备动画
   等待 WindupDuration 秒
   OnWindupEnd() - 前摇结束
   IsWindingUp = false

4. 使用阶段开始
   ↓
   IsUsing = true
   ApplyEffect() - 应用初始效果（立即执行一次）

5. DurationTimer(使用持续阶段)
   ↓
   在 FixedUpdate 中持续调用 ApplyEffectTick() - 每帧应用效果
   等待 Duration 秒
   IsUsing = false
   OnUseEnd() - **使用结束回调（标志使用阶段完全结束）**

6. RecoveryTimer(后摇阶段)
   ↓
   IsRecovering = true
   OnRecoveryStart() - 播放收尾动画
   等待 RecoveryDuration 秒
   OnRecoveryEnd() - 后摇结束
   此时滞空结束 buff类道具施加buff
   IsRecovering = false

7. CooldownTimer(冷却阶段)
   ↓
   等待 Cooldown 秒

8. 冷却结束
   ↓
   CanUse = true (可以再次使用)
   回到初始状态
     */
    public abstract class ItemBase : MonoBehaviour
    {
        // 是否用真实时间计时（
        public bool UseUnscaledCooldownTick { get; set; } = false;

        // 内部时间戳
        private bool  _isInCooldown;
        private bool  _cdUnscaled;
        private float _cdStart;   // 开始时刻
        private float _cdEnd;     // 结束时刻（开始+时长）

        public bool IsInCooldown => _isInCooldown;

        // 任何时刻都能读的“已过/剩余/归一化”值（供 UI 使用）
        public float CooldownElapsed
            => !_isInCooldown ? 0f
                : Mathf.Clamp((_cdUnscaled ? Time.realtimeSinceStartup : Time.time) - _cdStart, 0f, Cooldown);

        public float CooldownRemaining
            => !_isInCooldown ? 0f
                : Mathf.Max(0f, _cdEnd - (_cdUnscaled ? Time.realtimeSinceStartup : Time.time));

        public float CooldownNormalized
            => (Cooldown <= 0f || !_isInCooldown) ? 0f
                : Mathf.Clamp01(CooldownRemaining / Cooldown);
        
        public string Name { get; set; }
        public string Description { get; set; }
        //前摇时间
        public float WindupDuration { get; set; }
        //
        //持续时间
        public float BuffDuration { get; set; }
        public float Duration { get; set; }
        //后摇时间
        public float RecoveryDuration { get; set; }
        public float Cooldown { get; set; }

        public bool IsWindingUp { get; set; }
        public bool IsUsing { get; set; }
        public bool IsRecovering { get; set; }
        public bool CanUse { get; set; } = true;

        public Sprite sprite; // UI图标
        //是否已经拥有
        public bool IsUsed { get; set; }
        //是否是“装备类”
        public bool IsBasement { get; set; }
        //是否是“施加buff类”
        public bool IsBuff { get; set; }
        protected PlayerController _playerController;
        private float _beforeHoverSpeedX;
        private float _beforeHoverSpeedY;
        
        protected Coroutine HoverCoroutine;
        protected Coroutine WindupCoroutine;
        protected Coroutine DurationCoroutine;
        protected Coroutine RecoveryCoroutine;
        protected Coroutine CooldownCoroutine;

        public virtual void Start()
        {
        }

        public virtual void OnUseStart()
        {
            _playerController = FindFirstObjectByType<PlayerController>();
            if (!CanUse) return;
            CanUse = false;
            print($"{Name} 道具使用开始");
            WindupCoroutine = StartCoroutine(nameof(WindupTimer));
        }
        
        public virtual IEnumerator WindupTimer()
        {
            IsWindingUp = true;
            /*print("开始前摇动画");*/
            OnWindupStart();
            yield return new WaitForSeconds(WindupDuration);
            /*print("前摇结束");*/
            IsWindingUp = false;
            OnWindupEnd();
            IsUsing = true;
            ApplyEffect();
            DurationCoroutine = StartCoroutine(nameof(DurationTimer));
        }

        public virtual IEnumerator DurationTimer()
        {
            /*print("转使用持续时间");*/
            yield return new WaitForSeconds(Duration);
            /*print("使用持续时间转好了");*/
            IsUsing = false;
            OnUseEnd();
            // 使用结束后进入后摇
            RecoveryCoroutine = StartCoroutine(nameof(RecoveryTimer));
        }
        
        public virtual IEnumerator RecoveryTimer()
        {
            IsRecovering = true;
            /*print("开始后摇动画");*/
            OnRecoveryStart();
            yield return new WaitForSeconds(RecoveryDuration);
            /*print("后摇结束");*/
            IsRecovering = false;
            OnRecoveryEnd();
            CooldownCoroutine = StartCoroutine(nameof(CooldownTimer));
        }

        public virtual IEnumerator CooldownTimer()
        {
            // 冷却时长小于等于0：直接结束
            if (Cooldown <= 0f)
            {
                CanUse = true;
                yield break;
            }

            _isInCooldown = true;
            _cdUnscaled   = UseUnscaledCooldownTick;

            var now = _cdUnscaled ? Time.realtimeSinceStartup : Time.time;
            _cdStart = now;
            _cdEnd   = now + Cooldown;

            // 这里让协程“睡到结束”，UI 则用上面的属性随时读
            if (_cdUnscaled) yield return new WaitForSecondsRealtime(Cooldown);
            else             yield return new WaitForSeconds(Cooldown);

            _isInCooldown = false;
            CanUse = true;
        }

        public virtual IEnumerator HoverTimer()
        {
            StartHover();
            yield return new WaitForSeconds(WindupDuration+Duration);
            StopHover();
        }
        
        private void FixedUpdate()
        {
            if (!IsUsing)
            {
                return;
            }
            ApplyEffectTick();
        }
        
        public virtual void OnWindupStart()
        {
            /*print("前摇开始 - 播放准备动画");*/
            /*_playerController.inAirGravity = 0;*/
        }
        
        public virtual void OnWindupEnd()
        {
        }

        public virtual void OnUseEnd()
        {
            /*print("用完了");*/
            ApplyBuffEffect();
        }
        
        public virtual void OnRecoveryStart()
        {
            /*print("后摇开始 - 播放收尾动画");*/
            
        }
        
        public virtual void OnRecoveryEnd()
        {
            /*print("后摇结束");*/
        }

        public virtual void OnUseCancel()
        {
            /*print("不用了");*/
            StopIfRunning(ref WindupCoroutine);
            StopIfRunning(ref DurationCoroutine);
            StopIfRunning(ref RecoveryCoroutine);
            StopIfRunning(ref CooldownCoroutine);
            
            _isInCooldown = false;
            
            CanUse = true;
            IsWindingUp = false;
            IsUsing = false;
            IsRecovering = false;
        }

        public virtual void ApplyEffectTick()
        {
            /*print("此tick应用效果");*/
        }

        public virtual void ApplyEffect()
        {
            
        }

        public virtual void ApplyBuffEffect()
        {
            
        }

        protected virtual void StartHover()
        {
            print("滞空开始");
            _beforeHoverSpeedX = _playerController._frameVelocity.x;
            _beforeHoverSpeedY = _playerController._frameVelocity.y;
            _playerController._frameVelocity.x = 0;
            _playerController._frameVelocity.y = 0;
            _playerController.HorizontalSpeed = 0;
            _playerController.VerticalSpeed = 0;
        }

        protected virtual void StopHover()
        {
            _playerController._frameVelocity.x = _beforeHoverSpeedX;
            _playerController._frameVelocity.y = _beforeHoverSpeedY;
            _playerController.HorizontalSpeed = _playerController._stats.MaxSpeed;
            _playerController.VerticalSpeed = _playerController._stats.MaxFallSpeed;
        }
        
        // 放在 ItemBase 内
        private void StopIfRunning(ref Coroutine co)
        {
            if (co != null)
            {
                StopCoroutine(co);
                co = null;             // 清空句柄，防止二次 Stop
            }
        }

    }
}
