using System;
using Game.Player;
using JetBrains.Annotations;
using ShrinkEventBus;
using UnityEngine;

namespace Game.Item
{
    public class GreySpringShoe : ItemBase
    {
        [CanBeNull] private Player.Player _player;

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
        
        public override void Start()
        {
            base.Start();
        }

        public override void ApplyEffect()
        {
            _player = FindFirstObjectByType<Player.Player>();
            SoundManager.Instance.Play("shoeuse");

            if (_player)
            {
                _player.isWearGreySpringShoe = true;
                EventBus.TriggerEvent(new PlayerSpinEvent(_player.transform));
            }

            print(name +"已经使用");
        }
    }
}
