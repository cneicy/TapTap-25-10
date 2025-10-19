using UnityEngine;

namespace ScreenEffect
{
    [RequireComponent(typeof(ScreenGlitchManager))]
    public class MotionBlurFilter : MonoBehaviour
    {
        [Header("滤镜开关")]
        [SerializeField] private new bool enabled;
    
        [Header("强度参数")]
        [SerializeField, Range(0, 1)] private float intensity = 0.5f;
        [SerializeField, Range(0, 0.2f)] private float trailLength = 0.05f;
        [SerializeField, Range(2, 10)] private int trailCount = 5;
    
        private ScreenGlitchManager _manager;
        private Material _material;
    
        private static readonly int EnableID = Shader.PropertyToID("_EnableMotionBlur");
        private static readonly int IntensityID = Shader.PropertyToID("_MotionBlurIntensity");
        private static readonly int AmountID = Shader.PropertyToID("_MotionBlurAmount");
        private static readonly int CountID = Shader.PropertyToID("_TrailCount");

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
    
        public void SetTrailLength(float length)
        {
            trailLength = Mathf.Clamp(length, 0f, 0.2f);
            UpdateShaderParameters();
        }
    
        public void SetTrailCount(int count)
        {
            trailCount = Mathf.Clamp(count, 2, 10);
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
            _material.SetFloat(AmountID, trailLength);
            _material.SetFloat(CountID, trailCount);
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