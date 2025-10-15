using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Item
{
    public class ItemVisual : MonoBehaviour
    {
        [Tooltip("图片")] public Image _image;
        [Tooltip("画布组")] public CanvasGroup _canvasGroup;
        public int itemIndex;
        public int infoIndex;
        public string description;
        public RectTransform rectTransform;
        private ItemVisualController _topController;
        private bool _isDrag;

        private void Start()
        {
            rectTransform = GetComponent<RectTransform>();
        }

        public void SetInfo(Sprite sprite, string description,
            int infoIndex, ItemVisualController topController)
        {
            _image.sprite = sprite;
            this.description = description;
            this.infoIndex = infoIndex;
            _topController = topController;
        }

        public void SetAlpha(float alpha)
        {
            _canvasGroup.alpha = alpha;
        }
    }
}
