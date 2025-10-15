using System;
using TMPro;
using UnityEngine;

namespace Game.Item
{
    /// <summary>
    /// 控制滑动展示的物品UI系统（无拖拽版本）
    /// - 可通过外部接口切换展示项
    /// - 自动平滑移动到目标位置
    /// - 中心物品放大、透明度更高
    /// - 自动更新描述文本
    /// </summary>
    public class ItemVisualController : MonoBehaviour
    {
        [Header("UI 组件")]
        [SerializeField] private GameObject itemPrefab;       // 单个物品的Prefab
        [SerializeField] private RectTransform itemParent;    // 物品的容器
        [SerializeField] private TMP_Text descriptionText;    // 描述文本

        [Header("显示参数")]
        [SerializeField] private int displayNumber = 3;       // 同时显示的数量
        [SerializeField] private float itemSpace = 70f;       // 每个物品间距
        [SerializeField] private float moveSmooth = 8f;       // 移动平滑速度
        [SerializeField] private float scaleMultiplier = 0.002f;  // 缩放随距离衰减系数
        [SerializeField] private float alphaMultiplier = 0.002f;  // 透明度随距离衰减系数

        // ================= 内部状态 =================

        private struct ItemInfo
        {
            public Sprite Sprite;
            public string Description;
            public ItemInfo(Sprite sprite, string desc)
            {
                Sprite = sprite;
                Description = desc;
            }
        }

        private ItemInfo[] _itemInfos;
        private ItemVisual[] _items;

        private int _currentItemIndex;    // 当前居中的Item索引
        private float _targetX;           // 当前容器目标位置（平滑移动）
        private float _displayWidth;      // 计算排列宽度

        private void Start()
        {
            _displayWidth = (displayNumber - 1) * itemSpace;
        }

        #region 初始化

        /// <summary>
        /// 初始化所有物品信息
        /// </summary>
        public void SetItemsInfo(Sprite[] sprites, string[] descriptions)
        {
            if (sprites == null || descriptions == null)
            {
                Debug.LogError("存在空引用数组");
                return;
            }

            int len = sprites.Length;
            if (len == 0 || descriptions.Length != len)
            {
                Debug.LogError("数组长度不匹配");
                return;
            }

            _itemInfos = new ItemInfo[len];
            for (int i = 0; i < len; i++)
                _itemInfos[i] = new ItemInfo(sprites[i], descriptions[i]);

            // 清空旧的
            foreach (Transform child in itemParent)
                Destroy(child.gameObject);

            // 创建显示用对象
            _items = new ItemVisual[displayNumber];
            for (int i = 0; i < displayNumber; i++)
            {
                var item = Instantiate(itemPrefab, itemParent).GetComponent<ItemVisual>();
                _items[i] = item;
            }

            // 初始刷新
            MoveItems(0);
        }

        #endregion


        private void Update()
        {
            // 平滑过渡
            itemParent.localPosition = new Vector3(
                Mathf.Lerp(itemParent.localPosition.x, _targetX, Time.deltaTime * moveSmooth),
                itemParent.localPosition.y,
                0);

            // 刷新视觉表现
            UpdateItemVisuals();
        }

        #region 对外接口

        /// <summary>
        /// 切换到下一个物品
        /// </summary>
        public void NextItem()
        {
            SetCurrentIndex(_currentItemIndex + 1);
        }

        /// <summary>
        /// 切换到上一个物品
        /// </summary>
        public void PreviousItem()
        {
            SetCurrentIndex(_currentItemIndex - 1);
        }

        /// <summary>
        /// 切换到指定索引
        /// </summary>
        public void SetCurrentIndex(int index)
        {
            if (_itemInfos == null || _itemInfos.Length == 0)
                return;

            // 环绕索引
            if (index < 0)
                index = _itemInfos.Length - 1;
            else if (index >= _itemInfos.Length)
                index = 0;

            _currentItemIndex = index;
            _targetX = -_currentItemIndex * itemSpace;

            MoveItems(_currentItemIndex);
        }

        #endregion


        #region 内部逻辑

        private void MoveItems(int centerIndex)
        {
            int len = _itemInfos.Length;
            if (_items == null) return;

            int half = displayNumber / 2;

            for (int i = 0; i < displayNumber; i++)
            {
                int infoIndex = (centerIndex - half + i + len) % len;

                float x = itemSpace * (i - half);
                var item = _items[i];
                item.rectTransform.localPosition = new Vector2(x, 0);
                item.SetInfo(_itemInfos[infoIndex].Sprite, _itemInfos[infoIndex].Description, infoIndex, this);
            }

            descriptionText.text = _itemInfos[centerIndex].Description;
        }

        private void UpdateItemVisuals()
        {
            if (_items == null) return;

            foreach (var item in _items)
            {
                float distance = Mathf.Abs(item.rectTransform.position.x - transform.position.x);
                float scale = 1 - distance * scaleMultiplier;
                float alpha = 1 - distance * alphaMultiplier;

                item.rectTransform.localScale = Vector3.one * Mathf.Clamp(scale, 0.6f, 1.2f);
                item.SetAlpha(Mathf.Clamp01(alpha));
            }
        }

        #endregion
    }
}
