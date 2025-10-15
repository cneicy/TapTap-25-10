using System.Collections.Generic;
using Data;
using ShrinkEventBus;
using UnityEngine.InputSystem;
using Utils;

namespace Game.Item
{
    public class PlayerGetItemEvent : EventBase
    {
        public readonly ItemBase Item;

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

        private void Start()
        {
            /*EventBus.TriggerEvent(new PlayerGetItemEvent(gameObject.AddComponent<TestItem>()));
            EventBus.TriggerEvent(new PlayerGetItemEvent(gameObject.AddComponent<Hands>()));*/
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
                CurrentItem = evt.Item;
            }
            ItemsPlayerHad?.Add(evt.Item);
            DataManager.Instance.SetData("ItemsPlayerHad", ItemsPlayerHad);
        }

        public void SwitchToPreviousItem()
        {
            if (index == 0) return;
            CurrentItem.OnUseCancel();
            CurrentItem = ItemsPlayerHad[--index];
            print(CurrentItem.Name);
        }

        public void SwitchToNextItem()
        {
            if(index >= ItemsPlayerHad.Count-1) return;
            CurrentItem.OnUseCancel();
            CurrentItem = ItemsPlayerHad[++index];
            print(CurrentItem.Name);
        }

        private void Update()
        {
            if (InputSystem.actions.FindAction("Attack").triggered)
            {
                CurrentItem?.OnUseStart();
            }

            if (InputSystem.actions.FindAction("SwitchPrevious").triggered)
            {
                SwitchToPreviousItem();
            }

            if (InputSystem.actions.FindAction("SwitchNext").triggered)
            {
                SwitchToNextItem();
            }
        }
    }
}