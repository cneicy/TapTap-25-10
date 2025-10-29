using System.Collections;
using UnityEngine;

namespace Game.Item
{
    public class ParachuteGrower : MonoBehaviour
    {
        [Header("Scale Settings")]
        public Vector3 initialScale = Vector3.zero;      // 初始缩放（通常 0 或很小）
        public Vector3 targetScale = Vector3.one;        // 最终缩放
        public float growDuration = 1.0f;                // 变大的时间（秒）
        public float startDelay = 0f;                    // 延时开始（秒）

        [Tooltip("缩放缓动曲线；为空则使用默认 SmoothStep：k*k*(3-2k)")]
        public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Options")]
        public bool playOnEnable = true;                 // 启用时自动播放
        public bool useUnscaledTime = false;             // 是否忽略 Time.timeScale

        private Coroutine _growRoutine;

        private void OnEnable()
        {
            if (playOnEnable)
            {
                RestartGrow();
            }
        }

        /// <summary>
        /// 重新播放成长动画（可在实例化后按需修改 targetScale/growDuration 再调用）
        /// </summary>
        public void RestartGrow()
        {
            if (_growRoutine != null) StopCoroutine(_growRoutine);
            _growRoutine = StartCoroutine(GrowCoroutine());
        }

        /// <summary>
        /// 立即设置为目标大小（不播放动画）
        /// </summary>
        public void SnapToTarget()
        {
            if (_growRoutine != null) StopCoroutine(_growRoutine);
            transform.localScale = targetScale;
        }

        /// <summary>
        /// 停止并销毁该降落伞（比如 Buff 移除时一起清理）
        /// </summary>
        public void StopAndDestroy()
        {
            if (_growRoutine != null) StopCoroutine(_growRoutine);
            Destroy(gameObject);
        }

        private IEnumerator GrowCoroutine()
        {
            // 初始化到初始缩放
            transform.localScale = initialScale;

            // 可选延迟
            if (startDelay > 0f)
                yield return Wait(startDelay);

            var t = 0f;
            var from = initialScale;
            var to   = targetScale;

            // 避免除零
            var dur = Mathf.Max(0.0001f, growDuration);

            while (t < dur)
            {
                t += Delta();
                var k = Mathf.Clamp01(t / dur);

                // 缓动
                var eased = easeCurve != null ? easeCurve.Evaluate(k) : (k * k * (3f - 2f * k));

                transform.localScale = Vector3.LerpUnclamped(from, to, eased);
                yield return null;
            }

            transform.localScale = to;
            _growRoutine = null;
        }
        
        private float Delta() => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        private WaitForSeconds Wait(float seconds) => new WaitForSeconds(seconds);
    }
}
