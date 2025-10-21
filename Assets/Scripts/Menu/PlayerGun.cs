using UnityEngine;
using UnityEngine.InputSystem;

namespace Menu
{
    public class PlayerGun : MonoBehaviour
    {
        [Header("武器参数")]
        public GameObject bulletPrefab;
        public Transform firePoint;
        public float fireRate = 0.25f;
        private float _nextFireTime;
        [SerializeField]private SpriteRenderer spriteRenderer;
        private bool _isFacingRight = true;

        private void Update()
        {
            _isFacingRight = !spriteRenderer.flipX;
            if (!InputSystem.actions.FindAction("Attack").IsInProgress() || !(Time.time >= _nextFireTime)) return;
            Shoot();
            _nextFireTime = Time.time + fireRate;
        }

        private void Shoot()
        {
            // 实例化子弹
            var bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);

            // 根据面向方向发射
            var bulletScript = bullet.GetComponent<Bullet>();
            bulletScript.direction = _isFacingRight ? Vector2.right : Vector2.left;
        }
    }
}