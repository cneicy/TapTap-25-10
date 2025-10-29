using System;
using Game.Buff;
using Game.Mechanism;
using ShrinkEventBus;
using UnityEngine;

namespace Game.Player
{
    [EventBusSubscriber]
    public class Player : MonoBehaviour
    { 
        public Rigidbody2D rb2d;
        public float playerSpeedY1;
        public float playerSpeedY2;
        private BuffManager _buffManager;
        private PlayerController _playerController;

        public struct SpeedBoostContext
        {
            public float boost;              // 初始外力速度大小
            public float fade;               // 衰减速率（速度/秒）
            public float duration;           // 持续时间
            public SpeedUpDirection direction; // 方向
        }
        
        public bool isWearGreySpringShoe = false;

        private void Awake()
        {
            isWearGreySpringShoe = false;
        }

        private void Start()
        {
            rb2d = GetComponent<Rigidbody2D>();
            _buffManager = GetComponent<BuffManager>();
            _playerController = GetComponent<PlayerController>();
        }
        private void FixedUpdate()
        {
            WearGreySpringShoe();
        }

        private void WearGreySpringShoe()
        {
            if (isWearGreySpringShoe)
            {
                _playerController._stats.CoyoteTime = 15f;
            }
            else
            {
                _playerController._stats.CoyoteTime = 0.25f;
            }
        }
        
        public SpeedBoostContext LatestSpeedBoostContext { get; private set; }

        public void SetSpeedBoostContext(float boost, float fade, float duration, SpeedUpDirection direction)
        {
            LatestSpeedBoostContext = new SpeedBoostContext
            {
                boost = boost,
                fade = fade,
                duration = duration,
                direction = direction
            };
        }
    }
}
