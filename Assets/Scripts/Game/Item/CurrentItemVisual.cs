using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Item
{
    public class CurrentItemVisual : MonoBehaviour
    {
        [Header("UI 显示用 Image（可为空）")]
        [SerializeField] private Image uiImage;

        [Header("CD遮罩")]
        [SerializeField] private Image cd;
        
        [Header("鞋子Buff")]
        [SerializeField] private Image shoesImage;
        private ItemBase _currentItem;
        public Player.Player player;
        
        public void RefreshVisual(ItemBase item)
        {
            Sprite newSprite = null;
            newSprite = item.sprite;
            _currentItem = item;
            uiImage.enabled = newSprite;
            uiImage.sprite = newSprite;
        }
        
        private void Update()
        {
            if (_currentItem != null)
            {
                if (!_currentItem.IsInCooldown)
                {
                    cd.enabled = false;
                }
                else
                {
                    cd.enabled = true;
                    var size = cd.rectTransform.sizeDelta;
                    size.y = 150 * (_currentItem.CooldownRemaining / _currentItem.Cooldown);
                    cd.rectTransform.sizeDelta = size;
                }
            }
            player = FindFirstObjectByType<Player.Player>();
            if (player.isWearGreySpringShoe)
            {
                shoesImage.enabled = true;
            }
            else
            {
                shoesImage.enabled = false;
            }
        }
    }
}