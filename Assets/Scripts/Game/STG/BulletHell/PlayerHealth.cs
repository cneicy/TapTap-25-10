using System;
using System.Collections;
using UnityEngine;

namespace Game.STG.BulletHell
{
    public class PlayerHealth : MonoBehaviour
    {
        [Header("生命设置")]
        [Tooltip("最大生命数")]
        public int maxLives = 3;
        
        [Tooltip("当前生命数")]
        public int currentLives = 3;

        [Header("受击设置")]
        [Tooltip("无敌时间")]
        public float invincibleDuration = 3f;
        
        [Tooltip("受击后清除弹幕")]
        public bool clearBulletsOnHit = true;
        
        [Tooltip("使用弹幕清除特效")]
        public bool useClearEffect = true;

        [Header("重生设置")]
        [Tooltip("重生位置")]
        public Vector3 respawnPosition = new(0, -3, 0);
        
        [Tooltip("重生进场起始位置")]
        public Vector3 respawnEntranceOffset = new(0, -3, 0);
        
        [Tooltip("重生进场时间")]
        public float respawnEntranceDuration = 1f;

        [Header("视觉效果")]
        public SpriteRenderer spriteRenderer;
        
        [Tooltip("无敌时闪烁频率")]
        public float invincibleBlinkRate = 0.1f;
        
        [Tooltip("受击闪光颜色")]
        public Color hitFlashColor = Color.red;
        
        [Tooltip("受击闪光时间")]
        public float hitFlashDuration = 0.1f;

        [Header("引用")]
        public PlayerController playerController;

        public float rotateSpeed = 100;

        private bool _isInvincible;
        private bool _isDead;
        private Color _defaultColor;
        private Collider2D _playerCollider;

        private void FixedUpdate()
        {
            spriteRenderer.transform.Rotate(Vector3.right * (Time.fixedDeltaTime * rotateSpeed), Space.Self);
        }

        private void Start()
        {
            currentLives = maxLives;

            if (!spriteRenderer)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer)
                _defaultColor = spriteRenderer.color;

            if (!playerController)
                playerController = GetComponent<PlayerController>();

            _playerCollider = GetComponent<Collider2D>();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (_isInvincible || _isDead) return;

            if (collision.CompareTag("EnemyBullet") || collision.GetComponent<Bullet>() != null)
            {
                TakeHit();
                Destroy(collision.gameObject);
            }
        }

        public void TakeHit()
        {
            if (_isInvincible || _isDead) return;

            currentLives--;
            Debug.Log($"[Player] 受击！剩余生命: {currentLives}");

            if (currentLives <= 0)
            {
                Die();
            }
            else
            {
                StartCoroutine(HitSequence());
            }
        }

        private IEnumerator HitSequence()
        {
            _isInvincible = true;

            if (playerController)
                playerController.enabled = false;

            if (spriteRenderer)
            {
                spriteRenderer.color = hitFlashColor;
                yield return new WaitForSeconds(hitFlashDuration);
                spriteRenderer.color = _defaultColor;
            }

            if (clearBulletsOnHit)
            {
                var count = BulletClearEffect.ClearAllBullets(useClearEffect);
                Debug.Log($"[Player] 清除了 {count} 颗子弹");
            }

            yield return StartCoroutine(RespawnEntrance());

            if (playerController)
                playerController.enabled = true;

            yield return StartCoroutine(InvincibleBlink());

            _isInvincible = false;
            Debug.Log("[Player] 无敌时间结束");
        }

        private IEnumerator RespawnEntrance()
        {
            var startPos = respawnPosition + respawnEntranceOffset;
            transform.position = startPos;

            var elapsed = 0f;

            while (elapsed < respawnEntranceDuration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / respawnEntranceDuration;
                
                t = 1f - Mathf.Pow(1f - t, 2f);
                
                transform.position = Vector3.Lerp(startPos, respawnPosition, t);
                yield return null;
            }

            transform.position = respawnPosition;
        }

        private IEnumerator InvincibleBlink()
        {
            var elapsed = 0f;

            while (elapsed < invincibleDuration)
            {
                elapsed += Time.deltaTime;

                if (spriteRenderer)
                {
                    spriteRenderer.enabled = !spriteRenderer.enabled;
                }

                yield return new WaitForSeconds(invincibleBlinkRate);
            }

            if (spriteRenderer)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.color = _defaultColor;
            }
        }

        private void Die()
        {
            _isDead = true;
            Debug.Log("[Player] 玩家死亡！");

            if (playerController)
                playerController.enabled = false;

            if (_playerCollider)
                _playerCollider.enabled = false;

            StartCoroutine(DeathSequence());
        }

        private IEnumerator DeathSequence()
        {
            if (clearBulletsOnHit)
            {
                BulletClearEffect.ClearAllBullets(useClearEffect);
            }

            if (spriteRenderer)
            {
                for (var i = 0; i < 6; i++)
                {
                    spriteRenderer.enabled = !spriteRenderer.enabled;
                    yield return new WaitForSeconds(0.1f);
                }
            }

            Debug.Log("[Player] Game Over!");

            if (spriteRenderer)
                spriteRenderer.enabled = false;
        }

        private void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                normal =
                {
                    textColor = Color.white
                }
            };

            var livesText = $"Lives: {currentLives}";
            
            GUI.Label(new Rect(10, 60, 200, 30), livesText, style);
        }
    }
}