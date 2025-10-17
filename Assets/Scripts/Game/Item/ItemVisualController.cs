using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using Utils;

namespace Game.Item
{
    public class ItemVisualController : MonoBehaviour
    {
        private struct ItemInfo
        {
            public ItemBase Item;

            public ItemInfo(ItemBase targetItem)
            {
                Item = targetItem;
            }
        }
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private RectTransform itemParent;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private ItemInfo[] _itemInfos;
        [SerializeField] private int displayNumber;
        [SerializeField] private float itemSpace;
        [SerializeField] private float moveSmooth;
        [SerializeField] private float dragSpeed;
        [SerializeField] private float scaleMultiplier;
        [SerializeField] private float alphaMultiplier;
        
        public event Action<int> SelectAction;
        
        private ItemVisual[] _items;
        private float displayWidth;
        private int offsetTimes;
        private bool isDrag;
        private int currentItemIndex;
        private float[] distances;
        private float selectItemX;
        private bool isSelectMove;

        private bool isSelected;
        
        private bool isMoving = false;
        private float targetOffsetTimes;
        private float currentOffset;
        private void Start()
        {
            Init();
            MoveItem(0);
        }

        private void Init()
        {
            displayWidth = (displayNumber-1)*itemSpace;
            _items = new ItemVisual[displayNumber];
            for (int i = 0; i < displayNumber; i++)
            {
                ItemVisual item = Instantiate(itemPrefab, itemParent).GetComponent<ItemVisual>();
                item.itemIndex = i;
                _items[i] = item;
            }
        }

        public void SetItemsInfo(List<ItemBase> items)
        {
            if (items == null) {
                Debug.LogError("存在null参数数组");
                return;
            }
    
            if (items.Count == 0) {
                Debug.LogError("存在空数组");
                return;
            }
            // 初始化数组
            _itemInfos = new ItemInfo[items.Count];
    
            // 使用安全遍历
            for (int i = 0; i < items.Count; i++) // 关键修改点：用names.Length
            {
                // 元素级检查
                if (items[i] == null) {
                    Debug.LogError($"索引{i}的Sprite为null");
                    continue;
                }
        
                _itemInfos[i] = new ItemInfo(items[i]);
            }
        }

        public void Select(int itemIndex, int infoIndex, RectTransform rectTransform)
        {
            if (!isSelected && itemIndex == currentItemIndex)
            {
                SelectAction?.Invoke(itemIndex);
                isSelected = true;
                Debug.Log("select"+(infoIndex+1).ToString());
            }
            else
            {
                isSelected = true;
                selectItemX = rectTransform.localPosition.x;
            }
        }

        private void MoveItem(int offsetTime)
        {
            for (int i = 0; i < displayNumber; i++)
            {
                float x = itemSpace * (i-offsetTime) -displayWidth/2;
                _items[i].transform.localPosition = new Vector2(x,
                    _items[i].transform.localPosition.y);
            }

            int middle;
            if (offsetTime>0)
            {
                middle = _itemInfos.Length - offsetTime % _itemInfos.Length;
            }
            else
            {
                middle = -offsetTime % _itemInfos.Length;
            }

            int infoIndex = middle;

            for (int i = Mathf.FloorToInt(displayNumber / 2f); i < displayNumber; i++)
            {
                if (infoIndex >= _itemInfos.Length)
                {
                    infoIndex = 0;
                }
                _items[i].SetInfo(_itemInfos[infoIndex].Item,infoIndex,this);
                infoIndex++;
            }

            infoIndex = middle - 1;
            for (int i = Mathf.FloorToInt(displayNumber / 2f) - 1; i >= 0; i--)
            {
                if (infoIndex <= -1)
                {
                    infoIndex = _itemInfos.Length - 1;
                }
                _items[i].SetInfo(_itemInfos[infoIndex].Item,infoIndex,this);
                infoIndex--;
            }
        }

        private void Update()
        {
            SmoothMoveUpdate(); // 添加这一行
            ItemsControl();
        }

        private void ItemsControl()
        {
            distances = new float[displayNumber];
            for (int i = 0; i < displayNumber; i++)
            {
                float distance = Mathf.Abs(_items[i].rectTransform.position.x
                                           -itemParent.transform.position.x);
                distances[i] = distance;
                float scale = 1 - distance*scaleMultiplier;
                _items[i].rectTransform.localScale = new Vector3(scale, scale, 1);
                _items[i].SetAlpha(1-distance*alphaMultiplier);
            }
            
            float minDistance = itemSpace*displayNumber;
            int minIndex = 0;
            for (int i = 0; i < displayNumber; i++)
            {
                if (distances[i] < minDistance)
                {
                    minDistance = distances[i];
                    minIndex = i;
                }
                descriptionText.text = _items[minIndex].description;
                currentItemIndex = _items[minIndex].itemIndex;
            }
        }
        
        public void MoveToAdjacent(bool moveLeft)
        {
            if (isMoving) return; // 如果正在移动，忽略新的移动请求
    
            if (moveLeft)
            {
                targetOffsetTimes = offsetTimes + 1;
            }
            else
            {
                targetOffsetTimes = offsetTimes - 1;
            }
    
            currentOffset = offsetTimes;
            isMoving = true;
            isSelected = false;
        }

// 在 Update() 方法中调用此方法
        private void SmoothMoveUpdate()
        {
            if (!isMoving) return;
    
            // 平滑插值移动
            currentOffset = Mathf.Lerp(currentOffset, targetOffsetTimes, Time.deltaTime * moveSmooth);
    
            // 更新所有item的位置
            for (int i = 0; i < displayNumber; i++)
            {
                float x = itemSpace * (i - currentOffset) - displayWidth / 2;
                _items[i].transform.localPosition = new Vector2(x, _items[i].transform.localPosition.y);
            }
    
            // 检查是否到达目标位置
            if (Mathf.Abs(currentOffset - targetOffsetTimes) < 0.01f)
            {
                offsetTimes = Mathf.RoundToInt(targetOffsetTimes);
                currentOffset = offsetTimes;
                MoveItem(offsetTimes); // 最终对齐并更新信息
                isMoving = false;
            }
        }
    }
}