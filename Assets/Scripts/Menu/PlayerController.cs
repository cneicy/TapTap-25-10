using UnityEngine;
using UnityEngine.InputSystem;

namespace Menu
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("移动参数")]
        public float moveSpeed = 5f;
        public float jumpForce = 10f;

        [Header("地面检测参数")]
        public float rayLength = 0.2f;
        public Vector2 rayOffset = new(0f, -0.5f);

        private Rigidbody2D _rb;
        private bool _isGrounded;
        private float _moveInput;
        private bool _isFacingRight = true;
        private SpriteRenderer _spriteRenderer;

        private void Start()
        {
            _rb = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            _moveInput = InputSystem.actions.FindAction("Move").ReadValue<Vector2>().x;

            if (InputSystem.actions.FindAction("Jump").triggered && _isGrounded)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
            }

            if ((_moveInput > 0 && !_isFacingRight) || (_moveInput < 0 && _isFacingRight))
            {
                Flip();
            }
        }

        private void FixedUpdate()
        {
            var origin = (Vector2)transform.position + rayOffset;
            var hit = Physics2D.Raycast(origin, Vector2.down, rayLength);
            _isGrounded = hit.collider;

            _rb.linearVelocity = new Vector2(_moveInput * moveSpeed, _rb.linearVelocity.y);
        }

        private void Flip()
        {
            _isFacingRight = !_isFacingRight;
            _spriteRenderer.flipX = !_isFacingRight;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            var origin = (Vector2)transform.position + rayOffset;
            Gizmos.DrawLine(origin, origin + Vector2.down * rayLength);
        }
    }
}