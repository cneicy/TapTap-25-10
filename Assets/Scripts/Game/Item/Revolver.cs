using System;
using UnityEngine;
using System.Collections;
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
        [SerializeField] private LayerMask hitMask;
        public bool HasRayHit { get; private set; }
        public Vector2 lastHitPoint { get; private set; }

        // === 新增：视觉子弹配置 ===
        [Header("VFX Bullet")]
        [SerializeField] private GameObject bulletPrefab; // 纯视觉子弹预制体（无碰撞无伤害）
        [Header("子弹速度")]
        [SerializeField] private float bulletSpeed = 50f;    // 子弹飞行速度
        [SerializeField] private Transform muzzle;           // 可选：枪口位置
        [SerializeField] private Vector2 muzzleOffset = new(0.6f, -1.6f); // 若无muzzle，从玩家位置+偏移

        private GameObject _activeBullet;                    // 当前后摇期间存在的那颗子弹（只保留一颗）
        private Coroutine _bulletGuardRoutine;               // 守护协程的句柄

        private float _baselineX;
        [Header("前摇/道具使用/后摇时间")]
        public float parachuteWindupDuration;
        public float parachuteDuration;
        public float parachuteRecoveryDuration;
        [Header("道具冷却时间")]
        public float parachuteCooldown;

        private void RevolverInit()
        {
            //滞空开始
            WindupDuration = parachuteWindupDuration;
            Duration = parachuteDuration;
            RecoveryDuration = parachuteRecoveryDuration;
            //滞空结束
            Cooldown = parachuteCooldown;
        }
        
        public Revolver()
        {
            Name = "左轮手枪";
            Description = "";
            IsBuff = false;
            IsBasement = false;
            IsHoverStart = true;
            IsHoverEnd = true;
        }

        private void Awake()
        {
            RevolverInit();
        }

        public override void Start()
        {
            base.Start();
            ItemSystem.Instance.ItemsPlayerHad.Add(this);
        }

        public override void OnUseStart()
        {
            base.OnUseStart();
            muzzle = _playerController.transform;
        }

        public override void OnWindupEnd()
        {
            // —— 原有：记录基线速度与施加后坐力
            _baselineX = _playerController._frameVelocity.x;
            int facing = GetInstantFacingSign();
            float impulse = -facing * recoilImpulse;
            _playerController._frameVelocity.x += impulse;

            // —— 原有：可选的前向射线扫描
            TryScanForward(out _);

            // —— 迁移过来：发射“仅视觉”的子弹，并在后摇内监听是否切换道具
            if (_bulletGuardRoutine != null) StopCoroutine(_bulletGuardRoutine);
            if (_activeBullet != null) Destroy(_activeBullet);

            if (bulletPrefab == null) return;

            // 计算子弹初始位置与方向
            Vector3 origin =
                muzzle != null ? muzzle.position :
                    (Vector3)transform.position + (Vector3)(new Vector2(muzzleOffset.x * facing, muzzleOffset.y));
            Vector2 dir = new Vector2(facing, 0f).normalized;

            _activeBullet = Instantiate(bulletPrefab, origin, Quaternion.identity);

            // 让子弹朝向飞行方向（prefab 朝 +X 时可直接对齐）
            _activeBullet.transform.right = new Vector3(dir.x, dir.y, 0f);

            // 用刚体或纯Transform推进（纯视觉，不参与物理）
            var rb = _activeBullet.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = dir * bulletSpeed;
            }
            else
            {
                _activeBullet.AddComponent<VfxBulletMover>().Init(dir * bulletSpeed);
            }

            // 开启“后摇守护”协程：后摇期间切换道具则销毁；后摇结束也自动销毁
            _bulletGuardRoutine = StartCoroutine(BulletRecoveryGuard());
        }


        // === 关键：开火动作结束时，发射一颗“仅视觉”的子弹，并在后摇内监控是否切换道具 ===
        

        // 后摇守护：在整个 RecoveryDuration 内轮询当前是否仍装备此道具
        private IEnumerator BulletRecoveryGuard()
        {
            float t = 0f;
            while (t < RecoveryDuration)
            {
                // 若已经不再是当前装备的道具，则销毁子弹
                if (!IsThisItemCurrentlyEquipped())
                {
                    if (_activeBullet != null) Destroy(_activeBullet);
                    _activeBullet = null;
                    _bulletGuardRoutine = null;
                    yield break;
                }

                t += Time.deltaTime;
                yield return null;
            }

            
            /*if (_activeBullet != null) Destroy(_activeBullet);*/
            _activeBullet = null;
            _bulletGuardRoutine = null;
        }

        // —— 使用阶段每帧：让速度逐步回到基线
        public override void ApplyEffectTick()
        {
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

        // —— 工具：即时获取朝向
        private int GetInstantFacingSign()
        {
            if (_playerController == null) return 1;

            float x = _playerController.FrameInput.x;
            const float dead = 0.01f;
            if (x > dead)  return 1;
            if (x < -dead) return -1;

            try
            {
                return _playerController.FacingSign != 0 ? _playerController.FacingSign : 1;
            }
            catch
            {
                return 1;
            }
        }

        // —— 工具：判断当前是否仍是“正在装备/持有”的道具（用于监控切换）
        private bool IsThisItemCurrentlyEquipped()
        {
            try
            {
                // 假设 ItemSystem 有 CurrentItem 引用；若没有则默认 true（避免误判直接销毁）
                return ItemSystem.Instance != null && ItemSystem.Instance.CurrentItem == this;
            }
            catch { return true; }
        }
    }

    /// <summary>
    /// 简易的“纯视觉子弹”推进器：没有刚体时使用
    /// </summary>
    public class VfxBulletMover : MonoBehaviour
    {
        private Vector3 _velocity;

        public void Init(Vector2 velocity)
        {
            _velocity = velocity;
        }

        private void Update()
        {
            transform.position += _velocity * Time.deltaTime;
        }
    }
}
