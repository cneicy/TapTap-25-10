using Game.Level;
using UnityEngine;
using System.Collections;
using ScreenEffect;

namespace Menu
{
    public class ErrorPortal : MonoBehaviour
    {
        [Header("闪跳效果设置")]
        [SerializeField] private float jumpFrequency = 0.1f;
        [SerializeField] private float jumpIntensity = 0.5f;
        [SerializeField] private bool enableJumpEffect = true;
        
        [Header("颜色变化设置")]
        [SerializeField] private Color[] errorColors = {
            Color.red,
            new(1f, 0.5f, 0f),
            Color.yellow,
            new(0.5f, 0f, 1f),
            Color.cyan
        };
        [SerializeField] private float colorChangeSpeed = 2f;
        
        private Vector3 _originalPosition;
        private Coroutine _jumpCoroutine;
        private SpriteRenderer _spriteRenderer;

        private void Start()
        {
            _originalPosition = transform.position;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            
            if (enableJumpEffect)
            {
                _jumpCoroutine = StartCoroutine(ErrorJumpEffect());
            }
        }
        
        private void OnEnable()
        {
            if (enableJumpEffect && _jumpCoroutine == null)
            {
                _jumpCoroutine = StartCoroutine(ErrorJumpEffect());
            }
        }
        
        private void OnDisable()
        {
            if (_jumpCoroutine != null)
            {
                StopCoroutine(_jumpCoroutine);
                _jumpCoroutine = null;
            }
            
            transform.position = _originalPosition;
            ResetColor();
        }
        
        private IEnumerator ErrorJumpEffect()
        {
            var colorTimer = 0f;
            var currentColorIndex = 0;
            var nextColorIndex = 1;
            
            while (true)
            {
                var randomWait = UnityEngine.Random.Range(jumpFrequency * 0.3f, jumpFrequency * 1.5f);
                yield return new WaitForSeconds(randomWait);
                
                var randomIntensity = UnityEngine.Random.value > 0.8f ? jumpIntensity * 2.5f : 
                                       UnityEngine.Random.value > 0.5f ? jumpIntensity * 1.5f : jumpIntensity;
                
                var randomOffset = UnityEngine.Random.Range(-randomIntensity, randomIntensity);
                var newPosition = _originalPosition + new Vector3(randomOffset, 0f, 0f);
                transform.position = newPosition;
                
                if (_spriteRenderer && errorColors.Length > 1)
                {
                    colorTimer += Time.deltaTime * colorChangeSpeed;
                    var t = Mathf.PingPong(colorTimer, 1f);
                    
                    var currentColor = Color.Lerp(errorColors[currentColorIndex], errorColors[nextColorIndex], t);
                    _spriteRenderer.color = currentColor;
                    
                    if (colorTimer >= 1f)
                    {
                        colorTimer = 0f;
                        currentColorIndex = nextColorIndex;
                        nextColorIndex = (nextColorIndex + 1) % errorColors.Length;
                    }
                }
                
                if (UnityEngine.Random.value > 0.85f)
                {
                    yield return new WaitForSeconds(jumpFrequency * 2f);
                    transform.position = _originalPosition;
                    
                    if (_spriteRenderer)
                    {
                        var originalColor = _spriteRenderer.color;
                        _spriteRenderer.color = Color.white;
                        yield return new WaitForSeconds(0.1f);
                        _spriteRenderer.color = originalColor;
                    }
                }

                if (!(UnityEngine.Random.value > 0.9f)) continue;
                var shakeDuration = 0.3f;
                var shakeTimer = 0f;

                while (shakeTimer < shakeDuration)
                {
                    var shakeOffset = Mathf.Sin(shakeTimer * 50f) * jumpIntensity * 3f;
                    transform.position = _originalPosition + new Vector3(shakeOffset, 0f, 0f);
                        
                    if (_spriteRenderer)
                    {
                        _spriteRenderer.color = Color.Lerp(_spriteRenderer.color, Color.red, 0.3f);
                    }
                        
                    shakeTimer += Time.deltaTime;
                    yield return null;
                }
            }
        }
        
        private void ResetColor()
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = Color.white;
            }
        }
        
        private IEnumerator OnTriggerEnter2D(Collider2D other)
        {
            if(other.CompareTag("Player"))
            {
                SoundManager.Instance.Play("tp");
                RectTransitionController.Instance.StartTransition();
                yield return new WaitForSeconds(0.25f);
                LevelManager.Instance.SwitchLevel("Level1-1");
            }
        }
        
        public void SetJumpEffect(bool enabled)
        {
            enableJumpEffect = enabled;
            
            if (enabled && _jumpCoroutine == null)
            {
                _jumpCoroutine = StartCoroutine(ErrorJumpEffect());
            }
            else if (!enabled && _jumpCoroutine != null)
            {
                StopCoroutine(_jumpCoroutine);
                _jumpCoroutine = null;
                transform.position = _originalPosition;
                ResetColor();
            }
        }
        
        public void SetJumpParameters(float frequency, float intensity)
        {
            jumpFrequency = frequency;
            jumpIntensity = intensity;
        }
        
        [ContextMenu("测试剧烈错误效果")]
        private void TestIntenseErrorEffect()
        {
            SetJumpParameters(0.05f, 0.8f);
            if (!enableJumpEffect)
            {
                SetJumpEffect(true);
            }
        }
        
        [ContextMenu("停止错误效果")]
        private void StopErrorEffect()
        {
            SetJumpEffect(false);
        }
    }
}