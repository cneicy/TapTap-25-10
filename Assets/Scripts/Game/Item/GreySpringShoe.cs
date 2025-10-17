using System;
using Game.Buff;
using ShrinkEventBus;
using UnityEngine;

namespace Game.Item
{
    public class GreySpringShoe : ItemBase
    {
        public bool isUsed = false;
        public GreySpringShoe()
        {
            Name = "灰色弹簧鞋";
            Description = "";
            WindupDuration = 0;
            Duration = 0;
            RecoveryDuration = 0;
            Cooldown = 0;
        }
        private void Start()
        {
            Sprite = Resources.Load<Sprite>("Sprites/Items/GreySpringShoe");
            ItemSystem.Instance.ItemsPlayerHad.Add(this);//测试
        }

        public override void ApplyEffect()
        {
            var player = FindObjectOfType<Player.Player>();
            EventBus.TriggerEvent(new BuffAppliedEvent
            {
                Buff = new GreySpringShoeBuff(),
                Player = player
            });
        }
    }
}
