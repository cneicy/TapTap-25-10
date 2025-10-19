using UnityEngine;

namespace Game.STG.BulletHell
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Bullet : MonoBehaviour
    {
        private Rigidbody2D _rb;
        public float lifetime = 8f;

        private void Awake() => _rb = GetComponent<Rigidbody2D>();

        private void Start()
        {
            if (gameObject.tag == "Untagged")
            {
                gameObject.tag = "EnemyBullet";
            }
        }

        public void Init(Vector2 direction, float speed)
        {
            _rb.linearVelocity = direction * speed;
            Destroy(gameObject, lifetime);
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (col.CompareTag("Player"))
            {
                // 让玩家的PlayerHealth组件处理受击
            }
        }
    }
}