using System.Collections;
using Data;
using Game.Level;
using ScreenEffect;
using UnityEngine;

namespace Game.STG.BulletHell
{
    [RequireComponent(typeof(CirnoEnemyShooter))]
    public class CirnoBossController : MonoBehaviour
    {
        [Header("琪露诺属性")]
        public float maxHp = 2000f;
        public float currentHp;

        [Header("移动参数")]
        public float moveAmplitude = 1.5f;
        public float moveSpeed = 1.2f;
        public float horizontalSpeed = 0.6f;
        public Vector2 moveCenter = new(0, 3.5f);

        [Header("阶段设置")]
        [Range(0f, 1f)] public float phase2Threshold = 0.6f;
        [Range(0f, 1f)] public float phase3Threshold = 0.3f;

        [Header("阶段切换动画")]
        public ParticleSystem phaseChangeParticle;
        public float phaseChangeRotationAngle = 45f;
        public float phaseChangeRotationSpeed = 180f;
        public float returnToCenterSpeed = 8f;
        
        [Header("子弹清除设置")]
        [Tooltip("阶段切换时是否清除屏幕子弹")]
        public bool clearBulletsOnPhaseChange = true;
        [Tooltip("是否使用清除特效")]
        public bool useClearEffect = true;

        [Header("视觉效果")]
        public SpriteRenderer spriteRenderer;
        public Color hitFlashColor = new(0.5f, 0.8f, 1f);
        public float flashDuration = 0.08f;
        
        [Header("UI引用")]
        public BossHealthBar healthBar;

        private CirnoEnemyShooter _shooter;
        private int _currentPhase = 1;
        private Color _defaultColor;
        private float _moveTimer;
        private bool _isDead;
        private bool _isInvincible;
        private bool _isPhaseChanging;

        private void Awake()
        {
            _shooter = GetComponent<CirnoEnemyShooter>();

            if (!spriteRenderer)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer)
                _defaultColor = spriteRenderer.color;
        }

        private void Start()
        {
            currentHp = maxHp;
            moveCenter = transform.position;

            if (healthBar)
                healthBar.SetHealth(currentHp, maxHp);

            Debug.Log("⑨ 琪露诺登场！");
        }

        private void Update()
        {
            if (_isDead || _isPhaseChanging) return;

            HandleMovement();
            UpdateHealthBar();
            spriteRenderer.transform.Rotate(Vector3.forward * (1000 * Time.fixedDeltaTime), Space.Self);

            switch (_currentPhase)
            {
                case 1 when currentHp <= maxHp * phase2Threshold:
                    StartCoroutine(PhaseChangeSequence(2));
                    break;
                case 2 when currentHp <= maxHp * phase3Threshold:
                    StartCoroutine(PhaseChangeSequence(3));
                    break;
            }
        }

        private void HandleMovement()
        {
            _moveTimer += Time.deltaTime;
            var speedMultiplier = 1f + (_currentPhase - 1) * 0.3f;
            
            var offset = new Vector3(
                Mathf.Sin(_moveTimer * horizontalSpeed * speedMultiplier) * 1.8f,
                Mathf.Sin(_moveTimer * moveSpeed * speedMultiplier) * moveAmplitude,
                0
            );
            transform.position = moveCenter + (Vector2)offset;
        }

        private void UpdateHealthBar()
        {
            if (healthBar)
                healthBar.SetHealth(currentHp, maxHp);
        }

        private IEnumerator PhaseChangeSequence(int newPhase)
        {
            _isPhaseChanging = true;
            _isInvincible = true;
            
            _shooter.StopShooting();
            
            if (clearBulletsOnPhaseChange)
            {
                ClearAllBullets();
            }
            SoundManager.Instance.Play("bossphase");
            Debug.Log($"⑨ 琪露诺：阶段 {newPhase} 开始！");

            if (phaseChangeParticle)
            {
                phaseChangeParticle.transform.position = transform.position;
                phaseChangeParticle.Play();
            }

            var moveTime = 0f;
            var startPos = transform.position;
            while (Vector3.Distance(transform.position, moveCenter) > 0.1f)
            {
                moveTime += Time.deltaTime;
                transform.position = Vector3.Lerp(startPos, moveCenter, moveTime * returnToCenterSpeed);
                yield return null;
            }
            transform.position = moveCenter;

            var startRotation = transform.rotation;
            var targetRotation = Quaternion.Euler(
                phaseChangeRotationAngle, 
                phaseChangeRotationAngle, 
                0
            );

            var rotationProgress = 0f;
            var rotationDuration = phaseChangeRotationAngle / phaseChangeRotationSpeed;

            while (rotationProgress < 1f)
            {
                rotationProgress += Time.deltaTime / rotationDuration;
                transform.rotation = Quaternion.Lerp(startRotation, targetRotation, rotationProgress);
                yield return null;
            }

            rotationProgress = 0f;
            while (rotationProgress < 1f)
            {
                rotationProgress += Time.deltaTime / rotationDuration;
                transform.rotation = Quaternion.Lerp(targetRotation, Quaternion.identity, rotationProgress);
                yield return null;
            }
            transform.rotation = Quaternion.identity;

            _currentPhase = newPhase;
            _shooter.SwitchPhase(newPhase);

            for (var i = 0; i < 3; i++)
            {
                spriteRenderer.color = Color.white;
                yield return new WaitForSeconds(0.1f);
                spriteRenderer.color = _defaultColor;
                yield return new WaitForSeconds(0.1f);
            }
            
            _isInvincible = false;
            _isPhaseChanging = false;
            
            _moveTimer = 0f;
            
            _shooter.ResumeShooting();
        }

        public void TakeDamage(float damage)
        {
            if (_isDead || _isInvincible) return;

            currentHp -= damage;
            if (currentHp <= 0)
            {
                currentHp = 0;
                Die();
            }
            else
            {
                if (spriteRenderer)
                    StartCoroutine(HitFlash());
            }
        }

        private IEnumerator HitFlash()
        {
            spriteRenderer.color = hitFlashColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = _defaultColor;
        }

        private void Die()
        {
            _isDead = true;
            StopAllCoroutines();
            _shooter.StopShooting();
            SoundManager.Instance.Play("bossdie");
            Debug.Log("⑨ 琪露诺：诶？我输了？");
            StartCoroutine(DeathSequence());
        }

        private IEnumerator DeathSequence()
        {
            if (phaseChangeParticle)
                phaseChangeParticle.Play();

            for (var i = 0; i < 6; i++)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
                yield return new WaitForSeconds(0.1f);
            }
            
            RectTransitionController.Instance.StartTransition();
            yield return new WaitForSeconds(0.25f);
            LevelManager.Instance.SwitchLevel(DataManager.Instance.GetData<string>("CurrentLevel"));
            print("finish");
            Destroy(gameObject);
        }

        private void ClearAllBullets()
        {
            var count = BulletClearEffect.ClearAllBullets(useClearEffect);
            
            if (count > 0)
            {
                Debug.Log($"[CirnoBoss] 清除了 {count} 颗子弹");
            }
        }
    }
}