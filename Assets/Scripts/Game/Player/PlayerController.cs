using System;
using System.Collections;
using System.Collections.Generic;
using Game.Buff;
using Game.Item;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Game.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerController : MonoBehaviour, IPlayerController
    {
        //可以使用外力加速度
        public Vector2 ExternalVelocity { get; set; } = Vector2.zero;
        
        [SerializeField] public ScriptableStats _stats;
        private Rigidbody2D _rb;
        private CapsuleCollider2D _col;
        private FrameInput _frameInput;
        private bool _ready;
        public Vector2 _frameVelocity;
        private bool _cachedQueryStartInColliders;
        public float VerticalSpeed { get; set; }
        public float HorizontalSpeed { get; set; }
        public bool HandleGravityByController { get; set; } = true;
        public float JumpPowerRate { get; set; }
        public float HorizontalPowerRate { get; set; }
        public bool IsParachute { get; set; }
        public BuffManager BuffManager { get; set; }

        // 用于速度/朝向的标记（+1 右，-1 左）
        public int FacingSign { get; private set; } = 1;

        #region Interface

        public Vector2 FrameInput => _frameInput.Move;
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;

        #endregion

        private float _time;

        #region ItemSystem

        private int ItemIndex { get; set; }
        public List<ItemBase> items;
        public ItemVisualController itemVisualController;

        #endregion

        
        private static readonly RaycastHit2D[] _hitBuf = new RaycastHit2D[4];

        private bool CastNonTrigger(Vector2 dir, float distance, int layerMask, out RaycastHit2D hit)
        {
            var filter = new ContactFilter2D
            {
                useTriggers = false,         
                useLayerMask = true,
                layerMask = layerMask
            };
            int count = _col.Cast(dir, filter, _hitBuf, distance);
            hit = count > 0 ? _hitBuf[0] : new RaycastHit2D();
            return count > 0;
        }
        private void OnEnable()
        {
            ResetJumpState();
            JumpPowerRate = 1f;
            HorizontalPowerRate = 1f;
            IsParachute = false;
            BuffManager = GetComponent<BuffManager>();
        }

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(1f);
            _ready = true;
        }

        private void ResetJumpState()
        {
            _jumpToConsume = false;
            _bufferedJumpUsable = false;
            _endedJumpEarly = false;
            _coyoteUsable = false;
            _grounded = false;
            _frameVelocity = Vector2.zero;
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<CapsuleCollider2D>();

            _cachedQueryStartInColliders = Physics2D.queriesStartInColliders;

            VerticalSpeed = _stats.MaxFallSpeed;
            HorizontalSpeed = _stats.MaxSpeed;
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
            if (!_ready) return;
            CheckCollisions();

            HandleJump();
            HandleDirection();
            if (HandleGravityByController)
            {
                HandleGravity();
            }

            ApplyMovement();
        }

        #region Collisions
        
        private float _frameLeftGrounded = float.MinValue;
        private bool _grounded;

        private void CheckCollisions()
        {
            Physics2D.queriesStartInColliders = false;

            RaycastHit2D groundHit, ceilingHit;
            bool hitSolidGround  = CastNonTrigger(Vector2.down, _stats.GrounderDistance, ~_stats.PlayerLayer, out groundHit);
            bool hitSolidCeiling = CastNonTrigger(Vector2.up,   _stats.GrounderDistance, ~_stats.PlayerLayer, out ceilingHit);

            if (hitSolidCeiling)
                _frameVelocity.y = Mathf.Min(0, _frameVelocity.y);

            if (!_grounded && hitSolidGround)
            {
                _grounded = true;
                _coyoteUsable = true;
                _bufferedJumpUsable = true;
                _endedJumpEarly = false;
                GroundedChanged?.Invoke(true, Mathf.Abs(_frameVelocity.y));
            }
            else if (_grounded && !hitSolidGround)
            {
                _grounded = false;
                _frameLeftGrounded = _time;
                GroundedChanged?.Invoke(false, 0);
            }

            Physics2D.queriesStartInColliders = _cachedQueryStartInColliders;
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
            _frameVelocity.y = _stats.JumpPower * JumpPowerRate;
            Jumped?.Invoke();
        }

        #endregion

        #region Horizontal

        private void HandleDirection()
        {
            if (_frameInput.Move.x == 0)
            {
                var decel = _grounded ? _stats.GroundDeceleration : _stats.AirDeceleration;
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, decel * Time.fixedDeltaTime);

                // ★ 没有输入时，用当前速度方向维持朝向（静止时保持旧值）
                if (Mathf.Abs(_frameVelocity.x) > 0.01f)
                    FacingSign = _frameVelocity.x > 0 ? 1 : -1;
            }
            else
            {
                _frameVelocity.x = Mathf.MoveTowards(
                    _frameVelocity.x,
                    _frameInput.Move.x * HorizontalSpeed * HorizontalPowerRate,
                    _stats.Acceleration * Time.fixedDeltaTime
                );

                // 有输入时直接用输入方向
                FacingSign = _frameInput.Move.x > 0 ? 1 : -1;
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
                if (_endedJumpEarly && _frameVelocity.y > 0) inAirGravity *= _stats.JumpEndEarlyGravityModifier;
                _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, -VerticalSpeed, inAirGravity * Time.fixedDeltaTime);
            }
        }

        #endregion

        private void ApplyMovement() => _rb.linearVelocity = _frameVelocity + ExternalVelocity;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_stats == null) Debug.LogWarning("Please assign a ScriptableStats asset to the Player Controller's Stats slot", this);
        }
#endif
    }

    public struct FrameInput
    {
        public bool JumpDown;
        public bool JumpHeld;
        public Vector2 Move;

        public bool UseItem;
        public bool LeftSwitchItem;
        public bool RightSwitchItem;
    }

    
    
    public interface IPlayerController
    {
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;
        public Vector2 FrameInput { get; }
    }
}
