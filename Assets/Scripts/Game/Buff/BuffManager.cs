using System.Collections.Generic;
using ShrinkEventBus;
using UnityEngine;

namespace Game.Buff
{
    [EventBusSubscriber]
    public class BuffManager : MonoBehaviour
    {
        private readonly List<BuffBase> _buffs = new();
        private Player.Player _player;

        public void AddBuff(BuffBase newBuff)
        {
            _player = GetComponent<Player.Player>();

            // ✅ 检查是否已有相同类型的 Buff
            var existingBuff = _buffs.Find(b => b.BuffName == newBuff.BuffName);

            if (existingBuff != null)
            {
                existingBuff.AddStack();        // 增加层数
                existingBuff.RefreshDuration(); // 可选：刷新持续时间
                Debug.Log($"{newBuff.BuffName} 层数增加到 {existingBuff.StackCount}");
            }
            else
            {
                newBuff.OnApply(_player);
                _buffs.Add(newBuff);
                Debug.Log($"添加 Buff: {newBuff.BuffName}");
            }
        }

        private void FixedUpdate()
        {
            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                var buff = _buffs[i];
                buff.OnUpdate(_player, Time.deltaTime);
                if (buff.IsExpired)
                {
                    buff.OnRemove(_player);
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

