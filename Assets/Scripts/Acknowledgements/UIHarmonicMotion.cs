using UnityEngine;

namespace Acknowledgements
{
    [RequireComponent(typeof(RectTransform))]
    public class UIOrbitHarmonic : MonoBehaviour
    {
        public enum MotionMode
        {
            // x/y 各自是正弦运动（可做 Lissajous/椭圆/圆形，只要频率和相位合适）
            Lissajous,
            // 统一角速度，经典圆/椭圆（cos/sin 同一角速度）
            Circular
        }

        public enum CenterMode
        {
            // 以父物体(或指定 center)的锚点为中心（子物体 anchoredPosition 围绕 (0,0)）
            ParentAnchor,
            // 以启用时子物体的当前位置为中心（“就地”绕自身当前点转/摆）
            UseStartAsCenter
        }

        [Header("中心设置")]
        [Tooltip("可为空。为空时默认以父物体作为中心。强烈建议子物体的父亲就是这个中心。")]
        public RectTransform center;         // 通常不填，使用父物体
        public CenterMode centerMode = CenterMode.ParentAnchor;
        [Tooltip("在所选中心基础上的额外偏移（局部UI单位）。")]
        public Vector2 centerOffset = Vector2.zero;

        [Header("运动模式")]
        public MotionMode motionMode = MotionMode.Circular;

        [Header("Lissajous 参数（当模式= Lissajous 时有效）")]
        public Vector2 amplitude = new Vector2(80f, 40f);   // x/y 振幅
        [Min(0f)] public Vector2 frequency = new Vector2(1f, 1f); // Hz
        public Vector2 phaseDeg = new Vector2(0f, 90f);     // x/y 初相位（度）
        // 提示：若 amplitude.x==amplitude.y、frequency.x==frequency.y 且 phaseDeg.y - phaseDeg.x = 90°，即是标准圆

        [Header("Circular 参数（当模式= Circular 时有效）")]
        public Vector2 ellipseAxes = new Vector2(80f, 40f); // 椭圆半轴（等值则为圆）
        [Tooltip("角速度（度/秒）。1秒转 360° 则填 360。")]
        public float angularSpeedDeg = 180f;                // 角速度
        public float startAngleDeg = 0f;                    // 初始角度

        [Header("时间 & 衰减")]
        public bool useUnscaledTime = true; // 不受 Time.timeScale 影响（UI 常用）
        [Min(0f)] public float damping = 0f; // 指数衰减系数，0=不衰减

        [Header("控制")]
        public bool autoStart = true;
        public bool resetToCenterOnDisable = false;

        private RectTransform _rt;
        private RectTransform _centerRT;  // 实际使用的中心（优先 center，否则父节点）
        private Vector2 _startAsCenter;   // 若以“当前为中心”，记录启用时位置
        private float _t;
        private bool _running;

        void Awake()
        {
            _rt = GetComponent<RectTransform>();
        }

        void OnEnable()
        {
            // 选择中心：优先手动指定，否则用父物体
            _centerRT = center != null ? center : (_rt.parent as RectTransform);
            if (_centerRT == null)
            {
                Debug.LogError("[UIOrbitHarmonic] 没有可用中心：既未指定 center，父物体也不是 RectTransform。请确保这是一个 UI 元素的子物体。", this);
                enabled = false;
                return;
            }

            _t = 0f;
            _running = autoStart;
            _startAsCenter = _rt.anchoredPosition; // 记录启用时的相对父物体位置
        }

        void OnDisable()
        {
            if (resetToCenterOnDisable)
            {
                _rt.anchoredPosition = GetCenterBase() + centerOffset;
            }
        }

        void Update()
        {
            if (!_running) return;

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _t += dt;

            // 衰减因子
            float decay = damping > 0f ? Mathf.Exp(-damping * _t) : 1f;

            // 计算相对中心的位移（局部 UI 坐标）
            Vector2 rel;
            if (motionMode == MotionMode.Lissajous)
            {
                float wx = 2f * Mathf.PI * frequency.x;
                float wy = 2f * Mathf.PI * frequency.y;
                float px = phaseDeg.x * Mathf.Deg2Rad;
                float py = phaseDeg.y * Mathf.Deg2Rad;

                rel = new Vector2(
                    amplitude.x * Mathf.Sin(wx * _t + px),
                    amplitude.y * Mathf.Sin(wy * _t + py)
                );
            }
            else // Circular
            {
                float theta = startAngleDeg * Mathf.Deg2Rad + (angularSpeedDeg * Mathf.Deg2Rad) * _t;
                // 椭圆参数方程（等轴即圆）
                rel = new Vector2(
                    ellipseAxes.x * Mathf.Cos(theta),
                    ellipseAxes.y * Mathf.Sin(theta)
                );
            }

            rel *= decay;

            // 最终 anchoredPosition：中心基点 + 偏移 + 运动位移
            _rt.anchoredPosition = GetCenterBase() + centerOffset + rel;
        }

        /// <summary>
        /// 运行/暂停/重启
        /// </summary>
        public void Play()  { _running = true; }
        public void Pause() { _running = false; }
        public void Restart(float newStartAngleDeg = float.NaN)
        {
            _t = 0f;
            if (!float.IsNaN(newStartAngleDeg)) startAngleDeg = newStartAngleDeg;
            _running = true;
        }

        /// <summary>
        /// 将“当前子物体位置”设为新的中心（仅对 CenterMode=UseStartAsCenter 有意义）
        /// </summary>
        public void RecenterToCurrent()
        {
            _startAsCenter = _rt.anchoredPosition;
        }

        /// <summary>
        /// 以所选 CenterMode 计算“中心基点”
        /// </summary>
        private Vector2 GetCenterBase()
        {
            if (centerMode == CenterMode.ParentAnchor)
            {
                // 围绕父物体锚点 (0,0) 摆动/旋转
                return Vector2.zero;
            }
            else
            {
                // 围绕启用时的当前位置摆动/旋转（“就地”）
                return _startAsCenter;
            }
        }
    }
}
