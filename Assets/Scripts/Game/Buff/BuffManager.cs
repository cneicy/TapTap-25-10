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

        private void Awake()
        {
            _player = GetComponent<Player.Player>();
            EventBus.AutoRegister(this);
        }

        private void OnDestroy()
        {
            EventBus.UnregisterInstance(this);
        }

        /// <summary>
        /// 添加 Buff。如果已有相同 Buff，则叠加层数或刷新持续时间
        /// </summary>
        public void AddBuff(BuffBase newBuff)
        {
            var existingBuff = _buffs.Find(b => b.BuffName == newBuff.BuffName);

            if (existingBuff != null)
            {
                existingBuff.AddStack();
                existingBuff.RefreshDuration();
                Debug.Log($"[BuffManager] {newBuff.BuffName} 层数增加到 {existingBuff.StackCount}");
            }
            else
            {
                newBuff.OnApply(_player);
                _buffs.Add(newBuff);
                Debug.Log($"[BuffManager] 添加 Buff: {newBuff.BuffName}");
            }
        }

        private void FixedUpdate()
        {
            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                var buff = _buffs[i];
                buff.OnUpdate(_player, Time.fixedDeltaTime);
                if (buff.IsExpired)
                {
                    buff.OnRemove(_player);
                    _buffs.RemoveAt(i);
                    Debug.Log($"[BuffManager] 移除 Buff(超时): {buff.BuffName}");
                }
            }
        }

        [EventSubscribe]
        public void OnPlayerAddBuff(BuffAppliedEvent evt)
        {
            if (evt.Player == _player)
            {
                AddBuff(evt.Buff);
            }
        }

        // -------------------------------------------------------------
        // 🔽 下面是新增：移除/查询相关的便捷 API
        // -------------------------------------------------------------

        /// <summary>
        /// 按 Buff 名称移除（完全移除，不区分叠层）。
        /// 返回 true 表示找到并移除；false 表示未找到。
        /// </summary>
        public bool RemoveBuff(string buffName)
        {
            int idx = _buffs.FindIndex(b => b.BuffName == buffName);
            if (idx < 0) return false;

            var buff = _buffs[idx];
            buff.OnRemove(_player);
            _buffs.RemoveAt(idx);
            Debug.Log($"[BuffManager] 移除 Buff: {buffName}");
            return true;
        }

        /// <summary>
        /// 按类型移除（找到第一个匹配的 Buff 并移除）。
        /// </summary>
        public bool RemoveBuff<T>() where T : BuffBase
        {
            int idx = _buffs.FindIndex(b => b is T);
            if (idx < 0) return false;

            var buff = _buffs[idx];
            buff.OnRemove(_player);
            _buffs.RemoveAt(idx);
            Debug.Log($"[BuffManager] 移除 Buff: {buff.BuffName} (类型 {typeof(T).Name})");
            return true;
        }

        /// <summary>
        /// 按类型移除所有匹配的 Buff（如果你将来允许同类多实例的话会有用）。
        /// 返回移除数量。
        /// </summary>
        public int RemoveAll<T>() where T : BuffBase
        {
            int removed = 0;
            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                if (_buffs[i] is T)
                {
                    var buff = _buffs[i];
                    buff.OnRemove(_player);
                    _buffs.RemoveAt(i);
                    removed++;
                }
            }

            if (removed > 0)
                Debug.Log($"[BuffManager] 移除 {removed} 个类型 {typeof(T).Name} 的 Buff");

            return removed;
        }

        /// <summary>
        /// 是否拥有指定名称的 Buff。
        /// </summary>
        public bool HasBuff(string buffName) =>
            _buffs.Exists(b => b.BuffName == buffName);

        /// <summary>
        /// 是否拥有指定类型的 Buff。
        /// </summary>
        public bool HasBuff<T>() where T : BuffBase =>
            _buffs.Exists(b => b is T);

        /// <summary>
        /// 获取指定名称的 Buff（不存在返回 null）。
        /// </summary>
        public BuffBase GetBuff(string buffName) =>
            _buffs.Find(b => b.BuffName == buffName);

        /// <summary>
        /// 获取指定类型的 Buff（不存在返回 null）。
        /// </summary>
        public T GetBuff<T>() where T : BuffBase =>
            _buffs.Find(b => b is T) as T;

        /// <summary>
        /// 清空所有 Buff（会逐个调用 OnRemove）。
        /// </summary>
        public void ClearAllBuffs()
        {
            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                var buff = _buffs[i];
                buff.OnRemove(_player);
                _buffs.RemoveAt(i);
            }
            Debug.Log("[BuffManager] 已清空所有 Buff");
        }
    }
}
