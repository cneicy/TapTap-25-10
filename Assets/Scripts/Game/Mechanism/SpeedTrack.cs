// BoostPad2D.cs
using System.Collections.Generic;
using UnityEngine;
using ShrinkEventBus;
using Game.Buff;
using Game.Player;

namespace Game.Mechanism
{
    [RequireComponent(typeof(Collider2D))]
    public class SpeedTrack : MechanismBase
    {
        [Header("触发过滤")]
        public LayerMask playerMask;             // 玩家层
        public string[]  playerTagWhitelist;     // 可留空
        public float     perPlayerDebounce = 0.05f; // 同一玩家短时间内重复触发的抖动限制

        [Header("加速参数")]
        public float boostMagnitude = 8f;        // 冲量（额外速度大小）
        public float decayPerSecond = 20f;       // 衰减速率（越大越快消）
        public float maxExtraSpeed  = 12f;       // 叠加上限（防爆）
        public bool  horizontalOnly = true;      // 横版推荐：只沿 X 轴

        [Header("方向兜底")]
        [Tooltip("入射速度极小时时是否用朝向/默认方向")]
        public bool fallbackToFacing = true;
        public Vector2 defaultDirection = Vector2.right;

        // 玩家去抖：instanceID -> last time
        private readonly Dictionary<int, float> _lastHitTime = new();

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayer(other)) return;

            var root = other.attachedRigidbody ? other.attachedRigidbody.transform : other.transform;
            int id   = root.gameObject.GetInstanceID();

            if (_lastHitTime.TryGetValue(id, out var t) && (Time.time - t) < perPlayerDebounce)
                return;
            _lastHitTime[id] = Time.time;

            // 取入射速度方向（优先 PlayerController._frameVelocity，再退到 Rigidbody2D.velocity）
            var pc = root.GetComponent<PlayerController>() ?? root.GetComponentInChildren<PlayerController>();
            var rb = root.GetComponent<Rigidbody2D>()      ?? root.GetComponentInChildren<Rigidbody2D>();

            Vector2 vIn = Vector2.zero;
            if (pc != null)      vIn = pc._frameVelocity;
            else if (rb != null) vIn = rb.linearVelocity;

            Vector2 dir;
            if (vIn.sqrMagnitude > 1e-6f)
            {
                dir = vIn.normalized;
            }
            else if (fallbackToFacing && pc != null)
            {
                int sign = pc.FacingSign != 0 ? pc.FacingSign : 1;
                dir = new Vector2(sign, 0f);
            }
            else
            {
                dir = defaultDirection.sqrMagnitude > 1e-6f ? defaultDirection.normalized : Vector2.right;
            }

            if (horizontalOnly)
                dir = new Vector2(Mathf.Sign(dir.x) == 0 ? 1f : Mathf.Sign(dir.x), 0f);

            var buff = new SpeedBoostBuff(dir * boostMagnitude, decayPerSecond, horizontalOnly, maxExtraSpeed);
            
            // 兜底：直接调用该玩家的 BuffManager
            var mgr = root.GetComponentInParent<BuffManager>() ?? root.GetComponent<BuffManager>();
            if (mgr != null) mgr.AddBuff(buff);
        }

        private bool IsPlayer(Collider2D col)
        {
            if (((1 << col.gameObject.layer) & playerMask) == 0) return false;

            if (playerTagWhitelist != null && playerTagWhitelist.Length > 0)
            {
                bool ok = false;
                foreach (var t in playerTagWhitelist)
                {
                    if (!string.IsNullOrEmpty(t) && col.CompareTag(t)) { ok = true; break; }
                }
                if (!ok) return false;
            }

            // 认玩家：有 PlayerController 或 Rigidbody2D 即视为玩家根或其子
            var pc = col.GetComponentInParent<PlayerController>();
            var rb = col.attachedRigidbody;
            return pc != null || rb != null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
            var p = transform.position;
            var d = (defaultDirection.sqrMagnitude > 0 ? (Vector3)defaultDirection.normalized : Vector3.right) * 0.8f;
            Gizmos.DrawLine(p, p + d);
            Gizmos.DrawLine(p + d, p + d + (Quaternion.Euler(0,0, 150) * d * 0.25f));
            Gizmos.DrawLine(p + d, p + d + (Quaternion.Euler(0,0,-150) * d * 0.25f));
        }
#endif
    }

    // 你的事件类型（若工程里已存在，请删除这段重复定义）
    public struct BuffAppliedEvent
    {
        public Player.Player Player;
        public BuffBase Buff;
    }
}
