using System.Collections.Generic;
using Data;
using Game.Player;
using ShrinkEventBus;
using UnityEngine;
using UnityEngine.InputSystem;
using Utils;

namespace Game.Item
{
    public class PlayerGetItemEvent : EventBase
    {
        public readonly string ItemTypeName;
        public PlayerGetItemEvent(string itemTypeName)
        {
            ItemTypeName = itemTypeName;
        }
    }

    [EventBusSubscriber]
    public class ItemSystem : Singleton<ItemSystem>
    {
        public List<ItemBase> AllItems { get; private set; } = new();
        public List<string> ItemsPlayerHadTypeNames { get; private set; } = new();
        public ItemBase CurrentItem { get; private set; }

        private FrameInput _itemFrameInput;
        public CurrentItemVisual currentItemVisual;
        private int _index;

        protected override void Awake()
        {
            base.Awake();
            AllItems.Clear();
            AllItems.AddRange(GetComponents<ItemBase>());
            foreach (var item in AllItems)
            {
                item.enabled = false;
                item.IsUsed = false;
            }
            Debug.Log($"[ItemSystem] 初始化完毕，共加载 {AllItems.Count} 个 ItemBase 组件");
        }

        private void Start()
        {
            _index = 0;
            if (ItemsPlayerHadTypeNames.Count > 0)
                RestoreItemsFromSavedData();
            currentItemVisual?.RefreshVisual(CurrentItem);
        }

        [EventSubscribe]
        public void OnLoadItemsEvent(LoadItemsEvent evt)
        {
            var savedList = DataManager.Instance.GetData<List<string>>("ItemsPlayerHad");
            if (savedList is not { Count: > 0 }) return;
            ItemsPlayerHadTypeNames = savedList;
            RestoreItemsFromSavedData();
        }

        private void RestoreItemsFromSavedData()
        {
            foreach (var typeName in ItemsPlayerHadTypeNames)
            {
                var item = AllItems.Find(i => i.GetType().Name == typeName);
                if (!item) continue;
                item.enabled = true;
                item.IsUsed = true;
                Debug.Log($"[ItemSystem] 启用已拥有的道具脚本: {typeName}");
            }

            CurrentItem = AllItems.Find(i => i.enabled);
            if (CurrentItem)
                _index = AllItems.IndexOf(CurrentItem);
        }

        [EventSubscribe]
        public void OnPlayerGetItemEvent(PlayerGetItemEvent evt)
        {
            if (ItemsPlayerHadTypeNames.Contains(evt.ItemTypeName)) return;

            ItemsPlayerHadTypeNames.Add(evt.ItemTypeName);
            Debug.Log($"[ItemSystem] 玩家获得道具: {evt.ItemTypeName}");

            var item = AllItems.Find(i => i.GetType().Name == evt.ItemTypeName);
            if (item)
            {
                item.enabled = true;
                item.IsUsed = true;
            }

            if (!CurrentItem && item)
            {
                CurrentItem = item;
                _index = AllItems.IndexOf(item);
                currentItemVisual.RefreshVisual(CurrentItem);
            }

            DataManager.Instance.SetData("ItemsPlayerHad", ItemsPlayerHadTypeNames, true);
        }

        public void SwitchToPreviousItem()
        {
            if (!_itemFrameInput.LeftSwitchItem) return;
            for (var i = 1; i <= AllItems.Count; i++)
            {
                var newIndex = (_index - i + AllItems.Count) % AllItems.Count;
                if (!AllItems[newIndex].enabled) continue;
                CurrentItem?.OnUseCancel();
                CurrentItem = AllItems[newIndex];
                _index = newIndex;
                currentItemVisual.RefreshVisual(CurrentItem);
                SoundManager.Instance.Play("switchitem");
                Debug.Log($"切换到道具: {CurrentItem.Name}");
                return;
            }
        }

        public void SwitchToNextItem()
        {
            if (!_itemFrameInput.RightSwitchItem) return;
            for (var i = 1; i <= AllItems.Count; i++)
            {
                var newIndex = (_index + i) % AllItems.Count;
                if (!AllItems[newIndex].enabled) continue;
                CurrentItem?.OnUseCancel();
                CurrentItem = AllItems[newIndex];
                _index = newIndex;
                currentItemVisual.RefreshVisual(CurrentItem);
                SoundManager.Instance.Play("switchitem");
                Debug.Log($"切换到道具: {CurrentItem.Name}");
                return;
            }
        }


        public void UseItem()
        {
            if (!CurrentItem || !CurrentItem.enabled) return;
            if (_itemFrameInput.UseItem)
            {
                Debug.Log($"使用了 {CurrentItem.Name}");
                CurrentItem.OnUseStart();
            }
        }

        private void Update()
        {
            _itemFrameInput = new FrameInput
            {
                UseItem = InputSystem.actions.FindAction("Attack").triggered,
                LeftSwitchItem = InputSystem.actions.FindAction("LeftSwitchItem").triggered,
                RightSwitchItem = InputSystem.actions.FindAction("RightSwitchItem").triggered,
            };

            SwitchToNextItem();
            SwitchToPreviousItem();
            UseItem();
        }
    }
}
