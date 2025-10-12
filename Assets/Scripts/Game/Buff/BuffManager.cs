using System.Collections.Generic;
using ShrinkEventBus;
using UnityEngine;

namespace Game.Buff
{
    [EventBusSubscriber]
    public class BuffManager : MonoBehaviour
    {
        private readonly List<BuffBase> _buffs = new();

        public void AddBuff(BuffBase buff)
        {
            print("添加buff");
            var player = GetComponent<Player.Player>();
            buff.OnApply(player);
            _buffs.Add(buff);
        }

        private void FixedUpdate()
        {
            var player = GetComponent<Player.Player>();

            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                var buff = _buffs[i];
                buff.OnUpdate(player, Time.deltaTime);
                if (buff.IsExpired)
                {
                    buff.OnRemove(player);
                    _buffs.RemoveAt(i);
                }
            }
        }
        
        [EventSubscribe]
        public void OnPlayerAddBuff(BuffAppliedEvent evt)
        {
            if (evt.Player == GetComponent<Player.Player>())
            {
                AddBuff(evt.Buff);
            }
        }
    }
}
