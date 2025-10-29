using UnityEngine;
using UnityEngine.Events;

namespace Game.Item
{
    [RequireComponent(typeof(Collider2D))]
    public class Bullet : MonoBehaviour
    {
        [Header("命中检测")]
        [Tooltip("哪些 Layer 会被视为命中目标")]
        public LayerMask hitMask = ~0;

        [Tooltip("可选：仅当对方的 Tag 在此列表中时才算命中（留空=忽略 Tag 过滤）")]
        public string[] tagWhitelist;

        [Tooltip("命中一次后不再重复触发")]
        public bool triggerOnce = true;

        [Header("命中回调（可在 Inspector 里绑事件）")]
        public UnityEvent onAnyHit;                 // 不带参数的回调
        public UnityEvent<Collider2D> onHit;        // 带命中 Collider 的回调

        [Header("命中后是否自动销毁")]
        public bool autoDestroyOnHit = true;

        private bool _hasTriggered;

        // —— 公开：删除自身的方法（可在回调里调用）——
        public void DestroySelf()
        {
            if (gameObject)
                Destroy(gameObject);
        }

        private void Reset()
        {
            // 默认用非触发碰撞，当然你也可以手动改成 isTrigger = true 来走 OnTriggerEnter2D
            var col = GetComponent<Collider2D>();
            if (col) col.isTrigger = false;
        }

        // 非触发碰撞
        private void OnCollisionEnter2D(Collision2D c)
        {
            if (!ShouldHit(c.collider)) return;
            DispatchHit(c.collider);
        }

        // 触发器
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!ShouldHit(other)) return;
            DispatchHit(other);
        }

        private bool ShouldHit(Collider2D col)
        {
            if (!col) return false;

            // Layer 过滤
            if (((1 << col.gameObject.layer) & hitMask) == 0)
                return false;

            // Tag 白名单（如果配置了）
            if (tagWhitelist != null && tagWhitelist.Length > 0)
            {
                bool tagOK = false;
                foreach (var t in tagWhitelist)
                {
                    if (!string.IsNullOrEmpty(t) && col.CompareTag(t)) { tagOK = true; break; }
                }
                if (!tagOK) return false;
            }

            if (triggerOnce && _hasTriggered) return false;
            return true;
        }

        private void DispatchHit(Collider2D col)
        {
            _hasTriggered = true;
            
            // 先触发自定义回调（你可以在这里挂 DestroySelf）
            onAnyHit?.Invoke();
            onHit?.Invoke(col);

            // 自动销毁
            if (autoDestroyOnHit) DestroySelf();
            
        }

        private void Awake()
        {
            
        }
    }
}
