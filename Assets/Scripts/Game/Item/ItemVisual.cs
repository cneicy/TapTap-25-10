using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Item
{
    public class ItemVisual : MonoBehaviour
    {
        [Tooltip("图片")] public Image image;
        [Tooltip("名称")] public TMP_Text text;
        [Tooltip("画布组")] public CanvasGroup canvasGroup;
        public int itemIndex;
        public int infoIndex;
        public string description;
        public RectTransform rectTransform;
        private ItemVisualController _itemVisualController;
        private bool _isDrag;
        private void Start()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        public void SetInfo(ItemBase item,int myInfoIndex,ItemVisualController topController)
        {
            image.sprite = item.sprite;
            description = item.Description;
            infoIndex = myInfoIndex;
            _itemVisualController = topController;
        }

        public void SetAlpha(float alpha)
        {
            canvasGroup.alpha = alpha;
        }
    }
}
