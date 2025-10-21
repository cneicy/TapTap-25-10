using System;
using Game.Level;
using UnityEngine;

namespace Game.Cup
{
    public class Level1Cup : CupBase
    {
        public Level1Cup()
        {
            Name = "Level1Cup";
            Description = "嘻嘻嘻嘻";
        }

        private void OnCollisionEnter2D(Collision2D other)
        {
            if (other.gameObject.name.Contains("Player"))
            {
                LevelManager.Instance.SwitchLevel("Level1");
            }
        }
    }
}