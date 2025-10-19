using UnityEngine;

namespace ScreenEffect
{
    [RequireComponent(typeof(ScreenGlitchManager))]
    public class GhostEffectFilter : MonoBehaviour
    {
        [Header("滤镜开关")]
        [SerializeField] private new bool enabled;
    
        [Header("强度参数")]
        [SerializeField, Range(0, 1)] private float intensity = 0.5f;
        [SerializeField, Range(2, 8)] private int ghostCount = 4;
        [SerializeField, Range(0, 0.1f)] private float ghostSpread = 0.03f;
    
        private ScreenGlitchManager _manager;
        private Material _material;
    
        private static readonly int EnableID = Shader.PropertyToID("_EnableGhost");
        private static readonly int IntensityID = Shader.PropertyToID("_GhostIntensity");
        private static readonly int CountID = Shader.PropertyToID("_GhostCount");
        private static readonly int SpreadID = Shader.PropertyToID("_GhostSpread");

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
    
        public void SetGhostCount(int count)
        {
            ghostCount = Mathf.Clamp(count, 2, 8);
            UpdateShaderParameters();
        }
    
        public void SetGhostSpread(float spread)
        {
            ghostSpread = Mathf.Clamp(spread, 0f, 0.1f);
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
            _material.SetFloat(CountID, ghostCount);
            _material.SetFloat(SpreadID, ghostSpread);
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