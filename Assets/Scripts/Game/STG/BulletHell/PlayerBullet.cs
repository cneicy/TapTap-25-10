using UnityEngine;

namespace Game.STG.BulletHell
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerBullet : MonoBehaviour
    {
        [Header("基础参数")]
        public float normalSpeed = 12f;
        public float focusSpeed = 15f;
        public float lifetime = 2f;
        public float damage = 10f;

        [Header("追踪参数（普通模式）")]
        public float trackingStrength = 0.5f;
        public float trackingRadius = 3f;

        [Header("收束参数（低速模式）")]
        public float convergenceStrength = 2f;

        [HideInInspector] public bool isFocusMode;

        private Rigidbody2D _rb;
        private Vector2 _direction;
        private Transform _targetBoss;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            Destroy(gameObject, lifetime);
            
            var boss = GameObject.FindGameObjectWithTag("Boss");
            if (boss)
                _targetBoss = boss.transform;
        }

        public void SetDirection(Vector2 dir)
        {
            _direction = dir.normalized;
            
            var speed = isFocusMode ? focusSpeed : normalSpeed;
            _rb.linearVelocity = _direction * speed;
        }

        private void FixedUpdate()
        {
            if (isFocusMode)
            {
                ApplyConvergence();
            }
            else
            {
                ApplyTracking();
            }
        }

        private void Update()
        {
            transform.Rotate(Vector3.forward * (Time.fixedDeltaTime * 100), Space.Self);
        }

        private void ApplyTracking()
        {
            if (!_targetBoss) return;

            var distance = Vector2.Distance(transform.position, _targetBoss.position);
            if (distance > trackingRadius) return;

            Vector2 toBoss = (_targetBoss.position - transform.position).normalized;
            var currentVel = _rb.linearVelocity;
            
            var newVel = Vector2.Lerp(currentVel, toBoss * currentVel.magnitude, 
                                          trackingStrength * Time.fixedDeltaTime);
            _rb.linearVelocity = newVel;
        }

        private void ApplyConvergence()
        {
            var offsetX = transform.position.x;
            
            var convergenceForce = new Vector2(-offsetX * convergenceStrength, 0);
            _rb.linearVelocity += convergenceForce * Time.fixedDeltaTime;
            
            _rb.linearVelocity = _rb.linearVelocity.normalized * focusSpeed;
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (!col.CompareTag("Boss")) return;
            if (col.TryGetComponent<CirnoBossController>(out var boss))
                boss.TakeDamage(damage);
            else if (col.TryGetComponent<CirnoBossController>(out var cirnoBoss))
                cirnoBoss.TakeDamage(damage);

            Destroy(gameObject);
        }
    }
}