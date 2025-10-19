using UnityEngine;

namespace Game.STG.BulletHell
{
    public class PlayerController : MonoBehaviour
    {
        [Header("移动参数")]
        public float normalSpeed = 5f;
        public float focusSpeed = 2.5f;
        public KeyCode focusKey = KeyCode.LeftShift;

        [Header("射击参数")]
        public GameObject bulletPrefab;
        public Transform[] firePoints;
        public float fireRate = 0.1f;
        
        [Header("弹幕模式")]
        [Tooltip("普通模式：弹幕扩散追踪")]
        public float normalSpreadAngle = 15f;
        [Tooltip("低速模式：弹幕收束集中")]
        public float focusSpreadAngle = 5f;

        private float _nextFire;
        private bool _isFocusing;
        private Camera _camera;

        private void Start()
        {
            _camera = Camera.main;
        }

        private void Update()
        {
            HandleMovement();
            HandleShooting();
        }

        private void HandleMovement()
        {
            _isFocusing = Input.GetKey(focusKey);
            var currentSpeed = _isFocusing ? focusSpeed : normalSpeed;

            var h = Input.GetAxisRaw("Horizontal");
            var v = Input.GetAxisRaw("Vertical");
            var move = new Vector3(h, v, 0).normalized;
            transform.position += move * (currentSpeed * Time.deltaTime);
            if(move ==  Vector3.zero) return;
            var pos = transform.position;
            var viewPos = _camera.WorldToViewportPoint(pos);
            viewPos.x = Mathf.Clamp01(viewPos.x);
            viewPos.y = Mathf.Clamp01(viewPos.y);
            pos = _camera.ViewportToWorldPoint(viewPos);
            transform.position = pos;
        }

        private void HandleShooting()
        {
            if (!(Time.time > _nextFire)) return;
            _nextFire = Time.time + fireRate;
            Fire();
        }

        private void Fire()
        {
            if (firePoints == null || firePoints.Length == 0)
            {
                FireBullet(transform.position, Vector2.up, 0f);
            }
            else
            {
                var spreadAngle = _isFocusing ? focusSpreadAngle : normalSpreadAngle;
                var bulletCount = firePoints.Length;

                for (var i = 0; i < bulletCount; i++)
                {
                    if (!firePoints[i]) continue;

                    var angle = 0f;
                    if (bulletCount > 1)
                    {
                        var step = spreadAngle / (bulletCount - 1);
                        angle = -spreadAngle / 2f + step * i;
                    }

                    FireBullet(firePoints[i].position, Vector2.up, angle);
                }
            }
        }

        private void FireBullet(Vector3 position, Vector2 baseDirection, float angleOffset)
        {
            var bullet = Instantiate(bulletPrefab, position, Quaternion.identity);

            if (!bullet.TryGetComponent<PlayerBullet>(out var pb)) return;
            var radians = angleOffset * Mathf.Deg2Rad;
            var direction = new Vector2(
                baseDirection.x * Mathf.Cos(radians) - baseDirection.y * Mathf.Sin(radians),
                baseDirection.x * Mathf.Sin(radians) + baseDirection.y * Mathf.Cos(radians)
            );
                
            pb.SetDirection(direction);
            pb.isFocusMode = _isFocusing;
        }

        private void OnDrawGizmosSelected()
        {
            if (firePoints == null) return;
            Gizmos.color = Color.green;
            foreach (var point in firePoints)
            {
                if (point)
                    Gizmos.DrawWireSphere(point.position, 0.1f);
            }
        }
    }
}