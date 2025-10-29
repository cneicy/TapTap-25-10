using System;
using System.Collections;
using UnityEngine;
// 若不需要可去掉
using UnityEngine.Events;

namespace Acknowledgements
{
    public class AckPlayer : MonoBehaviour
    {
        public ScrollParallaxCompensator playerScrollParallaxCompensator;
        public AckSun ackSun;

        [Header("Move Settings")]
        [SerializeField] private float moveSpeed = 1450f;    // anchored 单位/秒
        [SerializeField] private float stopEpsilon = 0.5f;   // 认为到达的阈值（像素）
        [SerializeField] private bool useUnscaledTime = true; // 受不受 timescale 影响

        [Header("Events")]
        public UnityEvent onReachSunX; // Inspector 中可绑定的回调

        private Coroutine _moveRoutine;
        private Action _oneShotOnReached; // 代码中一次性回调

        private void Awake()
        {
            playerScrollParallaxCompensator = GetComponent<ScrollParallaxCompensator>();
        }

        /// <summary>
        /// 手动调用：开始水平移动到 AckSun 的 X 位置（同坐标系换算后）。
        /// </summary>
        public void StartMoveToAckSunX()
        {
            StartMoveToAckSunX(null);
        }

        /// <summary>
        /// 手动调用（带一次性回调）：到达后先触发 onReachSunX，再调用 onReached。
        /// </summary>
        public void StartMoveToAckSunX(Action onReached)
        {
            _oneShotOnReached = onReached;

            if (_moveRoutine != null) StopCoroutine(_moveRoutine);
            _moveRoutine = StartCoroutine(MoveToSunXCoroutine());
        }

        /// <summary>
        /// 手动调用：中途取消移动（不会触发回调）。
        /// </summary>
        public void CancelMoveToAckSunX()
        {
            if (_moveRoutine != null) StopCoroutine(_moveRoutine);
            _moveRoutine = null;
            _oneShotOnReached = null; // 取消后不再触发一次性回调
        }

        private IEnumerator MoveToSunXCoroutine()
        {
            var self = transform as RectTransform;
            if (self == null)
            {
                Debug.LogWarning("[AckPlayer] 该组件需要挂在 UI(RectTransform) 上。");
                yield break;
            }

            // 找到 AckSun 的 RectTransform（如果 AckSun 不是 UI，也能用它的 Transform）
            RectTransform sunRect = null;
            Transform sunTr = null;

            if (ackSun != null)
            {
                sunRect = ackSun.GetComponent<RectTransform>();
                sunTr = ackSun.transform;
            }

            if (sunTr == null)
            {
                Debug.LogWarning("[AckPlayer] 未指定 AckSun。");
                yield break;
            }

            // 初始目标 X（在本 UI 的父 RectTransform 坐标系下）
            if (!TryGetSunLocalX(self, sunRect ?? sunTr as RectTransform, sunTr, out var targetX))
            {
                Debug.LogWarning("[AckPlayer] 计算 AckSun 的本地 X 失败。");
                yield break;
            }

            // 若一开始就已经到达，则立刻触发回调并退出
            if (Mathf.Abs(self.anchoredPosition.x - targetX) <= stopEpsilon)
            {
                InvokeReachCallbacks();
                _moveRoutine = null;
                yield break;
            }

            var reached = false;

            // 往目标 X 移动
            while (true)
            {
                // 移动时若 AckSun 在移动，每帧重算目标 X（可选开关，这里默认开启）
                if (!TryGetSunLocalX(self, sunRect ?? sunTr as RectTransform, sunTr, out targetX))
                    break;

                var pos = self.anchoredPosition;
                var dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

                pos.x = Mathf.MoveTowards(pos.x, targetX, moveSpeed * dt);
                self.anchoredPosition = pos;

                if (Mathf.Abs(pos.x - targetX) <= stopEpsilon)
                {
                    reached = true;
                    break;
                }

                yield return null;
            }

            if (reached)
            {
                InvokeReachCallbacks();
            }

            _moveRoutine = null;
        }

        private void InvokeReachCallbacks()
        {
            try { onReachSunX?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); }

            // 取出并清空一次性回调，避免重复调用
            var cb = _oneShotOnReached;
            _oneShotOnReached = null;
            try { cb?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); }
        }
        
        private bool TryGetSunLocalX(RectTransform self, RectTransform sunRect, Transform sunTr, out float targetX)
        {
            targetX = 0f;
            var parentRect = self.parent as RectTransform;
            if (parentRect == null)
            {
                // 没父级时退化为本地坐标对比
                targetX = self.anchoredPosition.x;
                return false;
            }

            // Case A: 同一父物体下的两个 UI
            if (sunRect != null && sunRect.parent == parentRect)
            {
                targetX = sunRect.anchoredPosition.x;
                return true;
            }

            // Case B: 不同层级（或 AckSun 不是 UI）
            var canvas = self.GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera;

            // 用世界坐标 -> 屏幕坐标 -> 父 Rect 本地坐标
            var screenPoint = RectTransformUtility.WorldToScreenPoint(cam, sunTr.position);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, cam, out var localPoint))
            {
                targetX = localPoint.x;
                return true;
            }

            return false;
        }
    }
}
