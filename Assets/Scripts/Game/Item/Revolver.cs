using System.Collections;
using UnityEngine;
using Game.Player; 

namespace Game.Item
{
    public class Revolver : ItemBase
    {
        [Header("后坐力大小/后坐力衰减速度")]
        [SerializeField] private float recoilImpulse = 30f;
        [SerializeField] private float fadeSpeed     = 110f;

        [Header("Raycast")]
        [SerializeField] private float rayLength = 100f;
        [SerializeField] private LayerMask hitMask = ~0;
        public bool HasRayHit { get; private set; }
        public Vector2 lastHitPoint { get; private set; }
        private bool _revovlerHoverEnd;

        // === 视觉/实体子弹 ===
        [Header("VFX/Projectile")]
        [SerializeField] private GameObject bulletPrefab; // 纯视觉或实体子弹预制体
        [Header("子弹速度")]
        [SerializeField] private float bulletSpeed = 50f;    // 子弹飞行速度
        [SerializeField] private Transform muzzle;           // 可选：枪口位置
        [SerializeField] private Vector2 muzzleOffset = new(0.6f, -1.6f); // 若无muzzle，从玩家位置+偏移

        private GameObject _activeBullet;    // 只保留一颗（仅作引用管理，不强制销毁）
        private Coroutine _bulletGuardRoutine;

        private float _baselineX;

        [Header("前摇/道具使用/后摇时间")]
        public float revolverWindupDuration;
        public float revolverDuration;
        public float revolverRecoveryDuration;
        [Header("道具冷却时间")]
        public float parachuteCooldown;

        private void RevolverInit()
        {
            WindupDuration   = revolverWindupDuration;
            Duration         = revolverDuration;
            RecoveryDuration = revolverRecoveryDuration;
            Cooldown         = parachuteCooldown;
        }
        
        public Revolver()
        {
            Name = "左轮手枪";
            Description = "";
            IsBuff = false;
            IsBasement = false;
            IsHoverStart = true;
            IsHoverEnd = false;
            _revovlerHoverEnd = true;
        }

        private void Awake()
        {
            RevolverInit();
        }

        public override void Start()
        {
            base.Start();
            ItemSystem.Instance?.ItemsPlayerHad.Add(this);
        }

        public override void OnUseStart()
        {
            base.OnUseStart();
            muzzle = _playerController?.transform;
        }

        public override void OnWindupEnd()
        {
            // —— 保留原有：记录基线速度与施加后坐力
            _baselineX = _playerController?._frameVelocity.x ?? 0f;
            int facing = GetInstantFacingSign();
            float impulse = -facing * recoilImpulse;
            if (_playerController != null)
                _playerController._frameVelocity.x += impulse;

            // —— 可选：前向扫描
            TryScanForward(out _);

            // —— 保留原有：只保留一颗子弹引用，后摇内守护
            if (_bulletGuardRoutine != null) StopCoroutine(_bulletGuardRoutine);
            if (_activeBullet != null) Destroy(_activeBullet);
            if (bulletPrefab == null) return;

            // 计算初始位置与方向（你当前版本抬高了 0.5f，保留）
            Vector3 origin =
                muzzle != null ? muzzle.position + Vector3.up * 0.5f :
                (Vector3)transform.position + (Vector3)(new Vector2(muzzleOffset.x * facing, muzzleOffset.y));
            Vector2 dir = new Vector2(facing, 0f).normalized;

            _activeBullet = Instantiate(bulletPrefab, origin, Quaternion.identity);
            _activeBullet.transform.right = new Vector3(dir.x, dir.y, 0f); // 可视朝向

            // ====== 若预制体带 Rigidbody2D，则作为“实体子弹” ======
            var rb  = _activeBullet.GetComponent<Rigidbody2D>();
            var col = _activeBullet.GetComponent<Collider2D>();

            if (rb != null)
            {
                if (!col) col = _activeBullet.AddComponent<CircleCollider2D>(); // 实体需要碰撞体
                col.isTrigger = false; // 实体碰撞

                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                if (rb.interpolation == RigidbodyInterpolation2D.None)
                    rb.interpolation = RigidbodyInterpolation2D.Interpolate;

                // 出膛速度（2D 正确属性）
                rb.linearVelocity = dir * bulletSpeed;

                // 避免一出生就撞到玩家自身：短暂忽略碰撞
                if (_playerController != null && col != null)
                {
                    var playerCols = _playerController.GetComponentsInChildren<Collider2D>();
                    foreach (var pc in playerCols) if (pc) Physics2D.IgnoreCollision(col, pc, true);
                    StartCoroutine(ReenablePlayerCollisionLater(col, playerCols, 0.12f));
                }
            }
            else
            {
                // —— 无刚体：回退到“纯视觉移动”，保持你原来的行为
                _activeBullet.AddComponent<VfxBulletMover>().Init(dir * bulletSpeed);
            }

            // —— 后摇守护（兜底：若系统没调用 OnUseCancel 也能检测切换）
            _bulletGuardRoutine = StartCoroutine(BulletRecoveryGuard());
        }

        public override void OnUseEnd()
        {
            base.OnUseEnd();
            StopHover(_revovlerHoverEnd);
        }

        // ========= 关键：切换道具时会被 ItemSystem 调用 =========
        public override void OnUseCancel()
        {
            base.OnUseCancel();      // 停止所有协程并重置状态（见你的 ItemBase）
            CleanupActiveBullet();   // 立刻清除正在飞行的子弹
        }

        private void OnDisable()
        {
            // 防御式：道具被禁用时也清除
            CleanupActiveBullet();
        }

        // ========== 统一清理 ==========
        private void CleanupActiveBullet()
        {
            if (_bulletGuardRoutine != null)
            {
                StopCoroutine(_bulletGuardRoutine);
                _bulletGuardRoutine = null;
            }
            if (_activeBullet != null)
            {
                Destroy(_activeBullet);
                _activeBullet = null;
            }
        }

        // ========== 后摇守护：若后摇期间发生道具切换也能清除 ==========
        private IEnumerator BulletRecoveryGuard()
        {
            float t = 0f;
            while (t < RecoveryDuration)
            {
                // 后摇内一旦切换当前道具 -> 立刻清除正在飞行的子弹
                if (!IsThisItemCurrentlyEquipped())
                {
                    CleanupActiveBullet();
                    yield break;
                }
                t += Time.deltaTime;
                yield return null;
            }

            // 保持你原逻辑：后摇结束不强制销毁，只把引用清空
            _bulletGuardRoutine = null;
            _activeBullet = null;
        }

        private IEnumerator ReenablePlayerCollisionLater(Collider2D bulletCol, Collider2D[] playerCols, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (!bulletCol) yield break;
            foreach (var pc in playerCols)
                if (pc) Physics2D.IgnoreCollision(bulletCol, pc, false);
        }

        // —— 使用阶段：速度回到基线（保留）
        public override void ApplyEffectTick()
        {
            if (_playerController == null) return;
            _playerController._frameVelocity.x = Mathf.MoveTowards(
                _playerController._frameVelocity.x,
                _baselineX,
                fadeSpeed * Time.fixedDeltaTime
            );
        }

        public bool TryScanForward(out Vector2 hitPoint)
        {
            int facing = GetInstantFacingSign();

            var scanOrigin = transform.position;
            scanOrigin.y = scanOrigin.y - 1.6f;

            Vector2 origin = scanOrigin;
            Vector2 dir    = new Vector2(facing, 0);

            RaycastHit2D hit = Physics2D.Raycast(origin, dir, rayLength, hitMask);
            Debug.DrawRay(origin, dir * rayLength, Color.red, 0.1f);

            if (hit.collider != null)
            {
                lastHitPoint = hit.point;
                HasRayHit = true;
                hitPoint = lastHitPoint;
                return true;
            }

            HasRayHit = false;
            hitPoint = default;
            return false;
        }

        private int GetInstantFacingSign()
        {
            if (_playerController == null) return 1;

            float x = _playerController.FrameInput.x;
            const float dead = 0.01f;
            if (x > dead)  return 1;
            if (x < -dead) return -1;

            try { return _playerController.FacingSign != 0 ? _playerController.FacingSign : 1; }
            catch { return 1; }
        }

        private bool IsThisItemCurrentlyEquipped()
        {
            try
            {
                return ItemSystem.Instance != null && ItemSystem.Instance.CurrentItem == this;
            }
            catch { return true; }
        }
    }

    /// <summary>
    /// 纯视觉子弹：当预制体没有 Rigidbody2D 时使用
    /// </summary>
    public class VfxBulletMover : MonoBehaviour
    {
        private Vector3 _velocity;

        public void Init(Vector2 velocity) => _velocity = velocity;

        private void Update() => transform.position += _velocity * Time.deltaTime;
    }
}
