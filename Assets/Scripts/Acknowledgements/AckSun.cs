using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Acknowledgements
{
    [RequireComponent(typeof(RectTransform))]
    public class AckSun : MonoBehaviour
    {
        public ScrollParallaxCompensator sunScrollParallaxCompensator;

        [Header("默认动画参数")]
        public float fallDuration = 1.0f;      // 动画时长（秒）
        public float rotateDegrees = -360f;    // Z轴旋转角度
        [Tooltip("下落的最终目标高度（UI 本地 anchored Y）")]
        public float targetLocalY = -350f;     // 目标 Y（本地/anchoredPosition.y）

        [Tooltip("使用不受 Time.timeScale 影响的时间")]
        public bool useUnscaledTime = true;

        [Tooltip("动画期间是否临时关闭 ScrollParallaxCompensator")]
        public bool disableParallaxWhileFalling = true;

        [Header("Easing（可选）")]
        public AnimationCurve positionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public AnimationCurve rotationCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("事件")]
        public UnityEvent onFallStarted;
        public UnityEvent onFallFinished;

        public bool IsFalling { get; private set; }

        private RectTransform _rt;
        private Coroutine _co;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            if (!sunScrollParallaxCompensator)
                sunScrollParallaxCompensator = GetComponent<ScrollParallaxCompensator>();
        }

        /// <summary>
        /// 使用默认字段：在 fallDuration 内把锚点 Y 移到 targetLocalY，并旋转 rotateDegrees。
        /// </summary>
        public void PlayFallToTargetY()
        {
            PlayFallToY(targetLocalY, fallDuration, rotateDegrees);
        }

        /// <summary>
        /// 指定“目标 Y / 时长 / 旋转角度”的一次性动画。
        /// </summary>
        public void PlayFallToY(float targetY, float duration, float degrees)
        {
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(FallToYRoutine(targetY, duration, degrees));
        }

        private IEnumerator FallToYRoutine(float targetY, float duration, float degrees)
        {
            IsFalling = true;

            bool reenableParallax = false;
            if (disableParallaxWhileFalling && sunScrollParallaxCompensator)
            {
                // 防止动画过程中被视差脚本改写位置
                try { sunScrollParallaxCompensator.SetActive(false); }
                catch { sunScrollParallaxCompensator.active = false; }
                reenableParallax = true;
            }

            onFallStarted?.Invoke();

            Vector2 startPos = _rt.anchoredPosition;
            float startAngle = _rt.localEulerAngles.z;
            float dur = Mathf.Max(0.0001f, duration);

            // 0 时长直接到位
            if (duration <= 0f)
            {
                _rt.anchoredPosition = new Vector2(startPos.x, targetY);
                _rt.localRotation = Quaternion.Euler(0f, 0f, startAngle + degrees);
            }
            else
            {
                float t = 0f;
                while (t < 1f)
                {
                    float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                    t = Mathf.Min(1f, t + dt / dur);
                    float t01 = Mathf.Clamp01(t);

                    // 曲线映射
                    float posK = (positionCurve != null) ? Mathf.Clamp01(positionCurve.Evaluate(t01)) : t01;
                    float rotK = (rotationCurve  != null) ? rotationCurve.Evaluate(t01) : t01;

                    // 位置：从 startY 插值到 targetY（只改 Y，保持 X）
                    float y = Mathf.LerpUnclamped(startPos.y, targetY, posK);
                    _rt.anchoredPosition = new Vector2(startPos.x, y);

                    // 旋转
                    float angle = startAngle + degrees * rotK;
                    _rt.localRotation = Quaternion.Euler(0f, 0f, angle);

                    yield return null;
                }

                // 结束时强制对齐到精确目标，避免浮点误差
                _rt.anchoredPosition = new Vector2(startPos.x, targetY);
                _rt.localRotation = Quaternion.Euler(0f, 0f, startAngle + degrees);
            }

            onFallFinished?.Invoke();

            if (reenableParallax && sunScrollParallaxCompensator)
            {
                // 恢复并以当前姿态为新基准，避免跳变
                try { sunScrollParallaxCompensator.SetActive(true, rebind: true, keepCurrentVisualPosition: true); }
                catch { sunScrollParallaxCompensator.active = true; sunScrollParallaxCompensator.BindNow(true); }
            }

            _co = null;
            IsFalling = false;
        }

        // 可选：复位
        public void ResetTransform()
        {
            if (!_rt) _rt = GetComponent<RectTransform>();
            _rt.anchoredPosition = Vector2.zero;
            _rt.localRotation = Quaternion.identity;
        }
    }
}
