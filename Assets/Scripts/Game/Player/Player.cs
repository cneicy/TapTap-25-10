using System;
using Game.Buff;
using UnityEngine;

namespace Game.Player
{
    public class Player : MonoBehaviour
    { 
        public Rigidbody2D rb2d;
        public float playerSpeedY;
        private BuffManager _buffManager;
        
        private void Start()
        {
            rb2d = GetComponent<Rigidbody2D>();
            _buffManager = GetComponent<BuffManager>();
            
            //测试
            _buffManager.AddBuff(new ParachuteBuff(0.77f,5f));
        }

        [Obsolete("Obsolete")]
        private void FixedUpdate()
        { 
            //测试
            rb2d.velocity = new Vector2(rb2d.velocity.x, playerSpeedY);
        }
    }
}
