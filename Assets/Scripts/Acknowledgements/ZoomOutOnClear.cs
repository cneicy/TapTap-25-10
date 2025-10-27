using System.Collections;
using UnityEngine;

namespace Acknowledgements
{
    public class ZoomOutOnClear : MonoBehaviour
    {
        public Camera cam;
        [Header("播放")]
        public float startDelay = 0f;
        public float duration = 4f;
        public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public bool ignoreTimeScale = true;

        public enum TargetMode { ByFactor, FitBounds }
        [Header("目标尺寸")]
        public TargetMode mode = TargetMode.ByFactor;
        public float factor = 2f;              // 按倍数放大（相对于当前size）
        public Transform boundsRoot;           // FitBounds用：放关卡物体的根节点
        public float padding = 1f;             // FitBounds用：边缘留白

        float _startSize, _targetSize;

        void Reset() { cam = Camera.main; }

        public void Play()
        {
            if (!cam) cam = Camera.main;
            _startSize = cam.orthographicSize;

            if (mode == TargetMode.ByFactor)
            {
                _targetSize = _startSize * Mathf.Max(1f, factor);
            }
            else
            {
                if (!boundsRoot)
                {
                    Debug.LogWarning("[ZoomOutOnClear] FitBounds 需要指定 boundsRoot");
                    return;
                }
                var b = GetCombinedBounds(boundsRoot);
                _targetSize = Mathf.Max(b.extents.y, b.extents.x / cam.aspect) + padding;
            }

            StopAllCoroutines();
            StartCoroutine(Co_Run());
        }

        IEnumerator Co_Run()
        {
            if (startDelay > 0)
            {
                if (ignoreTimeScale) yield return new WaitForSecondsRealtime(startDelay);
                else yield return new WaitForSeconds(startDelay);
            }

            float t = 0f;
            while (t < 1f)
            {
                t += (ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime) / Mathf.Max(0.0001f, duration);
                float k = curve.Evaluate(Mathf.Clamp01(t));
                cam.orthographicSize = Mathf.LerpUnclamped(_startSize, _targetSize, k);
                yield return null;
            }
            cam.orthographicSize = _targetSize;
        }

        public static Bounds GetCombinedBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var cols2D    = root.GetComponentsInChildren<Collider2D>(true);

            bool has = false;
            Bounds b = new Bounds(root.position, Vector3.zero);

            foreach (var r in renderers)
            {
                if (!has) { b = r.bounds; has = true; }
                else b.Encapsulate(r.bounds);
            }
            foreach (var c in cols2D)
            {
                if (!has) { b = c.bounds; has = true; }
                else b.Encapsulate(c.bounds);
            }

            if (!has) Debug.LogWarning("[ZoomOutOnClear] boundsRoot 下没有 Renderer/Collider2D，FitBounds 将无效。");
            return b;
        }
    }
}
