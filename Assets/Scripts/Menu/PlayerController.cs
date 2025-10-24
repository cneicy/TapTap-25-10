using System;
using Game.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Menu
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerController : MonoBehaviour, IPlayerController
    {
        [SerializeField] private ScriptableStats _stats;
        private Rigidbody2D _rb;
        private CapsuleCollider2D _col;
        private FrameInput _frameInput;
        private Vector2 _frameVelocity;

        #region Interface

        public Vector2 FrameInput => _frameInput.Move;
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;

        #endregion

        private float _time;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CapsuleCollider2D>();
        }

        private void Update()
        {
            _time += Time.deltaTime;
            GatherInput();
        }

        private void GatherInput()
        {
            _frameInput = new FrameInput
            {
                JumpDown = InputSystem.actions.FindAction("Jump").triggered,
                JumpHeld = InputSystem.actions.FindAction("Jump").IsInProgress(),
                Move = InputSystem.actions.FindAction("Move").ReadValue<Vector2>(),
            };

            if (_stats.SnapInput)
            {
                _frameInput.Move.x = Mathf.Abs(_frameInput.Move.x) < _stats.HorizontalDeadZoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.x);
                _frameInput.Move.y = Mathf.Abs(_frameInput.Move.y) < _stats.VerticalDeadZoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.y);
            }

            if (_frameInput.JumpDown)
            {
                _jumpToConsume = true;
                _timeJumpWasPressed = _time;
            }
        }

        private void FixedUpdate()
        {
            HandleJump();
            HandleDirection();
            HandleGravity();
            ApplyMovement();
        }

        #region Ground Detection (改为碰撞检测)

        private bool _grounded;
        private float _frameLeftGrounded = float.MinValue;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // 检查是否从下方接触到
            foreach (var contact in collision.contacts)
            {
                if (Vector2.Dot(contact.normal, Vector2.up) > 0.5f) // 表示接触面朝上
                {
                    if (!_grounded)
                    {
                        _grounded = true;
                        _coyoteUsable = true;
                        _bufferedJumpUsable = true;
                        _endedJumpEarly = false;
                        GroundedChanged?.Invoke(true, Mathf.Abs(_frameVelocity.y));
                    }
                    return;
                }
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            // 检查是否所有接触点都离开地面
            if (_grounded)
            {
                // 等下一个 FixedUpdate 也确认不在地面（防止物理 jitter）
                StartCoroutine(WaitAndCheckGrounded());
            }
        }

        private System.Collections.IEnumerator WaitAndCheckGrounded()
        {
            yield return new WaitForFixedUpdate();
            if (!IsTouchingGround())
            {
                _grounded = false;
                _frameLeftGrounded = _time;
                GroundedChanged?.Invoke(false, 0);
            }
        }

        private bool IsTouchingGround()
        {
            var results = new Collider2D[4];
            var count = _col.Overlap(new ContactFilter2D().NoFilter(), results);
            for (int i = 0; i < count; i++)
            {
                var contactCol = results[i];
                if (contactCol)
                {
                    var dir = (_col.bounds.center - contactCol.bounds.center).normalized;
                    if (Vector2.Dot(dir, Vector2.down) < 0) return true;
                }
            }
            return false;
        }

        #endregion

        #region Jumping

        private bool _jumpToConsume;
        private bool _bufferedJumpUsable;
        private bool _endedJumpEarly;
        private bool _coyoteUsable;
        private float _timeJumpWasPressed;

        private bool HasBufferedJump => _bufferedJumpUsable && _time < _timeJumpWasPressed + _stats.JumpBuffer;
        private bool CanUseCoyote => _coyoteUsable && !_grounded && _time < _frameLeftGrounded + _stats.CoyoteTime;

        private void HandleJump()
        {
            if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && _rb.linearVelocity.y > 0)
                _endedJumpEarly = true;

            if (!_jumpToConsume && !HasBufferedJump) return;

            if (_grounded || CanUseCoyote) ExecuteJump();

            _jumpToConsume = false;
        }

        private void ExecuteJump()
        {
            _endedJumpEarly = false;
            _timeJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;
            _frameVelocity.y = _stats.JumpPower;
            Jumped?.Invoke();
        }

        #endregion

        #region Horizontal

        private void HandleDirection()
        {
            if (_frameInput.Move.x == 0)
            {
                var deceleration = _grounded ? _stats.GroundDeceleration : _stats.AirDeceleration;
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, deceleration * Time.fixedDeltaTime);
            }
            else
            {
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, _frameInput.Move.x * _stats.MaxSpeed, _stats.Acceleration * Time.fixedDeltaTime);
            }
        }

        #endregion

        #region Gravity

        private void HandleGravity()
        {
            if (_grounded && _frameVelocity.y <= 0f)
            {
                _frameVelocity.y = _stats.GroundingForce;
            }
            else
            {
                var inAirGravity = _stats.FallAcceleration;
                if (_endedJumpEarly && _frameVelocity.y > 0)
                    inAirGravity *= _stats.JumpEndEarlyGravityModifier;

                _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, -_stats.MaxFallSpeed, inAirGravity * Time.fixedDeltaTime);
            }
        }

        #endregion

        private void ApplyMovement() => _rb.linearVelocity = _frameVelocity;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_stats == null)
                Debug.LogWarning("Please assign a ScriptableStats asset to the Player Controller's Stats slot", this);
        }
#endif
    }

    public struct FrameInput
    {
        public bool JumpDown;
        public bool JumpHeld;
        public Vector2 Move;
    }

    public interface IPlayerController
    {
        event Action<bool, float> GroundedChanged;
        event Action Jumped;
        Vector2 FrameInput { get; }
    }
}
