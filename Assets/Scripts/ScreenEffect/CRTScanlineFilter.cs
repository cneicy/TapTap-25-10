using UnityEngine;

namespace ScreenEffect
{
    [RequireComponent(typeof(ScreenGlitchManager))]
    public class CRTScanlineFilter : MonoBehaviour
    {
        [Header("滤镜开关")]
        [SerializeField] private new bool enabled = true;

        [Header("扫描线参数")]
        [SerializeField, Range(0, 1)] private float intensity = 0.3f;
        [SerializeField, Range(100, 2000)] private float frequency = 800f;
        [SerializeField, Range(0, 2)] private float brightness = 1.0f;
        [SerializeField, Range(-5, 5)] private float speed = 1.0f;

        private ScreenGlitchManager _manager;
        private Material _material;

        private static readonly int EnableID = Shader.PropertyToID("_EnableScanline");
        private static readonly int IntensityID = Shader.PropertyToID("_ScanlineIntensity");
        private static readonly int FrequencyID = Shader.PropertyToID("_ScanlineFrequency");
        private static readonly int BrightnessID = Shader.PropertyToID("_ScanlineBrightness");
        private static readonly int SpeedID = Shader.PropertyToID("_ScanlineSpeed");

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

        public void Toggle()
        {
            SetEnabled(!enabled);
        }

        public void SetIntensity(float value)
        {
            intensity = Mathf.Clamp01(value);
            UpdateShaderParameters();
        }

        public void SetFrequency(float value)
        {
            frequency = Mathf.Clamp(value, 100f, 2000f);
            UpdateShaderParameters();
        }

        public void SetBrightness(float value)
        {
            brightness = Mathf.Clamp(value, 0f, 2f);
            UpdateShaderParameters();
        }

        public void SetSpeed(float value)
        {
            speed = Mathf.Clamp(value, -5f, 5f);
            UpdateShaderParameters();
        }
        
        public float GetBrightness()
        {
            return brightness;
        }

        private void UpdateShaderParameters()
        {
            if (!_material) return;
            _material.SetFloat(EnableID, enabled ? 1f : 0f);
            _material.SetFloat(IntensityID, intensity);
            _material.SetFloat(FrequencyID, frequency);
            _material.SetFloat(BrightnessID, brightness);
            _material.SetFloat(SpeedID, speed);
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
