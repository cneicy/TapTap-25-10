using UnityEngine;

namespace ScreenEffect
{
    public class FilterController : MonoBehaviour
    {
        private ScreenGlitchManager _manager;
        private UVDistortionFilter _distortion;
        private ChromaticAberrationFilter _chromatic;
        private GhostEffectFilter _ghost;
        private ColorGlowFilter _glow;
        private MotionBlurFilter _motionBlur;
        private CRTScanlineFilter _scanline;


        private void Start()
        {
            var mainCamera = Camera.main;
        
            _manager = mainCamera?.GetComponent<ScreenGlitchManager>();
            _distortion = mainCamera?.GetComponent<UVDistortionFilter>();
            _chromatic = mainCamera?.GetComponent<ChromaticAberrationFilter>();
            _ghost = mainCamera?.GetComponent<GhostEffectFilter>();
            _glow = mainCamera?.GetComponent<ColorGlowFilter>();
            _motionBlur = mainCamera?.GetComponent<MotionBlurFilter>();
            _scanline = mainCamera?.GetComponent<CRTScanlineFilter>();

            if (_manager)
            {
                _manager.EnableFilters();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                _manager?.ToggleFilters();
            }
        
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                _distortion?.Toggle();
                Debug.Log("UV扰动: " + (_distortion?.enabled ?? false));
            }
        
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                _chromatic?.Toggle();
                Debug.Log("红蓝分色: " + (_chromatic?.enabled ?? false));
            }
        
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                _ghost?.Toggle();
                Debug.Log("重影: " + (_ghost?.enabled ?? false));
            }
        
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                _glow?.Toggle();
                Debug.Log("光晕: " + (_glow?.enabled ?? false));
            }
        
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                _motionBlur?.Toggle();
                Debug.Log("运动残影: " + (_motionBlur?.enabled ?? false));
            }
            if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                _scanline?.Toggle();
                Debug.Log("CRT扫描线: " + (_scanline?.enabled ?? false));
            }
        }
        
        public void OnToggleFilters()
        {
            _manager?.ToggleFilters();
        }
    
        public void OnDistortionToggle()
        {
            _distortion?.Toggle();
        }
    
        public void OnChromaticToggle()
        {
            _chromatic?.Toggle();
        }
    
        public void OnGhostToggle()
        {
            _ghost?.Toggle();
        }
    
        public void OnGlowToggle()
        {
            _glow?.Toggle();
        }
    
        public void OnMotionBlurToggle()
        {
            _motionBlur?.Toggle();
        }
    
        public void OnChromaticIntensitySlider(float value)
        {
            _chromatic?.SetIntensity(value);
        }
    
        public void OnMotionBlurIntensitySlider(float value)
        {
            _motionBlur?.SetIntensity(value);
        }
    
        public void OnDistortionIntensitySlider(float value)
        {
            _distortion?.SetIntensity(value);
        }
    }
}