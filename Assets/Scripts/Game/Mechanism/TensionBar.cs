// LeverSwitch.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static Game.Mechanism.MechanismHelpers;
using Game.Item;               // 识别 Bullet 组件（可选）
using Game.Player;             // 识别 PlayerController（可选）

namespace Game.Mechanism
{
    [RequireComponent(typeof(Collider2D))]
    public class TensionBar : MechanismBase
    {
        [Header("受控目标（遍历切换）")]
        public List<MechanismBase> targets = new();

        [Header("事件（可选）")]
        public UnityEvent OnToggledOnce;
        public UnityEvent<MechanismBase> OnEachToggled;

        [Header("子弹触发设置")]
        public bool enableBulletTrigger = true;
        public LayerMask bulletLayerMask;
        public float bulletDebounce = 0.08f;

        [Header("玩家触碰触发设置")]
        public bool enablePlayerTouchTrigger = true;
        public LayerMask playerLayerMask;
        public string[] playerTagWhitelist;      // 留空则不按Tag过滤
        public float playerDebounce = 0.08f;

        [Header("任意触发去抖（防同帧多次）")]
        public float anyTriggerDebounce = 0.02f;

        private float _lastBulletTime;
        private float _lastPlayerTime;
        private float _lastAnyTriggerTime;

        protected override void Awake()
        {
            base.Awake();
            if (rb) { rb.bodyType = RigidbodyType2D.Kinematic; rb.gravityScale = 0f; }
        }

        [ContextMenu("Toggle Targets")]
        public void ToggleTargets()
        {
            foreach (var m in targets)
            {
                if (!m) continue;

                if (m.IsRunning && !m.IsPaused) { m.PauseProcess();  OnEachToggled?.Invoke(m); continue; }
                if (m.IsPaused)                 { m.ResumeProcess(); OnEachToggled?.Invoke(m); continue; }

                StartByConfigured(m);
                OnEachToggled?.Invoke(m);
            }
            OnToggledOnce?.Invoke();
        }

        // —— 碰撞/触发统一入口
        private void OnCollisionEnter2D(Collision2D c) { HandleCollider(c.collider); }
        private void OnTriggerEnter2D(Collider2D other){ HandleCollider(other); }

        public void HandleCollider(Collider2D col)
        {
            if (!col) return;
            // 防止同帧被子弹+玩家双重触发
            if (Time.time - _lastAnyTriggerTime < anyTriggerDebounce) return;

            bool fired = false;
            if (enableBulletTrigger && IsBullet(col) && Time.time - _lastBulletTime >= bulletDebounce)
            {
                _lastBulletTime = Time.time;
                fired = true;
            }
            else if (enablePlayerTouchTrigger && IsPlayer(col) && Time.time - _lastPlayerTime >= playerDebounce)
            {
                _lastPlayerTime = Time.time;
                fired = true;
            }

            if (fired)
            {
                _lastAnyTriggerTime = Time.time;
                ToggleTargets();
            }
        }

        private bool IsBullet(Collider2D col)
        {
            bool byLayer = ((1 << col.gameObject.layer) & bulletLayerMask) != 0;
            bool byComp  = col.GetComponent<Bullet>() != null;
            return byLayer || byComp;
        }

        private bool IsPlayer(Collider2D col)
        {
            bool byLayer = ((1 << col.gameObject.layer) & playerLayerMask) != 0;
            bool byTag   = true;
            if (playerTagWhitelist != null && playerTagWhitelist.Length > 0)
            {
                byTag = false;
                foreach (var t in playerTagWhitelist)
                    if (!string.IsNullOrEmpty(t) && col.CompareTag(t)) { byTag = true; break; }
            }
            // 组件识别（两者其一命中即可）
            bool byComp = col.GetComponentInParent<PlayerController>() != null
                       || col.GetComponentInParent<IPlayerController>() != null;

            return (byLayer && byTag) || byComp;
        }
    }
}
