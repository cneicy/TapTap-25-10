using System.Collections;
using UnityEngine;
using UnityEngine.UI; // 若不需要可去掉

namespace Acknowledgements
{
    public class AckPlayer : MonoBehaviour
    {
        public ScrollParallaxCompensator playerScrollParallaxCompensator;
        public AckSun ackSun;

        [Header("Move Settings")]
        [SerializeField] private float moveSpeed = 800f;   // anchored 单位/秒
        [SerializeField] private float stopEpsilon = 0.5f; // 认为到达的阈值（像素）
        [SerializeField] private bool useUnscaledTime = true; // 受不受 timescale 影响

        private Coroutine _moveRoutine;

        private void Awake()
        {
            playerScrollParallaxCompensator = GetComponent<ScrollParallaxCompensator>();
        }

        /// <summary>
        /// 手动调用：开始水平移动到 AckSun 的 X 位置（同坐标系换算后）。
        /// </summary>
        public void StartMoveToAckSunX()
        {
            if (_moveRoutine != null) StopCoroutine(_moveRoutine);
            _moveRoutine = StartCoroutine(MoveToSunXCoroutine());
        }

        /// <summary>
        /// 手动调用：中途取消移动。
        /// </summary>
        public void CancelMoveToAckSunX()
        {
            if (_moveRoutine != null) StopCoroutine(_moveRoutine);
            _moveRoutine = null;
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

            // 目标 X（在本 UI 的父 RectTransform 坐标系下）
            if (!TryGetSunLocalX(self, sunRect ?? sunTr as RectTransform, sunTr, out float targetX))
            {
                Debug.LogWarning("[AckPlayer] 计算 AckSun 的本地 X 失败。");
                yield break;
            }

            // 往目标 X 移动
            while (true)
            {
                // 移动时如果 AckSun 在移动，也可以每帧重算目标 X（可选）
                if (!TryGetSunLocalX(self, sunRect ?? sunTr as RectTransform, sunTr, out targetX))
                    break;

                var pos = self.anchoredPosition;
                float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

                pos.x = Mathf.MoveTowards(pos.x, targetX, moveSpeed * dt);
                self.anchoredPosition = pos;

                if (Mathf.Abs(pos.x - targetX) <= stopEpsilon)
                    break;

                yield return null;
            }

            _moveRoutine = null;
        }

        /// <summary>
        /// 计算 AckSun 在当前 UI 父级坐标系下的 X。
        /// 1) 如果二者共享同一个父 RectTransform，直接使用 anchoredPosition.x。
        /// 2) 否则使用 RectTransformUtility 把 AckSun 世界坐标转换到当前父级的本地坐标。
        /// </summary>
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
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, sunTr.position);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPoint, cam, out var localPoint))
            {
                targetX = localPoint.x;
                return true;
            }

            return false;
        }
    }
}
