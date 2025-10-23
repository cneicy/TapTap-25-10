using System;
using System.Linq;
using ShrinkEventBus;
using UnityEngine;

namespace Game.Item
{
    public class ItemInWorld : MonoBehaviour
    {
        public string theItem;

        private void OnEnable()
        {
            foreach (var unused in ItemSystem.Instance.ItemsPlayerHadTypeNames.Where(item => item == theItem))
            {
                gameObject.SetActive(false);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            EventBus.TriggerEvent(new PlayerGetItemEvent(theItem));
            gameObject.SetActive(false);
        }
    }
}