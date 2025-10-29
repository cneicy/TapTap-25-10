using System.Collections;
using UnityEngine;

namespace Acknowledgements
{
    [ExecuteAlways]
    public class TVOffEffect : MonoBehaviour
    {
        [Header("Assign the shader above")]
        public Shader tvOffShader;
        [Range(0,1)] public float progress = 0f;
        public float playDuration = 0.6f;   // 动画时长
        public bool playOnStart = false;

        Material _mat;
        bool _isPlaying;
        static readonly int _ProgressID = Shader.PropertyToID("_Progress");

        void OnEnable()
        {
            if (tvOffShader == null) tvOffShader = Shader.Find("Hidden/Effects/TVOff");
            if (tvOffShader != null && _mat == null) _mat = new Material(tvOffShader) { hideFlags = HideFlags.DontSave };
            if (playOnStart && Application.isPlaying) Play();
        }

        void OnDisable()
        {
            if (_mat) DestroyImmediate(_mat);
        }

        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (_mat == null || tvOffShader == null || progress <= 0f)
            {
                Graphics.Blit(src, dst); // 不影响画面
                return;
            }

            _mat.SetFloat(_ProgressID, progress);
            Graphics.Blit(src, dst, _mat);
        }

        public void Play()
        {
            if (!isActiveAndEnabled) return;
            StopAllCoroutines();
            StartCoroutine(AnimateTo(1f, playDuration));
        }

        public void Reverse(float duration = 0.6f) // 类似“开机”
        {
            if (!isActiveAndEnabled) return;
            StopAllCoroutines();
            StartCoroutine(AnimateTo(0f, duration));
        }

        IEnumerator AnimateTo(float target, float duration)
        {
            _isPlaying = true;
            var start = progress;
            var t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, duration);
                // 平滑缓出：关机时体验更“电子”
                var s = Mathf.SmoothStep(0f, 1f, Mathf.SmoothStep(0f, 1f, t));
                progress = Mathf.Lerp(start, target, s);
                yield return null;
            }
            progress = target;
            _isPlaying = false;
        }
    }
}