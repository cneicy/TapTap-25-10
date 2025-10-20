using System;
using System.Collections;
using Game.Buff;
using Game.Player;
using ShrinkEventBus;
using UnityEngine;

namespace Game.Item
{
    public class Parachute : ItemBase
    {
        public Parachute()
        {
            Name = "降落伞";
            Description = "";
            WindupDuration = 0.2f;
            Duration = 0f;
            BuffDuration = 2f;
            RecoveryDuration = 0;
            Cooldown = 0.1f;
            IsBuff = true;
        }
        
        public void Awake()
        {
            ItemSystem.Instance.ItemsPlayerHad.Add(this);//测试
            Sprite = Resources.Load<Sprite>("Sprites/Items/Parachute");
        }

        public override void OnUseStart()
        {
            base.OnUseStart();
            print(Name+"开始使用");
        }
        
        public override void ApplyBuffEffect()
        {
            var player = FindObjectOfType<Player.Player>();
            EventBus.TriggerEvent(new BuffAppliedEvent
            {
                Buff = new ParachuteBuff(BuffDuration, -3f,140f),
                Player = player
            });
        }
        
        public override void ApplyEffectTick()
        {
            base.ApplyEffectTick();
        }
    }
}
