using System.Collections;
using System.Collections.Generic;
using Game.Level;
using UnityEngine;
using ShrinkEventBus;

namespace Game.Item
{
    [EventBusSubscriber]
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

        // === 视觉/实体子弹 ===
        [Header("VFX/Projectile")]
        [SerializeField] private GameObject bulletPrefab;
        [Header("子弹速度")]
        [SerializeField] private float bulletSpeed = 50f;
        [SerializeField] private Transform muzzle;           
        [SerializeField] private Vector2 muzzleOffset = new(0.6f, -1.6f);

        private GameObject _activeBullet;
        private Coroutine _bulletGuardRoutine;

        private float _baselineX;

        [Header("前摇/使用/后摇/冷却时间")]
        public float revolverWindupDuration;
        public float revolverDuration;
        public float revolverRecoveryDuration;
        public float parachuteCooldown;

        // ===== 命中机关记录（最多5个，FIFO，去重；仅在进入CD时清空） =====
        private const int MaxRecorded = 5;
        private readonly Queue<GameObject> _recordedQueue = new();
        private readonly HashSet<GameObject> _recordedSet = new();

        private static readonly string[] _triggerMethodNames =
        {
            "ActivateTargets"
        };

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
        }

        private void Awake()
        {
            RevolverInit();
        }

        [EventSubscribe]
        public void OnLevelLoadedEvent(LevelLoadedEvent evt)
        {
            ClearRecordedMechanisms();
            CleanupActiveBullet();
        }

        public override void Start()
        {
            base.Start();
        }

        public override void OnUseStart()
        {
            base.OnUseStart();
            muzzle = _playerController?.transform;
        }

        protected override void StopHover()
        {
            _playerController.HorizontalSpeed = _playerController._stats.MaxSpeed;
            _playerController.VerticalSpeed   = _playerController._stats.MaxFallSpeed;
        }

        public override void OnWindupEnd()
        {
            // 滞空覆盖前摇+使用阶段
            StartCoroutine(nameof(HoverTimer));
            SoundManager.Instance.Play("shoot");

            // 后坐力
            _baselineX = _playerController.FacingSign;
            var facing  = GetInstantFacingSign();
            var impulse = -facing * recoilImpulse;
            if (_playerController != null)
                _playerController._frameVelocity.x += impulse;

            TryScanForward(out _);

            // 清理旧子弹
            if (_bulletGuardRoutine != null) StopCoroutine(_bulletGuardRoutine);
            if (_activeBullet != null) Destroy(_activeBullet);
            if (bulletPrefab == null) return;

            // 生成子弹
            var origin =
                muzzle != null ? muzzle.position + Vector3.up * 0.5f :
                transform.position + (Vector3)new Vector2(muzzleOffset.x * facing, muzzleOffset.y);
            var dir = new Vector2(facing, 0f).normalized;

            _activeBullet = Instantiate(bulletPrefab, origin, Quaternion.identity);
            _activeBullet.transform.right = new Vector3(dir.x, dir.y, 0f);

            // 监听命中事件：记录 + 连带触发
            var bulletComp = _activeBullet.GetComponent<Bullet>();
            if (bulletComp == null) bulletComp = _activeBullet.AddComponent<Bullet>();
            bulletComp.onHit.AddListener(HandleBulletHit);

            // 实体/纯视觉两种弹
            var rb  = _activeBullet.GetComponent<Rigidbody2D>();
            var col = _activeBullet.GetComponent<Collider2D>();

            if (rb != null)
            {
                if (!col) col = _activeBullet.AddComponent<CircleCollider2D>();
                col.isTrigger = false;

                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.gravityScale = 0f;
                rb.freezeRotation = true;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                if (rb.interpolation == RigidbodyInterpolation2D.None)
                    rb.interpolation = RigidbodyInterpolation2D.Interpolate;

                rb.linearVelocity = dir * bulletSpeed;

                if (_playerController != null && col != null)
                {
                    var playerCols = _playerController.GetComponentsInChildren<Collider2D>();
                    foreach (var pc in playerCols) if (pc) Physics2D.IgnoreCollision(col, pc, true);
                    StartCoroutine(ReenablePlayerCollisionLater(col, playerCols, 0.12f));
                }
            }
            else
            {
                _activeBullet.AddComponent<VfxBulletMover>().Init(dir * bulletSpeed);
            }

            // 后摇期间的守护
            _bulletGuardRoutine = StartCoroutine(BulletRecoveryGuard());
        }

        public override void OnUseEnd()
        {
            // 这里不清记录 —— 记录要保留到进入CD
            base.OnUseEnd();
        }

        public override void OnRecoveryStart()
        {
            base.OnRecoveryStart();
        }

        public override void OnRecoveryEnd()
        {
            // 仍然不清记录；紧接着将进入 CooldownTimer
            base.OnRecoveryEnd();
        }

        // ★★★ 关键：进入CD时清空命中记录（覆盖基类协程）★★★
        public override IEnumerator CooldownTimer()
        {
            ClearRecordedMechanisms();              // 进入CD：清记录
            yield return base.CooldownTimer();      // 执行父类：等待 Cooldown 秒并置 CanUse=true
        }

        public override void OnUseCancel()
        {
            base.OnUseCancel();
            CleanupActiveBullet();
        }

        private void OnDisable()
        {
            CleanupActiveBullet();
        }

        // ====== 命中处理：记录 + 连带触发 ======
        private void HandleBulletHit(Collider2D hitCol)
        {
            if (!hitCol) return;
            
            print("有碰撞到机关 "+hitCol.name);
            // 1) 锁定机关对象
            var mechGo = hitCol.attachedRigidbody ? hitCol.attachedRigidbody.gameObject : hitCol.gameObject;

            // 2) 记录（最多5个，去重，FIFO）
            if(!hitCol.name.Contains("Grid"))
                AddMechanismRecord(mechGo);

            // 3) 连带触发：对“之前记录过的其它机关”也触发
            foreach (var go in _recordedQueue)
            {
                print("遍历到机关 "+go.name);
                if (!go || go == mechGo) continue;
                TryTriggerMechanism(go);
            }
        }

        private void AddMechanismRecord(GameObject mechGo)
        {
            if (!mechGo) return;
            if (_recordedSet.Contains(mechGo)) return;
            print("确认机关列表加入 "+mechGo.name);
            if (_recordedQueue.Count >= MaxRecorded)
            {
                var old = _recordedQueue.Dequeue();
                _recordedSet.Remove(old);
            }
            _recordedQueue.Enqueue(mechGo);
            _recordedSet.Add(mechGo);
        }

        private void ClearRecordedMechanisms()
        {
            print("记录清除");
            SoundManager.Instance.Play("recorddelete");
            _recordedQueue.Clear();
            _recordedSet.Clear();
        }

        private void TryTriggerMechanism(GameObject mechGo)
        {
            print("尝试触发机关 "+mechGo.name);
            var t = mechGo ? mechGo.transform : null;
            int depth = 0;
            while (t != null && depth < 6)
            {
                foreach (var m in _triggerMethodNames)
                    t.gameObject.SendMessage(m, SendMessageOptions.DontRequireReceiver);
                t = t.parent;
                depth++;
            }
        }

        // ========== 清理 ==========
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

        // ========== 后摇守护 ==========
        private IEnumerator BulletRecoveryGuard()
        {
            var t = 0f;
            while (t < RecoveryDuration)
            {
                if (!IsThisItemCurrentlyEquipped())
                {
                    CleanupActiveBullet();
                    yield break;
                }
                t += Time.deltaTime;
                yield return null;
            }
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
            var facing = GetInstantFacingSign();

            var scanOrigin = transform.position;
            scanOrigin.y = scanOrigin.y - 1.6f;

            Vector2 origin = scanOrigin;
            var dir    = new Vector2(facing, 0);

            var hit = Physics2D.Raycast(origin, dir, rayLength, hitMask);
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

            var x = _playerController.FrameInput.x;
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
    
    public class VfxBulletMover : MonoBehaviour
    {
        private Vector3 _velocity;
        public void Init(Vector2 velocity) => _velocity = velocity;
        private void Update() => transform.position += _velocity * Time.deltaTime;
    }
}
