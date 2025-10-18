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
            WindupDuration = 2f;
            Duration = 5f;
            RecoveryDuration = 0;
            Cooldown = 20f;
        }
        
        public override void Start()
        {
            base.Start();
            ItemSystem.Instance.ItemsPlayerHad.Add(this);//测试
            Sprite = Resources.Load<Sprite>("Sprites/Items/Parachute");
        }

        public override void OnUseStart()
        {
            base.OnUseStart();
            print(Name+"开始使用");
        }

        public override void ApplyEffect()
        {
            /*var player = FindObjectOfType<Player.Player>();
            EventBus.TriggerEvent(new BuffAppliedEvent
            {
                Buff = new ParachuteBuff(5f, 2f,40f),
                Player = player
            });*/
        }

        public override void ApplyEffectTick()
        {
            base.ApplyEffectTick();
        }
    }
}
