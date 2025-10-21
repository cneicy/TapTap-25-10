using System.Collections;
using UnityEngine;

namespace Game.Item
{
    /// <summary>
    /// 挂到【降落伞预制体】上：
    /// - 生成后从 initialScale 平滑缩放到 targetScale
    /// - 可通过 RestartGrow() 重新播放一次成长动画（用于切换道具又再生成等情况）
    /// - 可通过 StopAndDestroy() 立刻停止并销毁（比如 ParachuteBuff 移除时）
    /// </summary>
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

            float t = 0f;
            Vector3 from = initialScale;
            Vector3 to   = targetScale;

            // 避免除零
            float dur = Mathf.Max(0.0001f, growDuration);

            while (t < dur)
            {
                t += Delta();
                float k = Mathf.Clamp01(t / dur);

                // 缓动
                float eased = easeCurve != null ? easeCurve.Evaluate(k) : (k * k * (3f - 2f * k));

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
