using System;
using Game.Buff;
using Game.Player;
using ShrinkEventBus;
using UnityEngine;

namespace Game.Item
{
    public class GreySpringShoe : ItemBase
    {
        public bool isUsed = false;
        public PlayerController playerController;
        private Player.Player _player;
        public GreySpringShoe()
        {
            Name = "灰色弹簧鞋";
            Description = "";
            WindupDuration = 0;
            Duration = 0;
            RecoveryDuration = 0;
            Cooldown = 0;
            IsBasement = true;
            IsBuff = false;
        }
        private void Awake()
        {
            isUsed = false;
            ItemSystem.Instance.ItemsPlayerHad.Add(this);
            _player = FindObjectOfType<Player.Player>();
        }

        public override void Start()
        {
            base.Start();
            Sprite = Resources.Load<Sprite>("Sprites/Items/GreySpringShoe");
        }

        public override void ApplyEffect()
        {
            if (isUsed == false)
            {
                _player.isWearGreySpringShoe = true;
                isUsed = true;
                print(name +"已经使用");
            }
        }
    }
}
