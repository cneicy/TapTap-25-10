using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Item
{
    public class CurrentItemVisual : MonoBehaviour
    {
        [Header("UI 显示用 Image（可为空）")]
        [SerializeField] private Image uiImage;

        [Header("SpriteRenderer 显示用（可为空）")]
        [SerializeField] private SpriteRenderer worldSpriteRenderer;

        private Dictionary<ItemBase, Sprite> _itemSprites = new();
        private ItemBase _currentItem;

        /// <summary>
        /// 初始化：加载所有 Item 的贴图。
        /// </summary>
        public void Init(List<ItemBase> allItems)
        {
            _itemSprites.Clear();

            foreach (var item in allItems)
            {
                var path = $"Texture/Items/{item.GetType().Name}";
                var sprite = Resources.Load<Sprite>(path);

                if (sprite != null)
                {
                    _itemSprites[item] = sprite;
                    item.Sprite = sprite;
                    Debug.Log($"[CurrentItemVisual] 已加载贴图: {path}");
                }
                else
                {
                    Debug.LogWarning($"[CurrentItemVisual] ❌ 找不到贴图: {path}");
                }
            }

            RefreshVisual(null);
        }

        /// <summary>
        /// 切换当前显示的 Item
        /// </summary>
        public void SetCurrentItem(ItemBase item)
        {
            _currentItem = item;
            RefreshVisual(item);
        }

        private void RefreshVisual(ItemBase item)
        {
            Sprite newSprite = null;

            if (item && _itemSprites.TryGetValue(item, out var s))
                newSprite = s;

            // 更新 UI Image
            if (uiImage)
            {
                uiImage.enabled = newSprite;
                uiImage.sprite = newSprite;
            }

            // 更新 SpriteRenderer
            if (worldSpriteRenderer)
            {
                worldSpriteRenderer.enabled = newSprite;
                worldSpriteRenderer.sprite = newSprite;
            }

            Debug.Log(item ? $"[CurrentItemVisual] 显示道具视觉: {item.GetType().Name}" : "[CurrentItemVisual] 没有当前道具，隐藏视觉");
        }
    }
}
