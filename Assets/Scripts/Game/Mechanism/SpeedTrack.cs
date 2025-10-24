// BoostPad2D.cs

using System;
using System.Collections.Generic;
using UnityEngine;
using ShrinkEventBus;
using Game.Buff;
using Game.Player;
using Unity.VisualScripting;

namespace Game.Mechanism
{
    public enum SpeedUpDirection
    {
        Up,Down,Left,Right
    }
    
    [RequireComponent(typeof(Collider2D))]
    public class SpeedTrack : MonoBehaviour
    {
        [Header("增加速度/增加速度衰减幅度")]
        public float speedBoost;
        public float speedBoostFade;
        [Header("增加速度的方向")]
        public SpeedUpDirection speedUpDirection;
        private Vector2 _speedUpDirection;
        //触发器
        private Collider2D _collider2D;

        private void OnTriggerEnter2D(Collider2D other)
        {
            
        }
    }
}
