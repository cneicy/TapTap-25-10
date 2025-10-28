using UnityEngine;

namespace ScreenEffect
{
    public class FilterBlock : MonoBehaviour
    {
        [Header("扫描线参数")]
        [SerializeField, Range(0, 1)] private float crtintensity = 0.3f;
        [SerializeField, Range(100, 2000)] private float frequency = 800f;
        [SerializeField, Range(0, 2)] private float brightness = 1.0f;
        [SerializeField, Range(-5, 5)] private float speed = 1.0f;
        
        [Header("分色")]
        [SerializeField, Range(0, 1)]
        public float chrointensity = 0.5f;
        [SerializeField, Range(0, 0.15f)] private float amount = 0.03f;
        
        [Header("鬼影强度参数")]
        [SerializeField, Range(0, 1)] private float ghointensity = 0.5f;
        [SerializeField, Range(2, 8)] private int ghostCount = 4;
        [SerializeField, Range(0, 0.1f)] private float ghostSpread = 0.03f;
        
        [Header("UV强度参数")]
        [SerializeField, Range(0, 1)] private float intensity = 0.5f;
        [SerializeField, Range(5, 100)] private float blockSize = 20f;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                var crt = FindAnyObjectByType<CRTScanlineFilter>();
                crt.SetIntensity(crtintensity);
                crt.SetBrightness(brightness);
                crt.SetSpeed(speed);
                var chro = FindAnyObjectByType<ChromaticAberrationFilter>();
                chro.SetIntensity(chrointensity);
                chro.SetAmount(amount);

                var gho = FindAnyObjectByType<GhostEffectFilter>();
                gho.SetIntensity(ghointensity);
                gho.SetGhostSpread(ghostSpread);
                gho.SetGhostCount(ghostCount);

                var uv = FindAnyObjectByType<UVDistortionFilter>();
                uv.SetIntensity(intensity);
                uv.SetBlockSize(brightness);

            }
        }
    }
}