using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Acknowledgements
{
    [RequireComponent(typeof(RectTransform))]
    public class AckSun : MonoBehaviour
    {
        public ScrollParallaxCompensator sunScrollParallaxCompensator;
        public AckPlayer ackPlayer;

        [Header("Rotate & Fall（默认参数，手动调用时使用）")]
        public float fallDistance = 250f;   // 向下移动距离（UI本地单位）
        public float fallDuration = 1.0f;   // 动画时长（秒）
        public float rotateDegrees = -360f; // Z轴旋转角度（逆时针为正）

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
            if (!sunScrollParallaxCompensator) sunScrollParallaxCompensator = GetComponent<ScrollParallaxCompensator>();
        }

        /// <summary>
        /// 使用“默认参数”手动播放旋转下落。
        /// </summary>
        public void PlayRotateFall()
        {
            PlayRotateFall(fallDistance, fallDuration, rotateDegrees);
        }

        /// <summary>
        /// 使用“本次调用的参数”手动播放旋转下落。
        /// </summary>
        public void PlayRotateFall(float distance, float duration, float degrees)
        {
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(RotateFallRoutine(distance, duration, degrees));
        }

        private IEnumerator RotateFallRoutine(float distance, float duration, float degrees)
        {
            IsFalling = true;

            // 动画期间临时关闭视差补偿，避免位置被改写；结束后再恢复并重绑为新基准
            bool reenableParallax = false;
            if (disableParallaxWhileFalling && sunScrollParallaxCompensator)
            {
                try { sunScrollParallaxCompensator.SetActive(false); }
                catch { sunScrollParallaxCompensator.active = false; }
                reenableParallax = true;
            }

            onFallStarted?.Invoke();

            Vector2 startPos = _rt.anchoredPosition;
            float startAngle = _rt.localEulerAngles.z;

            float t = 0f;
            float dur = Mathf.Max(0.0001f, duration);

            while (t < 1f)
            {
                float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                t = Mathf.Min(1f, t + dt / dur);
                
                float t01 = Mathf.Clamp01(t);
                
                float posK = (positionCurve != null) ? Mathf.Clamp01(positionCurve.Evaluate(t01)) : t01;
                float rotK = (rotationCurve != null) ? rotationCurve.Evaluate(t01) : t01;

                // 向下位移（锚点坐标系）
                _rt.anchoredPosition = new Vector2(
                    startPos.x,
                    startPos.y - distance * posK
                );

                // Z 轴旋转
                float angle = startAngle + degrees * rotK;
                _rt.localRotation = Quaternion.Euler(0f, 0f, angle);

                yield return null;
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

        // 可选：复位（若你需要）
        public void ResetTransform()
        {
            if (!_rt) _rt = GetComponent<RectTransform>();
            _rt.anchoredPosition = Vector2.zero;
            _rt.localRotation = Quaternion.identity;
        }
    }
}
