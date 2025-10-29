using System.Collections;
using UnityEngine;

namespace Acknowledgements
{
    public class LongImageScrollHorizontalOnce : MonoBehaviour
    {
        public AckPlayer ackPlayer;
        public AckSun ackSun;
        
        public RectTransform viewport;
        public RectTransform longImage;

        [Header("速度（单位：RectTransform 本地单位/秒；World Space 下随 Canvas 缩放）")]
        public float speedUnitsPerSec = 1.0f;
        public bool leftToRight = true;
        public bool loop = false;
        public bool autoStart = true;

        public enum StopReference { ViewportRight, ScreenRight }

        [Header("停止条件")]
        public bool stopOnRightEdgeAlign = true;
        public StopReference stopReference = StopReference.ViewportRight;

        [Tooltip("Viewport 模式的容差（单位：viewport 本地单位）")]
        public float viewportEpsilon = 0.001f;

        [Tooltip("Screen 模式的容差（单位：像素）")]
        public float screenEpsilonPx = 1f;

        [Tooltip("用于 ScreenRight 判断的摄像机（为空则用 Camera.main）")]
        public Camera screenCamera;
        

        private Coroutine co;
        private static readonly Vector3[] _imgCornersWorld = new Vector3[4];
        private static readonly Vector3[] _vpCornersWorld  = new Vector3[4];

        void Start()
        {
            if (autoStart) StartScroll();
        }

        public void StartScroll()
        {
            if (co != null) StopCoroutine(co);
            co = StartCoroutine(ScrollRoutine());
        }

        IEnumerator ScrollRoutine()
        {
            // 约定：longImage 在 viewport 下，且 longImage 的锚点/枢轴为 (0, 0.5)（左中）
            var maxOffset = Mathf.Max(0f, longImage.rect.width - viewport.rect.width);

            var pos = longImage.anchoredPosition;
            pos.y = 0f;
            pos.x = leftToRight ? 0f : -maxOffset; // 从左→右：从最左开始滚到最右
            longImage.anchoredPosition = pos;

            // 内容未超出视口：不需要滚动，也不触发“到达终点”
            if (maxOffset <= 0f)
            {
                co = null;
                yield break;
            }

            var dir = leftToRight ? -1f : 1f; // 左→右视觉 = 图向左移动
            var reachedEnd = false;

            while (true)
            {
                var step = speedUnitsPerSec * Time.unscaledDeltaTime * dir;
                pos.x = Mathf.Clamp(pos.x + step, -maxOffset, 0f);
                longImage.anchoredPosition = pos;

                // 1) 右缘与参考对齐/越过（可选条件）
                if (stopOnRightEdgeAlign && IsRightEdgeAlignedOrPassed())
                {
                    reachedEnd = true;
                    break;
                }

                // 2) 兜底：滚动范围自然到头
                var doneByRange = leftToRight ? (pos.x <= -maxOffset) : (pos.x >= 0f);
                if (doneByRange)
                {
                    reachedEnd = true;
                    break;
                }

                yield return null;
            }

            // —— 只在真正到达终点时触发回调 —— 
            if (reachedEnd)
                OnReachedEnd();

            co = null;

            if (loop)
                StartScroll();
        }
        
        private void OnReachedEnd()
        {
            ackPlayer.playerScrollParallaxCompensator.active = false;
            ackSun.sunScrollParallaxCompensator.active = false;
            ackPlayer.StartMoveToAckSunX();
        }

        bool IsRightEdgeAlignedOrPassed()
        {
            switch (stopReference)
            {
                case StopReference.ScreenRight:
                    return IsRightEdgeAlignedOnScreen();
                case StopReference.ViewportRight:
                default:
                    return IsRightEdgeAlignedInViewportLocal();
            }
        }

        // —— 推荐：把角点变换到 viewport 的本地坐标系里，再比较 X —— 
        bool IsRightEdgeAlignedInViewportLocal()
        {
            if (!viewport || !longImage) return false;

            longImage.GetWorldCorners(_imgCornersWorld);

            var imgRightLocal = float.NegativeInfinity;
            for (var i = 0; i < 4; i++)
            {
                var local = viewport.InverseTransformPoint(_imgCornersWorld[i]);
                if (local.x > imgRightLocal) imgRightLocal = local.x;
            }

            var vpRightLocal = viewport.rect.xMax;
            return imgRightLocal <= vpRightLocal + viewportEpsilon;
        }

        // —— 可选：把长图角点投影到屏幕坐标，与屏幕右边界比 —— 
        bool IsRightEdgeAlignedOnScreen()
        {
            if (!longImage) return false;
            var cam = screenCamera ? screenCamera : Camera.main;
            if (!cam) return false;

            longImage.GetWorldCorners(_imgCornersWorld);

            var imgRightPx = float.NegativeInfinity;
            for (var i = 0; i < 4; i++)
            {
                Vector3 scr = RectTransformUtility.WorldToScreenPoint(cam, _imgCornersWorld[i]);
                if (scr.x > imgRightPx) imgRightPx = scr.x;
            }

            float screenRight = Screen.width;
            return imgRightPx <= screenRight + screenEpsilonPx;
        }
    }
}
