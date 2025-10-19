using UnityEngine;
using UnityEngine.UI;

namespace ScreenEffect
{
    public class ScreenGlitchManager : MonoBehaviour
    {
        [Header("相机引用")]
        [SerializeField] private Camera targetCamera;
    
        [Header("滤镜材质")]
        [SerializeField] private Material filterMaterial;
    
        [Header("UI设置")]
        [SerializeField] private Canvas overlayCanvas;
        [SerializeField] private RawImage filterDisplay;
    
        [Header("渲染设置")]
        [SerializeField] private bool autoResolution = true;
        [SerializeField] private int renderTextureWidth = 1920;
        [SerializeField] private int renderTextureHeight = 1080;

        private RenderTexture _screenCapture;
        private RenderTexture _filteredOutput;
    
        public Material FilterMaterial { get; private set; }

        public bool IsActive { get; private set; }

        private void Awake()
        {
            if (!targetCamera)
            {
                targetCamera = Camera.main;
            }
        
            if (filterMaterial)
            {
                FilterMaterial = new Material(filterMaterial);
            }
            else
            {
                Debug.LogError("请指定滤镜材质！");
            }
        
            SetupUI();
            SetupRenderTextures();
        
            IsActive = false;
        }

        private void OnDestroy()
        {
            if (FilterMaterial)
            {
                Destroy(FilterMaterial);
            }
        
            CleanupRenderTextures();
        }

        private void LateUpdate()
        {
            if (!IsActive || !targetCamera || !FilterMaterial) return;
        
            CaptureAndApplyFilter();
        }
    
        /// <summary>
        /// 启用滤镜系统
        /// </summary>
        public void EnableFilters()
        {
            if (IsActive) return;
            IsActive = true;
            if (filterDisplay)
            {
                filterDisplay.enabled = true;
            }
            Debug.Log("滤镜系统已启用");
        }
    
        /// <summary>
        /// 禁用滤镜系统
        /// </summary>
        public void DisableFilters()
        {
            if (!IsActive) return;
            IsActive = false;
            if (filterDisplay)
            {
                filterDisplay.enabled = false;
            }
            Debug.Log("滤镜系统已禁用");
        }
    
        /// <summary>
        /// 切换滤镜开关
        /// </summary>
        public void ToggleFilters()
        {
            if (IsActive)
            {
                DisableFilters();
            }
            else
            {
                EnableFilters();
            }
        }
    
        private void SetupUI()
        {
            if (overlayCanvas && filterDisplay) return;
            var canvasObj = new GameObject("GlitchFilterCanvas");
            overlayCanvas = canvasObj.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 999;
            
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            var imageObj = new GameObject("FilterDisplay");
            imageObj.transform.SetParent(canvasObj.transform, false);
            
            filterDisplay = imageObj.AddComponent<RawImage>();
            var rt = filterDisplay.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            filterDisplay.material = FilterMaterial;
            filterDisplay.enabled = false;
        }
    
        private void SetupRenderTextures()
        {
            var width = autoResolution ? Screen.width : renderTextureWidth;
            var height = autoResolution ? Screen.height : renderTextureHeight;
        
            _screenCapture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            _screenCapture.Create();
        
            _filteredOutput = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            _filteredOutput.Create();
        
            if (filterDisplay)
            {
                filterDisplay.texture = _filteredOutput;
            }
        }
    
        private void CaptureAndApplyFilter()
        {
            if (!_screenCapture || !_filteredOutput || !FilterMaterial) return;
        
            var wasEnabled = filterDisplay && filterDisplay.enabled;
            if (filterDisplay)
            {
                filterDisplay.enabled = false;
            }
        
            var previousRT = targetCamera.targetTexture;
            targetCamera.targetTexture = _screenCapture;
            targetCamera.Render();
            targetCamera.targetTexture = previousRT;
        
            Graphics.Blit(_screenCapture, _filteredOutput, FilterMaterial);
        
            if (filterDisplay && wasEnabled)
            {
                filterDisplay.enabled = true;
            }
        }
    
        private void CleanupRenderTextures()
        {
            if (_screenCapture)
            {
                _screenCapture.Release();
                Destroy(_screenCapture);
                _screenCapture = null;
            }

            if (!_filteredOutput) return;
            _filteredOutput.Release();
            Destroy(_filteredOutput);
            _filteredOutput = null;
        }
    
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus || !autoResolution) return;
            CleanupRenderTextures();
            SetupRenderTextures();
        }
    }
}