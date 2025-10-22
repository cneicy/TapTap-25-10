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

        // ---------- 3个开始方法（按你的需求） ----------
        /// <summary>单程：从初始(A)走到目标(B)，结束。</summary>
        public void StartOnce(Direction dirEnum, float spd, float dist, float pauseEnds = 0f)
            => StartProcess(DirFromEnum(dirEnum), spd, dist, MotionMode.Once, pauseEnds);

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

        // 兼容你旧的调用习惯（可选）
        public virtual void StartMotion(Vector2 dir, float spd, float dist)
            => StartProcess(dir, spd, dist, MotionMode.Once, 0f);
        public virtual void StopMotion() => PauseProcess();
        public virtual void ResumeMotion() => ResumeProcess();

        // ---------- Unity ----------
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
            Vector2 legDir  = (LegIndex == 0) ? Dir : -Dir;
            Vector2 legTo   = (LegIndex == 0) ? _endPos : _startPos;

            float stepLen = Speed * Time.fixedDeltaTime;
            float moveLen = Mathf.Min(stepLen, LegRemain);

            if (moveLen <= 1e-6f)
            {
                // 抵达端点（对齐）
                rb.MovePosition(legTo);
                OnLegCompleted(LegIndex);

                if (Mode == MotionMode.Once)
                {
                    if (PauseAtEnds > 0f) _pauseTimer = PauseAtEnds;
                    CompleteProcess();                         // A→B 完成
                }
                else if (Mode == MotionMode.PingPongOnce)
                {
                    if (LegIndex == 0)                        // 刚完成 A→B → 切到 B→A
                    {
                        if (PauseAtEnds > 0f) _pauseTimer = PauseAtEnds;
                        LegIndex = 1;
                        LegMoved = 0f;
                    }
                    else                                      // 完成 B→A
                    {
                        if (PauseAtEnds > 0f) _pauseTimer = PauseAtEnds;
                        CompleteProcess();
                    }
                }
                else // Mode == Loop  (无限往返 A↔B)
                {
                    if (PauseAtEnds > 0f) _pauseTimer = PauseAtEnds;

                    if (LegIndex == 0)                        // 完成 A→B，下一腿 B→A
                    {
                        LegIndex = 1;
                        LegMoved = 0f;
                    }
                    else                                      // 完成 B→A，下一腿 A→B（完成一整循环）
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

        // ---------- Hooks（子类可重写） ----------
        protected virtual void OnProcessStarted()    { }
        protected virtual void OnProcessPaused()     { }
        protected virtual void OnProcessResumed()    { }
        protected virtual void OnLegCompleted(int legIndex) { } // 0=A→B, 1=B→A
        protected virtual void OnCycleLooped()       { }        // 仅 Loop
        protected virtual void OnProcessCompleted()  { }

        private void CompleteProcess()
        {
            if (Mode == MotionMode.Loop) return; // 理论上 Loop 不结束
            IsDone    = true;
            IsRunning = false;
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
