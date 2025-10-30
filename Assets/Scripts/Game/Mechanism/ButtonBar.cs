using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static Game.Mechanism.MechanismHelpers;
using Game.Item;    
using Game.Player;   

namespace Game.Mechanism
{
    [RequireComponent(typeof(Collider2D))]
    public class ButtonBar : MechanismBase
    {
        [Header("受控目标")]
        public List<MechanismBase> targets = new();

        [Header("按钮锁定事件（可选）")]
        public UnityEvent OnLocked;
        public UnityEvent OnUnlocked;
        public UnityEvent OnTouchWhileLocked;   // 触发被拒时的反馈（SFX/UI）

        [Header("逐个目标事件（可选）")]
        public UnityEvent<MechanismBase> OnEachAffected;

        [Header("子弹触发设置")]
        public bool enableBulletTrigger = true;
        public LayerMask bulletLayerMask;
        public float bulletDebounce = 0.08f;

        [Header("玩家触碰触发设置")]
        public bool enablePlayerTouchTrigger = true;
        public LayerMask playerLayerMask;
        public string[] playerTagWhitelist;
        public float playerDebounce = 0.08f;

        [Header("任意触发去抖")]
        public float anyTriggerDebounce = 0.02f;

        private float _lastBulletTime, _lastPlayerTime, _lastAnyTriggerTime;
        private bool _locked;
        private Coroutine _lockRoutine;
        public bool IsLocked => _locked;

        protected override void Awake()
        {
            base.Awake();
            if (rb) { rb.bodyType = RigidbodyType2D.Kinematic; rb.gravityScale = 0f; }
        }

        [ContextMenu("Activate / Toggle Targets")]
        public void ActivateTargets()
        {
            if (_locked) { OnTouchWhileLocked?.Invoke(); return; }

            var watchNonLoop = new List<MechanismBase>();
            StartCoroutine(nameof(PowerColor));
            foreach (var m in targets)
            {
                if (!m) continue;

                var effectiveMode = (m.IsRunning || m.IsPaused) ? m.Mode : m.mode;
                var touched = false;

                if (effectiveMode == MotionMode.Loop)
                {
                    if (m.IsRunning && !m.IsPaused) m.PauseProcess();
                    else if (m.IsPaused)           m.ResumeProcess();
                    else                           StartByConfigured(m);
                    // touched 对 Loop 不参与锁定逻辑
                }
                else
                {
                    if (m.IsPaused)        { m.ResumeProcess();    touched = true; }
                    else if (!m.IsRunning) { StartByConfigured(m); touched = true; }
                    // 已在运行且未暂停：不改变状态，但仍然要发事件以保持与 TensionBar 一致
                }

                // —— 关键：每个 target 都触发一次（与 TensionBar 行为保持一致）
                OnEachAffected?.Invoke(m);

                // 非 Loop 情况下，只有真实变更才进入锁定观察
                if (effectiveMode != MotionMode.Loop && touched)
                    watchNonLoop.Add(m);
            }

            if (watchNonLoop.Count > 0)
            {
                SetLocked(true);
                if (_lockRoutine != null) StopCoroutine(_lockRoutine);
                _lockRoutine = StartCoroutine(WaitUntilNonLoopCompleted(watchNonLoop));
            }
        }


        private IEnumerator WaitUntilNonLoopCompleted(List<MechanismBase> watch)
        {
            while (true)
            {
                var allDone = true;
                for (var i = watch.Count - 1; i >= 0; --i)
                {
                    var m = watch[i];
                    if (!m) { watch.RemoveAt(i); continue; }
                    if (!(!m.IsRunning && m.IsDone)) allDone = false;
                }
                if (allDone || watch.Count == 0) break;
                yield return null;
            }
            SetLocked(false);
            _lockRoutine = null;
        }

        private void SetLocked(bool v)
        {
            if (_locked == v) return;
            _locked = v;
            if (_locked) OnLocked?.Invoke(); else OnUnlocked?.Invoke();
        }

        // —— 碰撞/触发统一入口
        private void OnCollisionEnter2D(Collision2D c) { HandleCollider(c.collider); }
        private void OnTriggerEnter2D(Collider2D other){ HandleCollider(other); }

        public void HandleCollider(Collider2D col)
        {
            if (!col) return;
            if (Time.time - _lastAnyTriggerTime < anyTriggerDebounce) return;

            var fired = false;
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
                SoundManager.Instance.Play("mecbtn");
                ActivateTargets();
            }
        }

        private bool IsBullet(Collider2D col)
        {
            var byLayer = ((1 << col.gameObject.layer) & bulletLayerMask) != 0;
            var byComp  = col.GetComponent<Bullet>() != null;
            return byLayer || byComp;
        }

        private bool IsPlayer(Collider2D col)
        {
            var byLayer = ((1 << col.gameObject.layer) & playerLayerMask) != 0;
            var byTag   = true;
            if (playerTagWhitelist != null && playerTagWhitelist.Length > 0)
            {
                byTag = false;
                foreach (var t in playerTagWhitelist)
                    if (!string.IsNullOrEmpty(t) && col.CompareTag(t)) { byTag = true; break; }
            }
            var byComp = col.GetComponentInParent<PlayerController>() != null
                         || col.GetComponentInParent<IPlayerController>() != null;

            return (byLayer && byTag) || byComp;
        }
        private IEnumerator PowerColor()
        {
            GetComponent<SpriteRenderer>().color = Color.red;
            yield return new WaitForSeconds(1f);
            GetComponent<SpriteRenderer>().color = Color.white;
        }
    }
}
