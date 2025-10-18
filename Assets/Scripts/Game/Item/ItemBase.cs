using System;
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
        public string Name { get; set; }
        public string Description { get; set; }
        //前摇时间
        public float WindupDuration { get; set; }
        //持续时间
        public float Duration { get; set; }
        //后摇时间
        public float RecoveryDuration { get; set; }
        public float Cooldown { get; set; }

        public bool IsWindingUp { get; set; }
        public bool IsUsing { get; set; }
        public bool IsRecovering { get; set; }
        public bool CanUse { get; set; } = true;
        public Sprite Sprite { get; set; } // UI图标
        //是否已经拥有
        public bool IsUsed { get; set; }
        //是否是“装备类”
        public bool IsBasement { get; set; }
        //是否是“施加buff类”
        public bool IsBuff { get; set; }
        private PlayerController _playerController;
        private float _beforeHoverSpeed;

        public virtual void Start()
        {
            _playerController = FindFirstObjectByType<PlayerController>();
        }

        public virtual void OnUseStart()
        {
            if (!CanUse) return;
            CanUse = false;
            print($"{Name} 道具使用开始");
            StartCoroutine(nameof(WindupTimer));
        }
        
        public virtual IEnumerator WindupTimer()
        {
            IsWindingUp = true;
            print("开始前摇动画");
            OnWindupStart();
            yield return new WaitForSeconds(WindupDuration);
            print("前摇结束");
            IsWindingUp = false;
            OnWindupEnd();
            IsUsing = true;
            ApplyEffect();
            StartCoroutine(nameof(DurationTimer));
        }

        public virtual IEnumerator DurationTimer()
        {
            print("转使用持续时间");
            yield return new WaitForSeconds(Duration);
            print("使用持续时间转好了");
            IsUsing = false;
            OnUseEnd();

            // 使用结束后进入后摇
            StartCoroutine(nameof(RecoveryTimer));
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
            
            StartCoroutine(nameof(CooldownTimer));
        }

        public virtual IEnumerator CooldownTimer()
        {
            /*print("转cd");*/
            yield return new WaitForSeconds(Cooldown);
            /*print("cd转好了");*/
            CanUse = true;
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
            _beforeHoverSpeed = _playerController._frameVelocity.y;
            _playerController._frameVelocity.y = 0;
            _playerController.ParachuteSpeed = 0;
        }
        
        public virtual void OnWindupEnd()
        {
            /*print("前摇结束");*/
            /*_playerController.inAirGravity = _playerController._stats.FallAcceleration;*/
            _playerController._frameVelocity.y = _beforeHoverSpeed;
            _playerController.ParachuteSpeed = _playerController._stats.MaxFallSpeed;
        }

        public virtual void OnUseEnd()
        {
            /*print("用完了");*/
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
            StopAllCoroutines();
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
            /*print("应用效果了");*/
        }
    }
}