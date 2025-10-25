
using System;
using System.Collections.Generic;
using UnityEngine;
using ShrinkEventBus;
using Game.Buff;
using Game.Player;
using Game.Mechanism; 
using EventBus = ShrinkEventBus.EventBus;

namespace Game.Mechanism
{
    public enum SpeedUpDirection { Up, Down, Left, Right }

    [RequireComponent(typeof(Collider2D))]
    public class SpeedTrack : MechanismBase
    {
        [Header("增加速度/增加速度衰减幅度/加速持续时间")]
        public float speedBoost = 12f;
        public float speedBoostFade = 6f;
        public float speedBoostDuration = 0.6f;

        [Header("当前施力方向(循环)")]
        public SpeedUpDirection speedUpDirection = SpeedUpDirection.Up;

        [Header("外部控制触发时是否推进方向（方案A开关）")]
        [SerializeField] private bool cycleOnControlledTrigger = true;

       
        private bool autoWireControllers = true;
        private Transform arrowVisual;
        private float gizmoLength = 1.2f;

        private Collider2D _col;

        #region Unity lifecycle

        protected override void Awake()
        {
            base.Awake();
            _col = GetComponent<Collider2D>();
            if (_col) _col.isTrigger = true; // 推荐作为触发器使用
            RefreshVisualDirection();
        }

        private void OnEnable()
        {
            if (autoWireControllers) AutoWireControllerEvents(subscribe: true);
        }

        private void OnDisable()
        {
            if (autoWireControllers) AutoWireControllerEvents(subscribe: false);
        }

        #endregion

        #region 与玩家交互（保留你原本逻辑）

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<Player.Player>();
            if (player == null) return;

            // 将当前方向与参数写入玩家的速度加成上下文
            player.SetSpeedBoostContext(speedBoost, speedBoostFade, speedBoostDuration, speedUpDirection);

            // 再触发 Buff（保持你原有事件总线逻辑）
            EventBus.TriggerEvent(new BuffAppliedEvent
            {
                Buff = new SpeedBoostBuff(),
                Player = player,
            });
        }

        #endregion

        #region 方案A：供控制器的 UnityEvent<MechanismBase> 调用

        /// <summary>
        /// 由 ButtonBar.OnEachAffected 或 TensionBar.OnEachToggled 回调。
        /// 控制器会把“当前被作用的目标 m”作为参数传入；只有当 m == this 时才推进方向。
        /// </summary>
        public void OnPerTargetAffected(MechanismBase target)
        {
            if (!cycleOnControlledTrigger) return;
            if (target == this)
            {
                AdvanceDirection();
            }
        }

        [ContextMenu("Advance Direction")]
        public void AdvanceDirection()
        {
            speedUpDirection = NextDirection(speedUpDirection);
            RefreshVisualDirection();
            // Debug.Log($"[SpeedTrack] Direction -> {speedUpDirection}");
        }

        private static SpeedUpDirection NextDirection(SpeedUpDirection cur)
        {
            switch (cur)
            {
                case SpeedUpDirection.Up:    return SpeedUpDirection.Down;
                case SpeedUpDirection.Down:  return SpeedUpDirection.Left;
                case SpeedUpDirection.Left:  return SpeedUpDirection.Right;
                default:                     return SpeedUpDirection.Up; // Right -> Up
            }
        }

        #endregion

        #region （可选）自动连线：免去 Inspector 拖事件

        private void AutoWireControllerEvents(bool subscribe)
        {
            // ButtonBar
            foreach (var bar in FindObjectsOfType<ButtonBar>())
            {
                if (!bar) continue;
                if (bar.targets != null && bar.targets.Contains(this))
                {
                    if (subscribe) bar.OnEachAffected.AddListener(OnPerTargetAffected);
                    else           bar.OnEachAffected.RemoveListener(OnPerTargetAffected);
                }
            }
            // TensionBar
            foreach (var lever in FindObjectsOfType<TensionBar>())
            {
                if (!lever) continue;
                if (lever.targets != null && lever.targets.Contains(this))
                {
                    if (subscribe) lever.OnEachToggled.AddListener(OnPerTargetAffected);
                    else           lever.OnEachToggled.RemoveListener(OnPerTargetAffected);
                }
            }
        }

        #endregion

        #region 可视反馈 & Gizmos

        private void RefreshVisualDirection()
        {
            if (!arrowVisual) return;
            var dir = DirToVector(speedUpDirection);
            if (dir.sqrMagnitude > 0f)
            {
                // 让箭头的 right 朝向当前方向（也可用 up，看你的模型朝向）
                arrowVisual.right = dir;
            }
        }

        private static Vector2 DirToVector(SpeedUpDirection d)
        {
            switch (d)
            {
                case SpeedUpDirection.Up:    return Vector2.up;
                case SpeedUpDirection.Down:  return Vector2.down;
                case SpeedUpDirection.Left:  return Vector2.left;
                default:                     return Vector2.right;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            var dir = DirToVector(speedUpDirection).normalized;
            Gizmos.DrawLine(Vector3.zero, (Vector3)(dir * gizmoLength));
            // 箭头
            var head = (Vector3)(dir * gizmoLength);
            var left = Quaternion.Euler(0, 0, 140) * dir;
            var right = Quaternion.Euler(0, 0, -140) * dir;
            Gizmos.DrawLine(head, head + (Vector3)(left * 0.3f));
            Gizmos.DrawLine(head, head + (Vector3)(right * 0.3f));
        }

        #endregion
    }
}
