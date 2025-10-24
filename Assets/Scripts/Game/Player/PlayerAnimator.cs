using System.Collections;
using Data;
using Game.Level;
using ShrinkEventBus;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    public class PlayerSpinEvent : EventBase
    {
        public readonly Transform Player;
        public PlayerSpinEvent(Transform player)
        {
            Player = player;
        }
    }
    
    [EventBusSubscriber]
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private int flashCount = 4;           // 来回切换次数
        [SerializeField] private float flashInterval = 0.08f;  // 每次切换间隔

        
        [Header("References")] 
        [SerializeField] private Animator _anim;
        [SerializeField] private SpriteRenderer _sprite;

        [Header("Settings")] 
        [SerializeField, Range(1f, 3f)] private float _maxIdleSpeed = 2;
        [SerializeField] private float _maxTilt = 5;
        [SerializeField] private float _tiltSpeed = 20;

        [Header("Animation Layers")]
        [SerializeField] private int baseLayerIndex = 0;
        [SerializeField] private int level2LayerIndex = 1;
        [SerializeField] private float layerTransitionTime = 0.3f;

        [Header("Footstep Settings")]
        [SerializeField, Tooltip("脚步音之间的时间间隔（秒）")]
        private float _footstepInterval = 0.35f;
        private float _footstepTimer = 0f;

        [Header("Particles")] 
        [SerializeField] private ParticleSystem _jumpParticles;
        [SerializeField] private ParticleSystem _launchParticles;
        [SerializeField] private ParticleSystem _moveParticles;
        [SerializeField] private ParticleSystem _landParticles;

        [Header("Audio Clips")] 
        [SerializeField] private AudioClip[] _footsteps;

        private AudioSource _source;
        [SerializeField] private AudioSource _sfxSource;
        private IPlayerController _player;
        private bool _grounded;
        private ParticleSystem.MinMaxGradient _currentGradient;
        private bool _isTrueWorld;
        private bool _isLevel2LayerActive;
        private Coroutine _layerTransitionCoroutine;

        [EventSubscribe]
        public void OnLevelChangeEvent(LevelLoadedEvent evt)
        {
            _isTrueWorld = evt.IsTrueWorld;
        }
        
        [EventSubscribe]
        public void OnPlayerSpinEvent(PlayerSpinEvent evt)
        {
            if (!evt.Player) return;
            StartCoroutine(SpinWithElastic(evt.Player));
        }
        
        private IEnumerator SpinWithElastic(Transform player)
        {
            var duration = 1.2f;
            var elapsed = 0f;
            var startRotation = player.localEulerAngles;
            var startScale = player.localScale;
            var maxScale = startScale * 1.1f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / duration;

                var overshoot = Mathf.Sin(t * Mathf.PI * 2f) * (1f - t) * 20f;
                var angle = Mathf.Lerp(0f, 360f, EaseOutCubic(t)) + overshoot;

                player.localEulerAngles = new Vector3(
                    startRotation.x,
                    startRotation.y + angle,
                    startRotation.z
                );

                var scaleT = Mathf.Sin(t * Mathf.PI);
                player.localScale = Vector3.Lerp(startScale, maxScale, scaleT);

                yield return null;
            }

            player.localEulerAngles = startRotation;
            player.localScale = startScale;
        }


        private float EaseOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return 1 - Mathf.Pow(1 - t, 3);
        }

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _player = GetComponentInParent<IPlayerController>();
            
            InitializeLayerWeights();
        }

        private void InitializeLayerWeights()
        {
            if(!DataManager.Instance.GetData<bool>("IsLevel2LayerActive"))
            {
                _anim.SetLayerWeight(baseLayerIndex, 1f);
                _anim.SetLayerWeight(level2LayerIndex, 0f);
            }
            else
            {
                _anim.SetLayerWeight(baseLayerIndex, 0f);
                _anim.SetLayerWeight(level2LayerIndex, 1f);
            }
            _isLevel2LayerActive = DataManager.Instance.GetData<bool>("IsLevel2LayerActive");
        }

        private void OnEnable()
        {
            EventBus.AutoRegister(this);
            _player.Jumped += OnJumped;
            _player.GroundedChanged += OnGroundedChanged;
            _moveParticles.Play();
        }

        private void OnDisable()
        {
            EventBus.UnregisterAllEventsForObject(this);
            _player.Jumped -= OnJumped;
            _player.GroundedChanged -= OnGroundedChanged;
            _moveParticles.Stop();
            
            if (_layerTransitionCoroutine != null)
            {
                StopCoroutine(_layerTransitionCoroutine);
            }
        }

        private void Update()
        {
            if (_player == null) return;

            DetectGroundColor();
            HandleSpriteFlip();
            HandleIdleSpeed();
            HandleCharacterTilt();
            HandleRunAnimation();
            HandleLayerSwitch();
        }

        private void HandleLayerSwitch()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleAnimationLayer();
            }
        }

        private void ToggleAnimationLayer()
        {
            if (_layerTransitionCoroutine != null)
                return;

            if (_isLevel2LayerActive)
                SwitchToLayer(baseLayerIndex, level2LayerIndex);
            else
                SwitchToLayer(level2LayerIndex, baseLayerIndex);

            _isLevel2LayerActive = !_isLevel2LayerActive;
            DataManager.Instance.SetData("IsLevel2LayerActive", _isLevel2LayerActive,true);
        }

        private void SwitchToLayer(int targetLayer, int previousLayer)
        {
            if (_layerTransitionCoroutine != null)
                StopCoroutine(_layerTransitionCoroutine);

            _layerTransitionCoroutine = StartCoroutine(LayerFlashTransition(targetLayer, previousLayer));
        }
        
        private IEnumerator LayerFlashTransition(int targetLayer, int previousLayer)
        {
            print("Layer Flash Transition");
            if(Keyboard.current != null)
                InputSystem.DisableDevice(Keyboard.current);
            if(Mouse.current != null)
                InputSystem.DisableDevice(Mouse.current);
            if(Gamepad.current != null)
                InputSystem.DisableDevice(Gamepad.current);

            for (var i = 0; i < flashCount; i++)
            {
                var toggle = i % 2 == 0;
                _anim.SetLayerWeight(targetLayer, toggle ? 1f : 0f);
                _anim.SetLayerWeight(previousLayer, toggle ? 0f : 1f);
                yield return new WaitForSeconds(flashInterval);
            }

            _anim.SetLayerWeight(targetLayer, 1f);
            _anim.SetLayerWeight(previousLayer, 0f);

            yield return null;
            if(Keyboard.current != null)
                InputSystem.EnableDevice(Keyboard.current);
            if(Mouse.current != null)
                InputSystem.EnableDevice(Mouse.current);
            if(Gamepad.current != null)
                InputSystem.EnableDevice(Gamepad.current);

            _layerTransitionCoroutine = null;
        }


        private void HandleSpriteFlip()
        {
            if (_player.FrameInput.x != 0)
                _sprite.flipX = _player.FrameInput.x < 0;
        }

        private void HandleIdleSpeed()
        {
            var inputStrength = Mathf.Abs(_player.FrameInput.x);
            _anim.SetFloat(IdleSpeedKey, Mathf.Lerp(1, _maxIdleSpeed, inputStrength));
            _moveParticles.transform.localScale = Vector3.MoveTowards(_moveParticles.transform.localScale, Vector3.one * inputStrength, 2 * Time.deltaTime);
        }

        private void HandleRunAnimation()
        {
            _anim.SetInteger(RunSpeed, 0);

            if (_player.FrameInput.x == 0 || _isTrueWorld)
            {
                _footstepTimer = 0f;
                return;
            }

            _anim.SetInteger(RunSpeed, (int)_player.FrameInput.x);

            _footstepTimer -= Time.deltaTime;
            if (_footstepTimer <= 0f && _grounded)
            {
                var clip = _footsteps[Random.Range(0, 3)];
                _source.clip = clip;
                _source.Play();
                _footstepTimer = _footstepInterval;
            }
        }

        private void HandleCharacterTilt()
        {
            if (!_isTrueWorld) return;
            var runningTilt = _grounded ? Quaternion.Euler(0, 0, _maxTilt * _player.FrameInput.x) : Quaternion.identity;
            _anim.transform.up = Vector3.RotateTowards(_anim.transform.up, runningTilt * Vector2.up, _tiltSpeed * Time.deltaTime, 0f);
        }

        private void OnJumped()
        {
            _anim.SetTrigger(JumpKey);
            _anim.ResetTrigger(GroundedKey);

            if (_grounded)
            {
                SetColor(_jumpParticles);
                SetColor(_launchParticles);
                _jumpParticles.Play();
                _sfxSource.PlayOneShot(_footsteps[3]);
            }
        }

        private void OnGroundedChanged(bool grounded, float impact)
        {
            _grounded = grounded;

            if (grounded)
            {
                DetectGroundColor();
                SetColor(_landParticles);
                _anim.SetTrigger(GroundedKey);
                _sfxSource.PlayOneShot(_footsteps[4]);
                _moveParticles.Play();

                _landParticles.transform.localScale = Vector3.one * Mathf.InverseLerp(0, 40, impact);
                _landParticles.Play();
            }
            else
            {
                _moveParticles.Stop();
            }
        }

        private void DetectGroundColor()
        {
            var hit = Physics2D.Raycast(transform.position, Vector3.down, 2);
            if (!hit || hit.collider.isTrigger || !hit.transform.TryGetComponent(out SpriteRenderer r)) return;

            var color = r.color;
            _currentGradient = new ParticleSystem.MinMaxGradient(color * 0.9f, color * 1.2f);
            SetColor(_moveParticles);
        }

        private void SetColor(ParticleSystem ps)
        {
            var main = ps.main;
            main.startColor = _currentGradient;
        }

        private static readonly int GroundedKey = Animator.StringToHash("Grounded");
        private static readonly int IdleSpeedKey = Animator.StringToHash("IdleSpeed");
        private static readonly int JumpKey = Animator.StringToHash("Jump");
        private static readonly int RunSpeed = Animator.StringToHash("RunSpeed");
    }
}