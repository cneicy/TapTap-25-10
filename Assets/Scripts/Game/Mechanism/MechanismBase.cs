using UnityEngine;

namespace Game.Mechanism
{
    public enum Direction   { Up, Down, Left, Right }
    public enum MotionMode  { Once, Loop, PingPongOnce }

    [RequireComponent(typeof(Rigidbody2D))]
    public class MechanismBase : MonoBehaviour
    {
        [Header("Physics")]
        [SerializeField] protected Rigidbody2D rb;

        [Tooltip("启用时自动按下面参数启动进程")]
        [Header("是否在游戏开始时就开始移动")]
        public bool autoStartOnEnable = false;

        [Header("方向/速度/距离")]
        public Direction direction = Direction.Right;
        public float     speed     = 2f;
        public float     distance  = 3f;

        [Header("移动方式")]
        public MotionMode mode     = MotionMode.Once;

        [Header("抵达端点后的停顿(秒)")]
        public float    pauseAtEnds = 0f;

        // ===== 运行期（进程）状态 =====
        public Vector2    Dir        { get; protected set; } // 正向单位方向（A→B）
        public float      Speed      { get; protected set; } // 单位/秒
        public float      LegLength  { get; protected set; } // 单腿距离（A→B）
        public MotionMode Mode       { get; protected set; }
        public float      PauseAtEnds{ get; protected set; }

        public bool  IsRunning  { get; protected set; } // 进程是否在跑
        public bool  IsPaused   { get; protected set; } // 是否手动暂停
        public bool  IsDone     { get; protected set; } // 进程完成（Once/PingPongOnce）
        public int   LegIndex   { get; protected set; } // 0=A→B, 1=B→A
        public float LegMoved   { get; protected set; } // 当前腿已走距离
        public float LegRemain  => Mathf.Max(0f, LegLength - LegMoved);

        // 端点缓存
        private Vector2 _startPos;   // A
        private Vector2 _endPos;     // B
        private float   _pauseTimer; // 端点停顿计时

        // ================== 【新增】Once 的锚点与方向记忆 ==================
        [Header("Once 模式（A↔B）")]
        [Tooltip("true=下一次 Once 从A到B；false=下一次 Once 从B到A")]
        public bool onceNextIsAToB = true;        // 用这个bool区分“进程1/进程2”

        // 一旦首次 StartOnce，会锁定 A/B 锚点；后续 StartOnce 在这两个点之间往返
        private bool   _onceAnchored;
        private Vector2 _onceA, _onceB;
        private Vector2 _onceDir;                 // A→B 的方向（单位向量）
        private float   _onceLen;                 // A↔B 的距离

        /// <summary>（可选）重置 Once 的 A/B 锚点，下次 StartOnce 会以当前为A重新锚定。</summary>
        public void ResetOnceAnchors() { _onceAnchored = false; }
        // ===================================================================

        // ---------- 3个开始方法（按你的需求） ----------
        /// <summary>单程：从初始(A)走到目标(B)，结束。</summary>
        // 【修改】实现 Once 的 A↔B 往返（用 onceNextIsAToB 控制）
        public void StartOnce(Direction dirEnum, float spd, float dist, float pauseEnds = 0f)
        {
            var desiredDir = DirFromEnum(dirEnum);
            if (!rb) rb = GetComponent<Rigidbody2D>();

            // 第一次 StartOnce：用当前位置作为A，锚定A/B
            if (!_onceAnchored)
            {
                var a = rb.position;
                var d = (desiredDir.sqrMagnitude < 1e-6f) ? Vector2.right : desiredDir.normalized;
                var   L = Mathf.Abs(dist);
                if (L < 1e-6f) { CancelProcess(); return; }

                _onceA     = a;
                _onceDir   = d;
                _onceLen   = L;
                _onceB     = _onceA + _onceDir * _onceLen;
                _onceAnchored = true;
                // 初次默认 A→B（onceNextIsAToB 的初值默认为 true）
            }

            // 选择本次的起点/方向
            var start = onceNextIsAToB ? _onceA : _onceB;
            var runDir= onceNextIsAToB ? _onceDir : -_onceDir;

            // 把刚体放到起点，保证精确从锚点出发（避免累计误差/被外力挪走）
            rb.position = start;

            // 走一次（把 StartProcess 当作低层推进器来用）
            StartProcess(runDir, spd, _onceLen, MotionMode.Once, pauseEnds);
        }

        public void StartOnce(float pauseEnds = 0f)
            => StartOnce(direction, speed, distance, pauseEnds);

        /// <summary>乒乓一次：A→B→A，结束。</summary>
        public void StartPingPongOnce(Direction dirEnum, float spd, float dist, float pauseEnds = 0f)
            => StartProcess(DirFromEnum(dirEnum), spd, dist, MotionMode.PingPongOnce, pauseEnds);

        public void StartPingPongOnce(float pauseEnds = 0f)
            => StartPingPongOnce(direction, speed, distance, pauseEnds);

        /// <summary>循环：A→B→A→B… 无限往返。</summary>
        public void StartLoop(Direction dirEnum, float spd, float dist, float pauseEnds = 0f)
            => StartProcess(DirFromEnum(dirEnum), spd, dist, MotionMode.Loop, pauseEnds);

        public void StartLoop(float pauseEnds = 0f)
            => StartLoop(direction, speed, distance, pauseEnds);

        // ---------- 原有通用入口（仍可用） ----------
        /// <summary>从“当前位置”为 A，按枚举方向启动一个进程。</summary>
        public virtual void StartProcess(Direction dirEnum, float spd, float dist, MotionMode m, float pauseEnds = 0f)
            => StartProcess(DirFromEnum(dirEnum), spd, dist, m, pauseEnds);

        /// <summary>从“当前位置”为 A，按向量方向启动一个进程。</summary>
        public virtual void StartProcess(Vector2 direction, float spd, float dist, MotionMode m, float pauseEnds = 0f)
        {
            if (!rb) rb = GetComponent<Rigidbody2D>();
            if (direction.sqrMagnitude < 1e-6f || spd <= 0f || Mathf.Abs(dist) < 1e-6f)
            { CancelProcess(); return; }

            Dir         = direction.normalized;
            Speed       = spd;
            LegLength   = Mathf.Abs(dist);
            Mode        = m;
            PauseAtEnds = Mathf.Max(0f, pauseEnds);

            _startPos   = rb.position;
            _endPos     = _startPos + Dir * LegLength;

            LegIndex = 0;         // 先走 A→B
            LegMoved = 0f;
            _pauseTimer = 0f;

            IsPaused  = false;
            IsDone    = false;
            IsRunning = true;

            OnProcessStarted();
        }

        /// <summary>暂停（不重置进度）。</summary>
        public virtual void PauseProcess()
        {
            if (!IsRunning || IsPaused) return;
            IsPaused = true;
            OnProcessPaused();
        }

        /// <summary>继续（从暂停处继续）。</summary>
        public virtual void ResumeProcess()
        {
            if (!IsRunning || !IsPaused) return;
            IsPaused = false;
            OnProcessResumed();
        }

        /// <summary>取消并清空状态（不移动）。</summary>
        public virtual void CancelProcess()
        {
            IsRunning = false;
            IsPaused  = false;
            IsDone    = false;
            LegMoved  = 0f;
            _pauseTimer = 0f;
        }

        /// <summary>从当前位置用同一套参数重新开始（A=当前位置）。</summary>
        public virtual void RestartProcess()
            => StartProcess(Dir, Speed, LegLength, Mode, PauseAtEnds);
        
        protected virtual void Awake()
        {
            if (!rb) rb = GetComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            if (rb.interpolation == RigidbodyInterpolation2D.None)
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        protected virtual void OnEnable()
        {
            if (!autoStartOnEnable) return;

            switch (mode)
            {
                case MotionMode.Once:           StartOnce(pauseAtEnds); break;
                case MotionMode.PingPongOnce:   StartPingPongOnce(pauseAtEnds); break;
                case MotionMode.Loop:           StartLoop(pauseAtEnds); break;
            }
        }

        protected virtual void FixedUpdate()
        {
            if (!IsRunning || IsPaused || IsDone) return;

            // 端点停顿
            if (_pauseTimer > 0f)
            {
                _pauseTimer -= Time.fixedDeltaTime;
                if (_pauseTimer > 0f) return;
            }

            // 当前腿数据
            var legDir  = (LegIndex == 0) ? Dir : -Dir;
            var legTo   = (LegIndex == 0) ? _endPos : _startPos;

            var stepLen = Speed * Time.fixedDeltaTime;
            var moveLen = Mathf.Min(stepLen, LegRemain);

            if (moveLen <= 1e-6f)
            {
                // 抵达端点（对齐）
                rb.MovePosition(legTo);
                OnLegCompleted(LegIndex);

                if (Mode == MotionMode.Once)
                {
                    if (PauseAtEnds > 0f) _pauseTimer = PauseAtEnds;
                    CompleteProcess();                         // A→B（或 B→A）完成
                }
                else if (Mode == MotionMode.PingPongOnce)
                {
                    if (LegIndex == 0)
                    {
                        if (PauseAtEnds > 0f) _pauseTimer = PauseAtEnds;
                        LegIndex = 1;
                        LegMoved = 0f;
                    }
                    else
                    {
                        if (PauseAtEnds > 0f) _pauseTimer = PauseAtEnds;
                        CompleteProcess();
                    }
                }
                else // Loop
                {
                    if (PauseAtEnds > 0f) _pauseTimer = PauseAtEnds;

                    if (LegIndex == 0)
                    {
                        LegIndex = 1;
                        LegMoved = 0f;
                    }
                    else
                    {
                        LegIndex = 0;
                        LegMoved = 0f;
                        OnCycleLooped();
                    }
                }
                return;
            }

            // 正常推进
            rb.MovePosition(rb.position + legDir * moveLen);
            LegMoved += moveLen;
        }
        
        protected virtual void OnProcessStarted()    { }
        protected virtual void OnProcessPaused()     { }
        protected virtual void OnProcessResumed()    { }
        protected virtual void OnLegCompleted(int legIndex) { } // 0=A→B, 1=B→A
        protected virtual void OnCycleLooped()       { }        // 仅 Loop
        protected virtual void OnProcessCompleted()  { }

        // 【修改】Once 完成后自动切换下一次方向（A→B ↔ B→A）
        private void CompleteProcess()
        {
            if (Mode == MotionMode.Loop) return; // 理论上 Loop 不结束
            IsDone    = true;
            IsRunning = false;

            if (Mode == MotionMode.Once)
            {
                // 完成一次单程后，切换“下一次”的方向
                onceNextIsAToB = !onceNextIsAToB;
            }

            OnProcessCompleted();
        }

        protected static Vector2 DirFromEnum(Direction d)
            => d switch
            {
                Direction.Up    => Vector2.up,
                Direction.Down  => Vector2.down,
                Direction.Left  => Vector2.left,
                _               => Vector2.right,
            };
    }
}
