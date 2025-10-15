using System;
using Game.Buff;
using ShrinkEventBus;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Player
{
    [EventBusSubscriber]
    public class Player : MonoBehaviour
    { 
        public Rigidbody2D rb2d;
        public float playerSpeedY;
        private BuffManager _buffManager;
        private PlayerController _playerController;

        public bool isWearGreySpringShoe = false;
        
        private void Start()
        {
            rb2d = GetComponent<Rigidbody2D>();
            _buffManager = GetComponent<BuffManager>();
            _playerController = GetComponent<PlayerController>();
        }
        private void FixedUpdate()
        {
            playerSpeedY = rb2d.linearVelocity.y;
            
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
                _playerController._stats.CoyoteTime = 0.15f;
            }
        }
    }
}
