using UnityEngine;

namespace ScreenEffect
{
    [RequireComponent(typeof(ScreenGlitchManager))]
    public class ChromaticAberrationFilter : MonoBehaviour
    {
        [Header("滤镜开关")]
        [SerializeField] private new bool enabled;
    
        [Header("强度参数")]
        [SerializeField, Range(0, 1)]
        public float intensity = 0.5f;
        [SerializeField, Range(0, 0.15f)] private float amount = 0.03f;
    
        private ScreenGlitchManager _manager;
        private Material _material;
    
        private static readonly int EnableID = Shader.PropertyToID("_EnableChromatic");
        private static readonly int IntensityID = Shader.PropertyToID("_ChromaticIntensity");
        private static readonly int AmountID = Shader.PropertyToID("_ChromaticAmount");

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
    
        public void SetAmount(float value)
        {
            amount = Mathf.Clamp(value, 0f, 0.15f);
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
            _material.SetFloat(AmountID, amount);
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