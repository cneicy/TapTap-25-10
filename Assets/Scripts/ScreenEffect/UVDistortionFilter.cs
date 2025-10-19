using UnityEngine;

namespace ScreenEffect
{
    [RequireComponent(typeof(ScreenGlitchManager))]
    public class UVDistortionFilter : MonoBehaviour
    {
        [Header("滤镜开关")]
        [SerializeField] private new bool enabled;
    
        [Header("强度参数")]
        [SerializeField, Range(0, 1)] private float intensity = 0.5f;
        [SerializeField, Range(5, 100)] private float blockSize = 20f;
    
        private ScreenGlitchManager _manager;
        private Material _material;
    
        private static readonly int EnableID = Shader.PropertyToID("_EnableDistortion");
        private static readonly int IntensityID = Shader.PropertyToID("_DistortionIntensity");
        private static readonly int BlockSizeID = Shader.PropertyToID("_BlockSize");

        private void Awake()
        {
            _manager = GetComponent<ScreenGlitchManager>();
        }

        private void Start()
        {
            _material = _manager.FilterMaterial;
            UpdateShaderParameters();
        }
    
        public void SetEnabled(bool value)
        {
            enabled = value;
            UpdateShaderParameters();
        }
    
        public void SetIntensity(float value)
        {
            intensity = Mathf.Clamp01(value);
            UpdateShaderParameters();
        }
    
        public void SetBlockSize(float value)
        {
            blockSize = Mathf.Clamp(value, 5f, 100f);
            UpdateShaderParameters();
        }
    
        public void Toggle()
        {
            SetEnabled(!enabled);
        }
    
        private void UpdateShaderParameters()
        {
            if (!_material) return;
        
            _material.SetFloat(EnableID, enabled ? 1f : 0f);
            _material.SetFloat(IntensityID, intensity);
            _material.SetFloat(BlockSizeID, blockSize);
        }

        private void OnValidate()
        {
            if (Application.isPlaying && _material)
            {
                UpdateShaderParameters();
            }
        }
    }
}