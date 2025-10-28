using UnityEngine;

namespace Acknowledgements
{
    [ExecuteAlways]
    public class ScrollParallaxCompensator : MonoBehaviour
    {
        [Header("被滚动的目标（通常是 LongImage 的 RectTransform）")]
        public RectTransform moving;

        [Header("要做补偿的自己（留空=本物体）")]
        public RectTransform follower;

        [Tooltip("1=视觉静止；0.5=半速；0=不补偿；-1=同向翻倍")]
        [Range(-2f, 2f)] public float factor = 1f;

        public bool horizontal = true;
        public bool vertical   = false;

        [Header("开关")]
        public bool active = true; // 新增：外部可控总开关

        [Header("允许运行时移动（拖拽/动画/代码）")]
        public bool allowRuntimeMove = true;

        [Tooltip("外部手动移动判定阈值（单位：follower父节点的本地单位；World Space 下随Canvas缩放）")]
        public float rebindThresholdLocal = 0.5f;

        // —— 绑定时记录的基准 —— //
        Vector2 followerStartAnchored;               // follower 基准锚点坐标（在 followerParent 空间）
        Vector2 movingStartAnchored;                 // moving 基准锚点坐标（在 movingParent 空间）
        RectTransform movingParent;                  // moving 的父 Rect
        RectTransform followerParent;                // follower 的父 Rect

        // 运行时手动偏移（拖拽/动画）
        Vector2 manualOffset;

        // 我们上帧写入的最终位置（用于检测外部改动）
        Vector2 lastAppliedPos;
        bool hasLast;

        void Reset()
        {
            follower = GetComponent<RectTransform>();
        }

        void OnEnable()
        {
            if (!follower) follower = GetComponent<RectTransform>();
            BindNow(); // 以当前姿态为基准
        }

        /// <summary>
        /// 外部开关：on=true 开启；on=false 关闭。
        /// rebind=true 表示开启时将“当前姿态”作为新基准，避免跳变；
        /// keepCurrentVisualPosition=true 会保留当前视觉位置（不清空手动偏移）。
        /// </summary>
        public void SetActive(bool on, bool rebind = true, bool keepCurrentVisualPosition = true)
        {
            active = on;

            if (!active)
            {
                // 关闭时直接停止更新；下次开启前不记录外部改动
                hasLast = false;
                return;
            }

            // 开启
            if (rebind)
            {
                BindNow(keepCurrentVisualPosition);
            }
            else
            {
                // 不重绑：沿用旧基准，但同步 lastApplied，避免误判外部改动
                if (follower)
                {
                    lastAppliedPos = follower.anchoredPosition;
                    hasLast = true;
                }
            }
        }

        // 便捷API
        public void Enable(bool rebind = true, bool keepCurrentVisualPosition = true)
            => SetActive(true, rebind, keepCurrentVisualPosition);

        public void Disable() => SetActive(false);

        public void Toggle(bool? rebind = null, bool keepCurrentVisualPosition = true)
        {
            bool turningOn = !active;
            SetActive(turningOn, rebind ?? turningOn, keepCurrentVisualPosition);
        }

        /// <summary>
        /// 以“当前姿态”为新基准。默认保留当前视觉位置（不跳）。
        /// keepCurrentVisualPosition=false 时会清零 manualOffset。
        /// </summary>
        public void BindNow(bool keepCurrentVisualPosition = true)
        {
            if (!follower) follower = GetComponent<RectTransform>();

            movingParent  = moving       ? moving.parent        as RectTransform : null;
            followerParent= follower     ? follower.parent      as RectTransform : null;

            movingStartAnchored   = moving   ? moving.anchoredPosition   : Vector2.zero;

            if (keepCurrentVisualPosition)
            {
                // 令当前视觉位置成为基准：
                // final = followerStart - Convert(movingDelta) * factor + manualOffset
                // 选择 movingStart = moving.now → movingDelta=0 → followerStart = current - manualOffset
                followerStartAnchored = follower ? follower.anchoredPosition - manualOffset : Vector2.zero;
            }
            else
            {
                followerStartAnchored = follower ? follower.anchoredPosition : Vector2.zero;
                manualOffset          = Vector2.zero;
            }

            lastAppliedPos = follower ? follower.anchoredPosition : Vector2.zero;
            hasLast        = follower;
        }

        void OnRectTransformDimensionsChange()
        {
            // 编辑器下布局变化时，重绑一次避免漂移
            if (!Application.isPlaying) BindNow();
        }

        void LateUpdate()
        {
            if (!active) return;                  // 新增：外部开关
            if (!moving || !follower) return;

            // —— 1) 捕捉外部对 follower 的改动，作为手动偏移叠加 —— //
            if (allowRuntimeMove && hasLast)
            {
                Vector2 externalDelta = follower.anchoredPosition - lastAppliedPos; // 在 followerParent 本地单位
                if (externalDelta.sqrMagnitude > rebindThresholdLocal * rebindThresholdLocal)
                {
                    manualOffset += externalDelta;
                }
            }

            // —— 2) 将 moving 的 anchored 位移转换到 followerParent 空间 —— //
            Vector2 movingDeltaAnchored = moving.anchoredPosition - movingStartAnchored; // 在 movingParent 空间

            Vector3 worldDelta = Vector3.zero;
            if (horizontal)
            {
                Vector3 dxWorld = (movingParent ? movingParent.TransformVector(new Vector3(movingDeltaAnchored.x, 0f, 0f))
                                                : new Vector3(movingDeltaAnchored.x, 0f, 0f));
                worldDelta += dxWorld;
            }
            if (vertical)
            {
                Vector3 dyWorld = (movingParent ? movingParent.TransformVector(new Vector3(0f, movingDeltaAnchored.y, 0f))
                                                : new Vector3(0f, movingDeltaAnchored.y, 0f));
                worldDelta += dyWorld;
            }

            Vector3 followerParentLocal3 =
                followerParent ? followerParent.InverseTransformVector(worldDelta) : worldDelta;

            Vector2 followerParentLocalDelta = new Vector2(followerParentLocal3.x, followerParentLocal3.y);

            // —— 3) 计算最终位置：基准 - 转换后的位移 * factor + 手动偏移 —— //
            Vector2 basePos = followerStartAnchored - followerParentLocalDelta * factor;
            Vector2 final   = basePos + manualOffset;

            follower.anchoredPosition = final;
            lastAppliedPos = final;
            hasLast = true;
        }

        // —— 可选API：外部脚本直接设置/读取手动偏移 —— //
        public void SetManualOffset(Vector2 offset) => manualOffset = offset;
        public Vector2 GetManualOffset()            => manualOffset;

        /// <summary>在当前基础上微调（如拖拽时调用）。</summary>
        public void Nudge(Vector2 delta) => manualOffset += delta;
    }
}
