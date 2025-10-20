using System;
using System.Collections.Generic;
using Data;
using Game.Player;
using ShrinkEventBus;
using UnityEngine.InputSystem;
using Utils;

namespace Game.Item
{
    public abstract class PlayerGetItemEvent : EventBase
    {
        public readonly ItemBase Item;
        private bool _isHover;

        public PlayerGetItemEvent(ItemBase item)
        {
            Item = item;
        }
    }

    [EventBusSubscriber]
    public class ItemSystem : Singleton<ItemSystem>
    {
        public List<ItemBase> ItemsPlayerHad { get; set; } = new();
        public ItemBase CurrentItem { get; set; }
        public int index;
        private FrameInput ItemFrameInput;
        public ItemVisualController itemVisualController;
        public CurrentItemVisual currentItemVisual;
        
        private void Start()
        {
            /*EventBus.TriggerEvent(new PlayerGetItemEvent(gameObject.AddComponent<TestItem>()));
            EventBus.TriggerEvent(new PlayerGetItemEvent(gameObject.AddComponent<Hands>()));*/
            index = 0;
            CurrentItem = ItemsPlayerHad[index];
        }
        
        [EventSubscribe]
        public void OnPlayerDataLoadedEvent(PlayerDataLoadedEvent evt)
        {
            if(DataManager.Instance.GetData<List<ItemBase>>("ItemsPlayerHad") is not null)
                ItemsPlayerHad = DataManager.Instance.GetData<List<ItemBase>>("ItemsPlayerHad");
        }

        [EventSubscribe]
        public void OnPlayerGetItemEvent(PlayerGetItemEvent evt)
        {
            if (ItemsPlayerHad.Contains(evt.Item)) return;
            if (ItemsPlayerHad.Count == 0)
            {
                ItemsPlayerHad?.Add(evt.Item);
                CurrentItem = evt.Item;
            }
            ItemsPlayerHad?.Add(evt.Item);
            DataManager.Instance.SetData("ItemsPlayerHad", ItemsPlayerHad);
        }

        public void SwitchToPreviousItem()
        {
            if (index == 0) return;
            if (ItemFrameInput.LeftSwitchItem)
            {
                CurrentItem.OnUseCancel();
                CurrentItem = ItemsPlayerHad[--index];
                print(CurrentItem.Name);
                currentItemVisual.PreviousSprite();
            }
        }

        public void SwitchToNextItem()
        {
            if(index >= ItemsPlayerHad.Count-1) return;
            if (ItemFrameInput.RightSwitchItem)
            {
                CurrentItem.OnUseCancel();
                CurrentItem = ItemsPlayerHad[++index];
                print(CurrentItem.Name);
                currentItemVisual.NextSprite();
            }
        }

        public void UseItem()
        {
            print(ItemsPlayerHad.Count);
            if(CurrentItem == null) return;
            if (ItemFrameInput.UseItem)
            {
                CurrentItem.OnUseStart();
            }
        }

        private void Update()
        {
            ItemFrameInput = new FrameInput
            {
                UseItem = InputSystem.actions.FindAction("Attack").triggered,
                LeftSwitchItem = InputSystem.actions.FindAction("LeftSwitchItem").triggered,
                RightSwitchItem = InputSystem.actions.FindAction("RightSwitchItem").triggered,
            };
            SwitchToNextItem();
            SwitchToPreviousItem();
            UseItem();
        }

        private void FixedUpdate()
        {
            
        }
    }
}