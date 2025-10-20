using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Utils;

namespace ScreenEffect
{
    [RequireComponent(typeof(RawImage))]
    public class GlitchTransitionController : Singleton<GlitchTransitionController>
    {
        [Header("相机引用")]
        [SerializeField] private Camera targetCamera;
    
        [Header("动画参数")]
        [SerializeField] private float transitionDuration = 2.5f;
        [SerializeField] private AnimationCurve progressCurve = AnimationCurve.Linear(0, 0, 1, 1);
    
        [Header("故障效果参数")]
        [SerializeField, Range(0, 1)] private float glitchIntensity = 0.8f;
        [SerializeField, Range(0, 0.15f)] private float chromaticAmount = 0.06f;
        [SerializeField, Range(5, 100)] private float blockSize = 20f;
    
        [Header("渲染设置")]
        [SerializeField] private int renderTextureWidth = 1920;
        [SerializeField] private int renderTextureHeight = 1080;
        [SerializeField] private bool autoResolution = true;
    
        [Header("音效")]
        [SerializeField] private AudioClip glitchSound;
        [SerializeField] private AudioSource audioSource;
    
        private RawImage _rawImage;
        private Material _material;
        private RenderTexture _renderTexture;
        private RenderTexture _originalTargetTexture;
        private bool _isTransitioning;
    
        private static readonly int ProgressID = Shader.PropertyToID("_Progress");
        private static readonly int GlitchIntensityID = Shader.PropertyToID("_GlitchIntensity");
        private static readonly int ChromaticAmountID = Shader.PropertyToID("_ChromaticAmount");
        private static readonly int BlockSizeID = Shader.PropertyToID("_BlockSize");

        protected override void Awake()
        {
            _rawImage = GetComponent<RawImage>();
            
            if (!targetCamera)
            {
                targetCamera = Camera.main;
                if (!targetCamera)
                {
                    Debug.LogError("未找到相机！请在Inspector中指定targetCamera或确保场景中有MainCamera");
                }
            }
            
            if (_rawImage.material)
            {
                _material = new Material(_rawImage.material);
                _rawImage.material = _material;
            }
            else
            {
                Debug.LogError("请先为RawImage指定使用GlitchTransitionV2 Shader的材质！");
            }
            
            if (!audioSource)
            {
                audioSource = GetComponent<AudioSource>();
                if (!audioSource && glitchSound)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
        
            _rawImage.enabled = false;
            UpdateShaderParameters();
        }

        private void OnDestroy()
        {
            CleanupRenderTexture();
            if (_material)
            {
                Destroy(_material);
            }
        }
    
        /// <summary>
        /// 开始转场
        /// </summary>
        public void StartTransition()
        {
            if (!_isTransitioning)
            {
                StartCoroutine(TransitionCoroutine(null, null));
            }
        }
    
        /// <summary>
        /// 开始转场（带回调）
        /// </summary>
        public void StartTransition(System.Action onMiddle, System.Action onComplete)
        {
            if (!_isTransitioning)
            {
                StartCoroutine(TransitionCoroutine(onMiddle, onComplete));
            }
        }
    
        private IEnumerator TransitionCoroutine(System.Action onMiddle, System.Action onComplete)
        {
            if (!_material || !targetCamera) yield break;
        
            _isTransitioning = true;
            
            SetupRenderTexture();
            CaptureCamera();
        
            _rawImage.enabled = true;

            if (audioSource && glitchSound)
            {
                audioSource.PlayOneShot(glitchSound);
            }
        
            var elapsed = 0f;
            var middleCallbackInvoked = false;
        
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                var normalizedTime = elapsed / transitionDuration;
                var progress = progressCurve.Evaluate(normalizedTime);
            
                _material.SetFloat(ProgressID, progress);
            
                // 在黑屏阶段调用中间回调（0.35-0.65）
                if (!middleCallbackInvoked && progress >= 0.5f)
                {
                    middleCallbackInvoked = true;
                    onMiddle?.Invoke();
                }
            
                yield return null;
            }
        
            _material.SetFloat(ProgressID, 1f);
            yield return new WaitForSeconds(0.05f);
            
            _rawImage.enabled = false;
            RestoreCamera();
            CleanupRenderTexture();
            _isTransitioning = false;
        
            _material.SetFloat(ProgressID, 0f);
            onComplete?.Invoke();
        }
    
        /// <summary>
        /// 设置RenderTexture
        /// </summary>
        private void SetupRenderTexture()
        {
            var width = autoResolution ? Screen.width : renderTextureWidth;
            var height = autoResolution ? Screen.height : renderTextureHeight;
        
            _renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            _renderTexture.Create();
        
            _rawImage.texture = _renderTexture;
        }
    
        /// <summary>
        /// 捕获相机画面
        /// </summary>
        private void CaptureCamera()
        {
            if (!targetCamera || !_renderTexture) return;
            
            _originalTargetTexture = targetCamera.targetTexture;
            
            targetCamera.targetTexture = _renderTexture;
            targetCamera.Render();
        }
    
        /// <summary>
        /// 恢复相机设置
        /// </summary>
        private void RestoreCamera()
        {
            if (targetCamera)
            {
                targetCamera.targetTexture = _originalTargetTexture;
            }
        }
    
        /// <summary>
        /// 清理RenderTexture
        /// </summary>
        private void CleanupRenderTexture()
        {
            if (_renderTexture)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
                _renderTexture = null;
            }
            _rawImage.texture = null;
        }
    
        private void UpdateShaderParameters()
        {
            if (!_material) return;
        
            _material.SetFloat(GlitchIntensityID, glitchIntensity);
            _material.SetFloat(ChromaticAmountID, chromaticAmount);
            _material.SetFloat(BlockSizeID, blockSize);
        }
    
        public void SetDuration(float duration)
        {
            transitionDuration = Mathf.Max(0.1f, duration);
        }
    
        public void SetGlitchIntensity(float intensity)
        {
            glitchIntensity = Mathf.Clamp01(intensity);
            UpdateShaderParameters();
        }
    
        public void SetChromaticAmount(float amount)
        {
            chromaticAmount = Mathf.Clamp(amount, 0f, 0.15f);
            UpdateShaderParameters();
        }
    
        public void SetBlockSize(float size)
        {
            blockSize = Mathf.Clamp(size, 5f, 100f);
            UpdateShaderParameters();
        }
    
        public void StopTransition()
        {
            StopAllCoroutines();
            _rawImage.enabled = false;
            RestoreCamera();
            CleanupRenderTexture();
            _isTransitioning = false;
            if (_material)
            {
                _material.SetFloat(ProgressID, 0f);
            }
        }

        private void OnValidate()
        {
            UpdateShaderParameters();
        }
    }
}