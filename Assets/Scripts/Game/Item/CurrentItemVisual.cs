using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Item
{
    public class CurrentItemVisual : MonoBehaviour
    {
        public List<Sprite> spriteVisuals = new List<Sprite>();
        public Image spriteRenderer;
        private int _spriteIndex;
        
        public void Init(List<ItemBase> items)
        {
            if (items == null)
            {
                var defaultSprite = Resources.Load<Sprite>("Sprites/Items/diceDontKown");
                spriteRenderer.sprite = defaultSprite;
            }
            foreach (var item in items)
            {
                spriteVisuals.Add(item.Sprite);
            }
        }
        public void NextSprite()
        {
            if (_spriteIndex == spriteVisuals.Count - 1)
                _spriteIndex = 0;
            else
            {
                ++_spriteIndex;
            }
        }

        public void PreviousSprite()
        {
            if (_spriteIndex == 0)
                _spriteIndex = spriteVisuals.Count - 1;
            else
            {
                --_spriteIndex;
            }
        }

        private void UpdateCurrentItemVisual()
        {
            spriteRenderer.sprite = spriteVisuals[_spriteIndex];
        }
        

        public void AddSpriteVisual(Sprite sprite)
        {
            spriteVisuals.Add(sprite);
        }
        
        private void Update()
        {
            UpdateCurrentItemVisual();
        }
    }
}
