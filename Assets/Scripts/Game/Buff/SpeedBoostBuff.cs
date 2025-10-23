// SpeedBoostBuff.cs
using System;
using UnityEngine;

namespace Game.Buff
{
    public class SpeedBoostBuff : BuffBase
    {
        public Vector2 InitialImpulse;     // 初始冲量（单位：速度）
        public float   DecayPerSecond = 20f;
        public bool    HorizontalOnly = true;
        public float   MaxExtraSpeed  = 12f;

        private Vector2 _extra;         // 当前应叠加到玩家上的额外速度
        private Vector2 _lastApplied;   // 上一帧实际叠加值，用于抵消
        private const float EPS = 1e-4f;

        // 运行期缓存
        private Player.PlayerController _pc;
        private Rigidbody2D _rb;

        public SpeedBoostBuff(Vector2 impulse, float decayPerSecond, bool horizontalOnly, float maxExtraSpeed)
        {
            BuffName        = "SpeedBoost#" + Guid.NewGuid().ToString("N");
            Duration        = 999f; 
            InitialImpulse  = impulse;
            DecayPerSecond  = decayPerSecond;
            HorizontalOnly  = horizontalOnly;
            MaxExtraSpeed   = maxExtraSpeed;
        }

        public override void OnApply(Player.Player target)
        {
            // 抓到 PlayerController / Rigidbody2D
            var root = (target as Component) ?? throw new Exception("SpeedBoostBuff target must be a Component");
            _pc = root.GetComponent<Player.PlayerController>() ?? root.GetComponentInChildren<Player.PlayerController>();
            _rb = root.GetComponent<Rigidbody2D>()            ?? root.GetComponentInChildren<Rigidbody2D>();

            var impulse = InitialImpulse;
            if (HorizontalOnly) impulse = new Vector2(impulse.x, 0f);
            if (impulse.sqrMagnitude > 0f)
            {
                _extra += impulse;
                if (MaxExtraSpeed > 0f) _extra = Vector2.ClampMagnitude(_extra, MaxExtraSpeed);
            }
        }

        public override void OnUpdate(Player.Player target, float deltaTime)
        {
            // 先抵消上帧
            if (_lastApplied.sqrMagnitude > EPS)
            {
                if (_pc != null)      _pc._frameVelocity -= _lastApplied;
                else if (_rb != null) _rb.linearVelocity       -= _lastApplied;
                _lastApplied = Vector2.zero;
            }

            // 叠加当前额外速度
            if (_extra.sqrMagnitude > EPS)
            {
                if (_pc != null)      _pc._frameVelocity += _extra;
                else if (_rb != null) _rb.linearVelocity       += _extra;
                _lastApplied = _extra;

                // 线性衰减
                float mag = _extra.magnitude;
                mag = Mathf.Max(0f, mag - DecayPerSecond * deltaTime);
                _extra = (mag > EPS) ? _extra.normalized * mag : Vector2.zero;
            }

            // 正常推进计时
            base.OnUpdate(target, deltaTime);

            // 当额外速度近似为 0 时，令 Buff 立刻过期（BuffManager 会在本帧移除）
            if (_extra.sqrMagnitude <= EPS)
            {
                Duration = 0f; // 因为 _timeElapsed 已递增 > 0，IsExpired 将为 true
            }
        }

        public override void OnRemove(Player.Player target)
        {
            if (_lastApplied.sqrMagnitude > EPS)
            {
                if (_pc != null)      _pc._frameVelocity -= _lastApplied;
                else if (_rb != null) _rb.linearVelocity       -= _lastApplied;
                _lastApplied = Vector2.zero;
            }
        }
    }
}
