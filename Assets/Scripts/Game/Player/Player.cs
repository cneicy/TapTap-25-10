using System;
using Game.Buff;
using ShrinkEventBus;
using UnityEngine;

namespace Game.Player
{
    [EventBusSubscriber]
    public class Player : MonoBehaviour
    { 
        public Rigidbody2D rb2d;
        public float playerSpeedY;
        private BuffManager _buffManager;
        
        private void Start()
        {
            rb2d = GetComponent<Rigidbody2D>();
            _buffManager = GetComponent<BuffManager>();
        }
        private void FixedUpdate()
        {
            playerSpeedY = rb2d.linearVelocity.y;
        }
    }
}
