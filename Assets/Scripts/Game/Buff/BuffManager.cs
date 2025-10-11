using System.Collections.Generic;
using UnityEngine;

namespace Game.Buff
{
    public class BuffManager : MonoBehaviour
    {
        private readonly List<BuffBase> _buffs = new();

        public void AddBuff(BuffBase buff)
        {
            buff.OnApply(GetComponent<Player.Player>());
            _buffs.Add(buff);
        }

        private void Update()
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
    }
}
