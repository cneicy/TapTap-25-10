using UnityEngine;

namespace Menu
{
    public class Bullet : MonoBehaviour
    {
        public float speed = 10f;
        public float lifetime = 2f;
        [HideInInspector] public Vector2 direction = Vector2.right;

        private void Start()
        {
            Destroy(gameObject, lifetime);
            GetComponent<Rigidbody2D>().AddForce(direction * speed, ForceMode2D.Impulse);
        }


        private void OnTriggerEnter2D(Collider2D other)
        {
            Destroy(gameObject);
        }
    }
}